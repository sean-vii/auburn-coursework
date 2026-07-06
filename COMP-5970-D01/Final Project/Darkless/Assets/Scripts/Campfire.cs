using UnityEngine;
using UnityEngine.InputSystem; // new Input System — used only for the temporary test-refuel key

// The campfire: your main safe zone. It slowly BURNS its fuel; as the fuel drops, the safe
// LightZone radius (and the fire's glow) shrink with it. Feed it wood to build it back up.
// If the fuel reaches zero the fire goes OUT and there's no safe zone — but it's recoverable:
// add fuel and it relights. Put this on the campfire object, next to a LightZone.
[RequireComponent(typeof(LightZone))]
public class Campfire : MonoBehaviour
{
    [Header("Fuel")]
    public float maxFuel = 100f;
    public float startingFuel = 60f;
    [Tooltip("Fuel burned per second. The night difficulty ramp (Phase 6) will raise this over time.")]
    public float burnPerSecond = 2f;

    [Header("Safe radius (driven by fuel)")]
    [Tooltip("LightZone radius when the fire is at full fuel.")]
    public float maxRadius = 8f;
    [Tooltip("LightZone radius when the fire is almost out (just before it dies).")]
    public float minRadius = 1.5f;

    [Header("Visuals (optional — wire if you have them)")]
    [Tooltip("A point Light child of the fire. Its intensity and range scale with the fuel.")]
    public Light fireLight;
    public float litLightIntensity = 2f;
    [Tooltip("A fire particle effect. Plays while lit, stops when the fire goes out.")]
    public ParticleSystem fireParticles;

    [Header("Testing (temporary — replaced by backpack wood in Phase 3)")]
    [Tooltip("Press this key anywhere to add fuel, just to test refuelling for now.")]
    public bool enableTestRefuel = true;
    public float testRefuelAmount = 25f;

    LightZone zone;
    float currentFuel;

    public bool IsLit => currentFuel > 0f;
    public float Fuel01 => maxFuel > 0f ? currentFuel / maxFuel : 0f;

    void Awake()
    {
        zone = GetComponent<LightZone>();
        currentFuel = Mathf.Clamp(startingFuel, 0f, maxFuel);
    }

    void Update()
    {
        // Burn down over time.
        if (currentFuel > 0f)
            currentFuel = Mathf.Max(0f, currentFuel - burnPerSecond * Time.deltaTime);

        // Temporary manual refuel so we can test the loop now. Phase 3 replaces this with
        // "deposit a log/stick from the backpack" -> AddFuel(...). Uses the new Input System
        // directly (Keyboard.current) so it works whatever the project's input setting is.
        if (enableTestRefuel && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            AddFuel(testRefuelAmount);

        ApplyFuelToLook();
    }

    // Feed the fire. Called by the test key now; by the backpack "deposit wood" action later.
    public void AddFuel(float amount)
    {
        currentFuel = Mathf.Clamp(currentFuel + amount, 0f, maxFuel);
    }

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
    }
}
