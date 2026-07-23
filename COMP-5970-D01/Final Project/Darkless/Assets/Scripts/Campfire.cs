using UnityEngine;

// The campfire: your main safe zone. It slowly BURNS its fuel; as the fuel drops, the safe
// LightZone radius (and the fire's glow) shrink with it. Feed it LOGS (via CampfireDeposit) to build
// it back up. If the fuel reaches zero the fire goes OUT and there's no safe zone — but it's
// recoverable: add fuel and it relights. Put this on the campfire object, next to a LightZone.
//
// FUEL IS MEASURED IN SECONDS OF ILLUMINATION: burnPerSecond = 1 means 1 fuel = 1 second lit, so
// maxFuel is the fire's maximum burn time (300 = 5 minutes). Each Log's fuelValue is its seconds
// (90 = 1.5 minutes). The Phase-6 night ramp can raise burnPerSecond to make fuel burn faster.
[RequireComponent(typeof(LightZone))]
public class Campfire : MonoBehaviour
{
    [Header("Fuel (measured in SECONDS of illumination)")]
    [Tooltip("Maximum burn time in seconds (the fire caps here no matter how many logs you add). " +
             "300 = 5 minutes.")]
    public float maxFuel = 300f;
    [Tooltip("Seconds of illumination the fire starts the game with.")]
    public float startingFuel = 120f;
    [Tooltip("Fuel burned per second (1 = fuel is in real seconds). The night difficulty ramp " +
             "(Phase 6) will raise this over time to make later nights harder.")]
    public float burnPerSecond = 1f;

    [Header("Safe radius (driven by fuel)")]
    [Tooltip("LightZone radius when the fire is at full fuel.")]
    public float maxRadius = 56f;
    [Tooltip("LightZone radius when the fire is almost out (just before it dies).")]
    public float minRadius = 8f;

    [Header("Visuals (optional — wire if you have them)")]
    [Tooltip("A point Light child of the fire. Its intensity and range scale with the fuel.")]
    public Light fireLight;
    public float litLightIntensity = 2f;
    [Tooltip("A fire particle effect. Plays while lit, stops when the fire goes out.")]
    public ParticleSystem fireParticles;

    [Header("Fire VFX (Full Opaque Fire)")]
    [Tooltip("The VFX_FireController on the Full Opaque Fire effect. Its intensity (particle size, " +
             "spawn rate, and glow) is driven by the current fuel, so the fire visibly shrinks as it " +
             "burns down and goes dark when the fire is out.")]
    public YourNamespace.VFX_FireController fireVfx;
    [Tooltip("Fire VFX intensity at full fuel (a big, roaring fire).")]
    public float maxFireIntensity = 0.55f;
    [Tooltip("Fire VFX intensity when the fire is almost out (a small, guttering flame).")]
    public float minFireIntensity = 0.25f;
    [Tooltip("The fire glow's REACH (point-light RANGE) at full fuel — how big the lit circle is. " +
             "Scales with fuel so a bigger fire lights a bigger area.")]
    public float maxLightRange = 98f;
    [Tooltip("The fire glow's reach when the fire is almost out.")]
    public float minLightRange = 14f;
    [Tooltip("The fire light's BRIGHTNESS at full fuel. Driven directly (NOT via the VFX controller, " +
             "whose intensity is tied to particle size and is far too dim on its own), so the campfire " +
             "clearly lights the circle around it. The old campfire point light used ~50 for reference.")]
    public float maxLightIntensity = 60f;
    [Tooltip("The fire light's brightness when almost out.")]
    public float minLightIntensity = 20f;
    [Tooltip("Overall fire SIZE at full fuel: the whole FireVFX transform is scaled to this. This is " +
             "what makes the flames visibly GROW when you stoke the fire (all 4 particle systems use " +
             "Local scaling, so scaling the transform scales the actual particle sizes).")]
    public float maxFireScale = 0.2f;
    [Tooltip("Overall fire size when the fire is almost out (a small, low flame).")]
    public float minFireScale = 0.1f;

    Transform fireVfxTransform;   // cached FireVFX transform, scaled by fuel

    LightZone zone;
    Light fireVfxLight;   // the Full Opaque Fire's own point light (its range scales with fuel)
    float currentFuel;

    public bool IsLit => currentFuel > 0f;
    public float Fuel01 => maxFuel > 0f ? currentFuel / maxFuel : 0f;

    void Awake()
    {
        zone = GetComponent<LightZone>();
        if (fireVfx != null)
        {
            fireVfxLight = fireVfx.GetComponentInChildren<Light>(true);
            fireVfxTransform = fireVfx.transform;
        }
        currentFuel = Mathf.Clamp(startingFuel, 0f, maxFuel);
    }

    void Update()
    {
        // Burn down over time.
        if (currentFuel > 0f)
            currentFuel = Mathf.Max(0f, currentFuel - burnPerSecond * Time.deltaTime);

        ApplyFuelToLook();
    }

    // Feed the fire (seconds of fuel). Called by CampfireDeposit when the player dumps logs in.
    public void AddFuel(float amount)
    {
        currentFuel = Mathf.Clamp(currentFuel + amount, 0f, maxFuel);
    }

    // True when the fire is at its 5-minute cap (CampfireDeposit stops feeding logs here).
    public bool IsFull => currentFuel >= maxFuel;

    void ApplyFuelToLook()
    {
        // Safe radius: 0 when the fire is out, otherwise scales minRadius..maxRadius by fuel.
        zone.radius = IsLit ? Mathf.Lerp(minRadius, maxRadius, Fuel01) : 0f;

        if (fireLight != null)
        {
            fireLight.enabled = IsLit;
            fireLight.intensity = litLightIntensity * Fuel01;
            fireLight.range = Mathf.Max(zone.radius, 0.01f);
        }

        if (fireParticles != null)
        {
            if (IsLit && !fireParticles.isPlaying) fireParticles.Play();
            else if (!IsLit && fireParticles.isPlaying) fireParticles.Stop();
        }

        // Drive the Full Opaque Fire VFX from fuel: full fuel -> a big fire, low fuel -> a small
        // flame, no fuel -> intensity 0 (particles and glow vanish, so the fire looks "out"). This
        // scales the particle SIZE + spawn rate + the light's BRIGHTNESS together.
        if (fireVfx != null)
            fireVfx.SetFireIntensity(IsLit ? Mathf.Lerp(minFireIntensity, maxFireIntensity, Fuel01) : 0f);

        // Drive the fire light directly for a CLEAR, bright circle of light. We set this AFTER
        // SetFireIntensity (which also writes intensity, but far too dim) so our value wins — the
        // light's brightness and reach both grow with fuel, independently of the particle size.
        if (fireVfxLight != null)
        {
            fireVfxLight.enabled = IsLit;
            fireVfxLight.intensity = IsLit ? Mathf.Lerp(minLightIntensity, maxLightIntensity, Fuel01) : 0f;
            fireVfxLight.range = IsLit ? Mathf.Lerp(minLightRange, maxLightRange, Fuel01) : 0f;
        }

        // Scale the whole VFX transform with fuel — the main lever for VISIBLE particle size, so
        // the flames clearly grow as you stoke it. (SetFireIntensity's startSize tweak alone is too
        // subtle to read.) When out, hold at min scale; the particles are already killed above.
        if (fireVfxTransform != null)
        {
            float s = Mathf.Lerp(minFireScale, maxFireScale, IsLit ? Fuel01 : 0f);
            fireVfxTransform.localScale = new Vector3(s, s, s);
        }
    }
}
