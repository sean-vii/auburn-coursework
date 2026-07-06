using UnityEngine;

// Cross-fades between a day music track and a night music track based on
// DayNightCycle.IsNight. A separate constant ambience track (not controlled here)
// plays underneath the whole time. Both music tracks play at once; their VOLUME
// is what decides which one you actually hear.
public class DayNightMusic : MonoBehaviour
{
    [Header("References")]
    public DayNightCycle dayNightCycle;
    public AudioSource dayMusic;
    public AudioSource nightMusic;

    [Header("Volumes")]
    public float dayMaxVolume = 0.5f;
    public float nightMaxVolume = 0.5f;
    [Tooltip("How fast tracks fade in/out, in volume units per second.")]
    public float fadeSpeed = 0.5f;

    void Start()
    {
        // Loop both tracks, start them silent, and play them. Volume does the rest.
        if (dayMusic != null)
        {
            dayMusic.loop = true;
            dayMusic.volume = 0f;
            dayMusic.Play();
        }
        if (nightMusic != null)
        {
            nightMusic.loop = true;
            nightMusic.volume = 0f;
            nightMusic.Play();
        }
    }

    void Update()
    {
        // Guard: if anything isn't wired up in the Inspector yet, do nothing (no errors).
        if (dayNightCycle == null || dayMusic == null || nightMusic == null)
            return;

        // The active track targets its max volume; the other targets 0.
        float dayTarget = dayNightCycle.IsNight ? 0f : dayMaxVolume;
        float nightTarget = dayNightCycle.IsNight ? nightMaxVolume : 0f;

        // Ease each volume toward its target for a smooth fade instead of a hard switch.
        dayMusic.volume = Mathf.MoveTowards(dayMusic.volume, dayTarget, fadeSpeed * Time.deltaTime);
        nightMusic.volume = Mathf.MoveTowards(nightMusic.volume, nightTarget, fadeSpeed * Time.deltaTime);
    }
}
