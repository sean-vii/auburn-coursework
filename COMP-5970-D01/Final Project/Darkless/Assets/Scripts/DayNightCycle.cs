using UnityEngine;

// Drives a simple day/night cycle. An internal timer (timeOfDay) runs from 0 to 1
// and loops forever. As it advances we rotate the directional light ("sun") and
// blend the scene's lighting and sky from a night look to a day look.
// Other scripts (e.g. DayNightMusic) can read the public IsNight value.
public class DayNightCycle : MonoBehaviour
{
    [Header("Sun")]
    [Tooltip("The scene's Directional Light. Unity treats this as the sun.")]
    public Light sun;

    [Header("Cycle length")]
    [Tooltip("How many real seconds one full day + night takes. Use ~20 for testing, raise later.")]
    public float fullDayLengthSeconds = 20f;

    [Header("Day / night balance")]
    [Tooltip("Fraction of the whole cycle that is daytime. Lower = longer, darker nights. " +
             "0.4 means night is 60% of the cycle — good for a horror mood.")]
    [Range(0.1f, 0.9f)]
    public float dayPortion = 0.4f;

    [Header("Sun intensity")]
    public float daySunIntensity = 1f;
    public float nightSunIntensity = 0.05f;

    [Header("Ambient light (fills shadowed areas even where the sun doesn't hit)")]
    public Color dayAmbientColor = new Color(0.75f, 0.75f, 0.72f);
    public Color nightAmbientColor = new Color(0.05f, 0.07f, 0.15f);

    [Header("Skybox tint (only if the skybox material supports it — see notes)")]
    [Tooltip("Turn off if the low-poly skybox has no _Tint/_Exposure and shouldn't be blended.")]
    public bool blendSkybox = true;
    public Color daySkyTint = new Color(0.5f, 0.5f, 0.5f);
    public Color nightSkyTint = new Color(0.10f, 0.12f, 0.25f);
    public float daySkyExposure = 1f;
    public float nightSkyExposure = 0.3f;

    [Header("Fog (blended so day is clear and night is thick)")]
    [Tooltip("Let the cycle drive the scene fog. When ON, tune fog HERE, not in the Lighting " +
             "window — these values override it while the game is playing.")]
    public bool blendFog = true;
    [Tooltip("Fog colour at midday. Keep it pale so daytime stays readable.")]
    public Color dayFogColor = new Color(0.62f, 0.66f, 0.70f);
    [Tooltip("Fog colour at night. Dark and cold for the horror mood.")]
    public Color nightFogColor = new Color(0.04f, 0.05f, 0.10f);
    [Tooltip("Exponential fog thickness at midday. Low = you can see far.")]
    public float dayFogDensity = 0.006f;
    [Tooltip("Exponential fog thickness at night. High = closed-in, can't see far.")]
    public float nightFogDensity = 0.03f;

    [Header("Time of day")]
    [Tooltip("0 = dawn. Rises to midday around dayPortion/2, dusk at dayPortion, then night " +
             "until it loops back to 0. Starts near dawn so the player gets a full day to prep.")]
    [Range(0f, 1f)]
    public float timeOfDay = 0.05f;

    // Read by DayNightMusic to decide which track to fade in.
    // First half of the cycle is day, second half is night.
    public bool IsNight { get; private set; }

    void Update()
    {
        // Advance the clock. Dividing by the day length means a bigger length = slower cycle.
        timeOfDay += Time.deltaTime / fullDayLengthSeconds;
        if (timeOfDay > 1f)
            timeOfDay -= 1f; // loop forever

        UpdateLighting();
    }

    void UpdateLighting()
    {
        // Split the cycle into a DAY window [0, dayPortion) and a longer NIGHT window.
        // During the day the sun arcs overhead and daylight rises to 1 and falls back to 0;
        // during the (longer) night the sun stays below the horizon and daylight is 0.
        float daylight;
        float sunAngle;
        if (timeOfDay < dayPortion)
        {
            float dayPhase = timeOfDay / dayPortion;            // 0..1 across the daytime
            sunAngle = dayPhase * 180f;                          // horizon → overhead → horizon
            daylight = Mathf.Sin(dayPhase * Mathf.PI);           // smooth 0 → 1 → 0
        }
        else
        {
            float nightPhase = (timeOfDay - dayPortion) / (1f - dayPortion); // 0..1 across the night
            sunAngle = 180f + nightPhase * 180f;                 // stays below the horizon
            daylight = 0f;                                       // full dark all night
        }
        daylight = Mathf.Clamp01(daylight);

        // Rotate the sun to match (170 on Y just keeps the light angled, not dead-on).
        if (sun != null)
            sun.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

        // Blend each setting from its night value to its day value by 'daylight'.
        if (sun != null)
            sun.intensity = Mathf.Lerp(nightSunIntensity, daySunIntensity, daylight);

        // Ambient light must change too, or night stays oddly bright under a bright sky/post-fx.
        RenderSettings.ambientLight = Color.Lerp(nightAmbientColor, dayAmbientColor, daylight);

        // Blend the sky itself — but only if a skybox is assigned AND it actually has these
        // properties (HasProperty guards against the low-poly skybox using different names).
        if (blendSkybox && RenderSettings.skybox != null)
        {
            if (RenderSettings.skybox.HasProperty("_Tint"))
                RenderSettings.skybox.SetColor("_Tint", Color.Lerp(nightSkyTint, daySkyTint, daylight));
            if (RenderSettings.skybox.HasProperty("_Exposure"))
                RenderSettings.skybox.SetFloat("_Exposure", Mathf.Lerp(nightSkyExposure, daySkyExposure, daylight));
        }

        // Fog: blend from a thin, pale day haze to thick dark night fog, so daytime stays readable
        // and night closes in. Driven here every frame, which overrides the Lighting-window fog.
        if (blendFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = Color.Lerp(nightFogColor, dayFogColor, daylight);
            RenderSettings.fogDensity = Mathf.Lerp(nightFogDensity, dayFogDensity, daylight);
        }

        // Simple day/night flag for other scripts to read: night starts once the day window ends.
        IsNight = timeOfDay >= dayPortion;
    }
}
