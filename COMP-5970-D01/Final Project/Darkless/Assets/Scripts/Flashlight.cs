using UnityEngine;
using UnityEngine.InputSystem; // new Input System — read the toggle/burst keys directly

// The flashlight: a finite, IRREPLACEABLE emergency light. Held by the player.
// Unlike the campfire, its battery ONLY depletes — it never recharges. That whole battery
// is your budget for the entire game, so you hoard it and use it in emergencies.
//
// While the beam is ON it counts as safe light (via a LightZone on the same object), so it
// keeps the darkness meter (PlayerDarkness) topped up — and later it will slow the monster.
// It also drives a real Spot Light (`beam`) so you can actually see.
//
// The BURST is the panic button: a brief, WIDER safe radius for emergencies, paid for with an
// extra chunk of battery.
//
// Put this on the flashlight object, which should be a CHILD of the player so its light zone
// follows you. It needs a LightZone (auto-added) and, ideally, a Spot Light assigned to `beam`.
[RequireComponent(typeof(LightZone))]
public class Flashlight : MonoBehaviour
{
    [Header("Battery (only ever depletes — never recharges)")]
    [Tooltip("Seconds of light in a full battery. This is your ENTIRE budget for the whole game.")]
    public float maxBatterySeconds = 180f;
    [Tooltip("Battery to start the game with. Usually the same as max (a fresh battery).")]
    public float startingBatterySeconds = 180f;
    [Tooltip("Battery seconds drained per real second while the beam is ON.")]
    public float drainPerSecond = 1f;

    [Header("Safe radius / beam")]
    [Tooltip("LightZone radius while the beam is on (how far it keeps you safe from the dark).")]
    public float onRadius = 5f;
    [Tooltip("Optional Spot Light child that is the visible beam. Enabled/disabled with the flashlight.")]
    public Light beam;
    [Tooltip("Optional faked 'volumetric' beam cone (a FlashlightBeamCone object). Shown only while " +
             "the flashlight is on. Purely cosmetic; the real safe light is the LightZone + Spot Light.")]
    public GameObject beamCone;

    [Header("Held model")]
    [Tooltip("The visible flashlight mesh in the player's hand. It only shows while the flashlight is " +
             "ON (being used) and hides when it's off or dead. Leave empty to keep the model always " +
             "visible. Assign the mesh object here (usually a child of this object).")]
    public GameObject heldModel;
    [Tooltip("If true, hide the model when the flashlight is off (only appears in hand when in use). " +
             "If false, the model stays in hand all the time and only the beam turns on/off.")]
    public bool hideModelWhenOff = true;

    [Header("Emergency burst (panic button)")]
    [Tooltip("Allow the burst. Turn off if you don't want an emergency mode yet.")]
    public bool enableBurst = true;
    [Tooltip("Safe radius during a burst (wider than the normal on-radius).")]
    public float burstRadius = 9f;
    [Tooltip("How long one burst stays wide, in seconds.")]
    public float burstDuration = 1.5f;
    [Tooltip("Extra battery seconds spent to fire one burst (on top of the normal drain).")]
    public float burstBatteryCost = 10f;

    [Header("One light at a time")]
    [Tooltip("The player's Torch. Turning the flashlight ON puts the torch away (you re-equip it from " +
             "your pack). Auto-found if empty.")]
    public Torch torch;

    [Header("Input (new Input System keys)")]
    public Key toggleKey = Key.F;
    public Key burstKey = Key.G;

    [Header("State (read-only)")]
    [Tooltip("Is the beam currently on? Shown here just so you can watch it in Play mode.")]
    public bool isOn = false;

    LightZone zone;
    float currentBattery;
    float burstTimer; // seconds of burst still remaining (>0 = a burst is active)

    // 1 = full battery, 0 = dead. Public so a battery UI meter can read it later.
    public float Battery01 => maxBatterySeconds > 0f ? currentBattery / maxBatterySeconds : 0f;
    // Once the battery hits zero it's gone for good — the flashlight can never turn on again.
    public bool IsDead => currentBattery <= 0f;

    void Awake()
    {
        zone = GetComponent<LightZone>();
        if (torch == null) torch = FindFirstObjectByType<Torch>();
        currentBattery = Mathf.Clamp(startingBatterySeconds, 0f, maxBatterySeconds);
        ApplyState();
    }

    void Update()
    {
        // Read keys directly (like Campfire) so it works whatever the project's input setting is.
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb[toggleKey].wasPressedThisFrame) Toggle();
            if (enableBurst && kb[burstKey].wasPressedThisFrame) TryBurst();
        }

        // Count the active burst down.
        if (burstTimer > 0f)
            burstTimer = Mathf.Max(0f, burstTimer - Time.deltaTime);

        // Drain the battery only while the beam is actually on.
        if (isOn && currentBattery > 0f)
            currentBattery = Mathf.Max(0f, currentBattery - drainPerSecond * Time.deltaTime);

        // A dead battery forces the light off — and it can't come back.
        if (currentBattery <= 0f)
            isOn = false;

        ApplyState();
    }

    // Flip the beam on/off (bound to toggleKey; can also be called from other systems/UI).
    public void Toggle()
    {
        if (IsDead) return;   // dead battery: nothing happens
        isOn = !isOn;
        if (isOn) StowTorch();   // one light at a time — the flashlight always wins
    }

    // Fire an emergency burst: force the light on, pay the battery cost, widen the safe radius.
    void TryBurst()
    {
        if (IsDead) return;
        isOn = true;
        StowTorch();
        currentBattery = Mathf.Max(0f, currentBattery - burstBatteryCost);
        burstTimer = burstDuration;
    }

    // Put the currently-held torch away (its remaining life is kept so it can be re-equipped).
    void StowTorch()
    {
        if (torch == null) torch = FindFirstObjectByType<Torch>();
        if (torch != null) torch.Unequip();
    }

    // Put the flashlight away — called when a torch is equipped (one light at a time).
    public void ForceOff()
    {
        isOn = false;
        ApplyState();
    }

    // Pushes the current state onto the LightZone radius and the visible beam.
    void ApplyState()
    {
        bool lit = isOn && currentBattery > 0f;
        bool bursting = burstTimer > 0f;

        // Safe radius: wide during a burst, normal while on, zero when off/dead.
        zone.radius = lit ? (bursting ? burstRadius : onRadius) : 0f;

        if (beam != null)
            beam.enabled = lit;

        // The faked visible beam cone follows the same on/off state as the beam.
        if (beamCone != null)
            beamCone.SetActive(lit);

        // Show the flashlight in the player's hand only while it's on (unless the model is
        // meant to stay out all the time). SetActive to the same value is a no-op, so this is
        // safe to call every frame.
        if (heldModel != null)
            heldModel.SetActive(hideModelWhenOff ? lit : true);
    }
}
