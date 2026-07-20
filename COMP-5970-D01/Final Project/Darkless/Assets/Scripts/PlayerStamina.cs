using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

// Darkless — player stamina. Running costs stamina; you can't run forever, so every chase has to be
// ENDED, not outlasted (GDD §6).
//
//   * Sprinting (holding Shift while moving) DRAINS stamina.
//   * Walking (moving, not sprinting) refills it VERY slowly — a gentle top-up, not the real source.
//   * Eating food RESTORES a chunk (InventoryMenu.Eat -> Restore(item.staminaValue)).
//   * When stamina runs low the player is EXHAUSTED and can't sprint until it recovers past a higher
//     threshold (hysteresis, so running doesn't flicker on/off at the edge).
//
// It gates running by owning the StarterAssets `sprint` flag each frame, so nothing in the
// StarterAssets controller needs editing. Put this on the PlayerArmature (next to
// StarterAssetsInputs / ThirdPersonController).
public class PlayerStamina : MonoBehaviour
{
    [Header("Capacity")]
    public float maxStamina = 100f;

    [Header("Rates (per second)")]
    [Tooltip("Stamina drained per second while sprinting.")]
    public float runDrainPerSecond = 4f;
    [Tooltip("Stamina regained per second while WALKING (moving, not sprinting). Keep this VERY " +
             "small — walking only trickles it back; food is the real refill.")]
    public float walkRegenPerSecond = 1.5f;

    [Header("Exhaustion (hysteresis)")]
    [Tooltip("At or below this, the player becomes EXHAUSTED and can't sprint. Keep at 0 so the bar " +
             "drains all the way to empty before exhaustion kicks in.")]
    public float exhaustedAt = 0f;
    [Tooltip("Once exhausted, stamina must climb back to at least this before sprinting is allowed again.")]
    public float recoverTo = 20f;

    [Header("Detection")]
    [Tooltip("Minimum move-input magnitude to count as 'moving' (walking).")]
    public float moveDeadzone = 0.1f;

    // 0..1 for the UI bar. Public so StaminaBar (and anything else) can read it.
    public float Stamina01 => maxStamina > 0f ? Mathf.Clamp01(current / maxStamina) : 0f;
    public bool IsExhausted => exhausted;

    float current;
    bool exhausted;
    StarterAssetsInputs input;

    void Start()
    {
        current = maxStamina;
        input = GetComponent<StarterAssetsInputs>();
    }

    void Update()
    {
        if (input == null) { input = GetComponent<StarterAssetsInputs>(); if (input == null) return; }

        bool moving = input.move.sqrMagnitude > moveDeadzone * moveDeadzone;
        bool wantsRun = WantsRun();
        bool canRun = !exhausted && current > 0f;
        bool running = wantsRun && moving && canRun;

        // We OWN the sprint flag: this both blocks running when exhausted and lets it resume the
        // instant stamina recovers (even if Shift is held the whole time).
        input.sprint = running;

        if (running)
            current -= runDrainPerSecond * Time.deltaTime;
        else if (moving)
            current += walkRegenPerSecond * Time.deltaTime; // slow walking top-up

        current = Mathf.Clamp(current, 0f, maxStamina);

        // Hysteresis: fall into exhaustion at the low mark, climb out only at the higher mark.
        if (!exhausted && current <= exhaustedAt) exhausted = true;
        else if (exhausted && current >= recoverTo) exhausted = false;
    }

    // Read the run key directly (Sprint is bound to Shift), so we control sprint fully each frame.
    bool WantsRun()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null) return kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
        return input != null && input.sprint; // fallback (e.g. gamepad)
    }

    // Called by InventoryMenu when the player eats food.
    public void Restore(float amount)
    {
        current = Mathf.Clamp(current + Mathf.Max(0f, amount), 0f, maxStamina);
    }
}
