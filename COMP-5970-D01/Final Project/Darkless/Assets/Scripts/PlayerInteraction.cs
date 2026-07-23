using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

// Lives on the player. Each frame it finds the nearest interactable in range (a harvestable
// resource, a searchable leaf pile, the campfire) and shows a prompt. Interactions come in two
// flavours, decided by the interactable itself:
//   * TAP  -> one press of E resolves it immediately (pick fruit, feed the fire).
//   * HOLD -> hold E for a few seconds (searching a leaf pile). A progress bar fills, and letting
//             go or walking away cancels it.
//
// Input: new Input System with PlayerInput "Send Messages", so the press callback MUST be named
// OnInteract. The HELD state (for searching) is read straight from the Interact action each frame.
public class PlayerInteraction : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("How close the player must be for something to be interactable.")]
    public float interactionRange = 3f;
    [Tooltip("Require the player to be LOOKING at the thing (within Max Look Angle of where the camera " +
             "is aimed), not just near it. When several are in view, the one most centered on your aim " +
             "wins. Turn off to fall back to 'nearest in range regardless of facing'.")]
    public bool requireLookingAt = true;
    [Tooltip("Max angle (degrees) between the camera's aim and the direction to the thing for it to " +
             "count as 'looked at'. Smaller = you must aim more precisely at it.")]
    public float maxLookAngle = 30f;
    [Tooltip("The prompt text (starts disabled). Shows e.g. 'Hold E to search'.")]
    public TMP_Text promptText;

    [Header("Timing")]
    [Tooltip("Wait before a TAP interaction resolves. This is also how long the first-person pickup " +
             "arm stays on screen (viewmodel is shown for collectDelay + cooldown), so raise it to " +
             "let the pick animation play longer.")]
    public float collectDelay = 1.35f;
    [Tooltip("Cooldown after an interaction before another can start.")]
    public float cooldown = 0.3f;
    [Tooltip("How long a feedback message (e.g. 'Pack full') stays on screen.")]
    public float messageDuration = 1.2f;

    [Header("Animation")]
    [Tooltip("Also play the pickup/search animation on the player's OWN body. Leave this OFF for " +
             "first-person: the main body keeps its walking/idle locomotion and the pickup plays " +
             "ONLY on the first-person hands viewmodel (FirstPersonPickupHands listens to the " +
             "InteractionStarted event). Turn it ON only for a third-person view where you want to " +
             "see the whole body perform the action.")]
    public bool animatePickupOnMainBody = false;

    // The interactable we're currently near (or null).
    IInteractable current;
    Animator animator;
    Camera cam;                   // the first-person camera, for the "looking at it" check
    InputAction interactAction;   // the Interact (E) action, so we can read the HELD state
    bool isInteracting;           // true while a TAP interaction's coroutine is running

    // Hold-to-search progress, 0..1. -1 means "not holding / hide the bar". Read by the progress UI.
    public float HoldProgress01 { get; private set; } = -1f;

    // True while the player is busy with an interaction (a tap animation playing, or a hold in
    // progress). A camera rig / hands viewmodel reads this to react to the action.
    public bool IsBusy => isInteracting || HoldProgress01 >= 0f;

    // Fired the moment an interaction animation starts, carrying its Animator trigger name
    // (e.g. "PickFruit"). The first-person hands viewmodel listens so it can play the SAME
    // animation in front of the camera.
    public event System.Action<string> InteractionStarted;

    float holdTimer;
    float messageTimer;           // >0 while a feedback message is showing
    string overrideMessage;       // the feedback message, shown instead of the normal prompt

    void Start()
    {
        animator = GetComponent<Animator>();

        // Grab the Interact action so Update can ask "is E held right now?" for hold interactions.
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
            interactAction = playerInput.actions["Interact"];

        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    void Update()
    {
        FindNearby();

        bool held = interactAction != null && interactAction.IsPressed();
        HandleHold(held);

        UpdatePrompt();
    }

    // Finds the best usable interactable: in range AND (if required) the one you're LOOKING at.
    void FindNearby()
    {
        if (cam == null) cam = Camera.main;   // the first-person camera; may spawn after us

        Collider[] hits = Physics.OverlapSphere(transform.position, interactionRange);

        IInteractable best = null;
        // Lower score = better. When "looking at" is required the score is the ANGLE off your aim (so
        // the most-centered thing wins); otherwise it's plain distance (nearest wins).
        float bestScore = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            // GetComponentInParent so the collider can live on a child mesh of the interactable.
            IInteractable it = hit.GetComponentInParent<IInteractable>();
            if (it == null || !it.CanInteract)
                continue;

            // Must be genuinely within range (OverlapSphere is bounds-based; re-check the real point).
            if (Vector3.Distance(transform.position, it.Position) > interactionRange)
                continue;

            float score;
            if (requireLookingAt && cam != null)
            {
                // You must be aiming roughly AT it: the angle between the camera's forward and the
                // direction to the thing must be within maxLookAngle.
                Vector3 toIt = it.Position - cam.transform.position;
                if (toIt.sqrMagnitude < 1e-4f) score = 0f;   // basically on top of it
                else
                {
                    float ang = Vector3.Angle(cam.transform.forward, toIt);
                    if (ang > maxLookAngle) continue;         // not looking at it -> ignore
                    score = ang;                              // prefer whatever is most centered
                }
            }
            else
            {
                score = Vector3.Distance(transform.position, it.Position); // nearest
            }

            if (score < bestScore) { bestScore = score; best = it; }
        }

        // If we moved / looked off the thing we were searching, cancel the in-progress hold.
        if (!ReferenceEquals(best, current))
            CancelHold();

        current = best;
    }

    // Advances a hold-to-search interaction while E is held on a Hold target.
    void HandleHold(bool held)
    {
        if (isInteracting) { CancelHold(); return; }

        bool holdingValidTarget = current != null && current.Kind == InteractionKind.Hold && held;
        if (!holdingValidTarget) { CancelHold(); return; }

        // Kick the search animation on the first frame of the hold.
        if (holdTimer <= 0f)
        {
            // Main body only plays it if explicitly opted in (off by default -> keeps walking/idle).
            if (animatePickupOnMainBody && animator != null && !string.IsNullOrEmpty(current.AnimationTrigger))
                animator.SetTrigger(current.AnimationTrigger);
            // Always tell the first-person hands viewmodel to play it in front of the camera.
            InteractionStarted?.Invoke(current.AnimationTrigger);
        }

        holdTimer += Time.deltaTime;
        float dur = Mathf.Max(0.01f, current.HoldDuration);
        HoldProgress01 = Mathf.Clamp01(holdTimer / dur);

        if (holdTimer >= dur)
        {
            Complete(current);
            CancelHold();
        }
    }

    void CancelHold()
    {
        holdTimer = 0f;
        HoldProgress01 = -1f;
    }

    // Shows the right prompt / feedback message.
    void UpdatePrompt()
    {
        if (promptText == null) return;

        // A feedback message ("Pack full", "+1 Apple") takes priority for a moment.
        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
            promptText.text = overrideMessage;
            promptText.gameObject.SetActive(true);
            return;
        }

        // While a tap interaction plays, its coroutine owns the screen.
        if (isInteracting)
            return;

        if (current != null)
        {
            promptText.text = current.Prompt;
            promptText.gameObject.SetActive(true);
        }
        else
        {
            promptText.gameObject.SetActive(false);
        }
    }

    // Fired by the Input System (Send Messages) when Interact (E) is pressed or released. We only
    // act on the press edge, and only for TAP interactions — HOLD is handled in Update while held.
    public void OnInteract(InputValue value)
    {
        if (!value.isPressed) return;
        if (current == null || isInteracting) return;
        if (current.Kind != InteractionKind.Tap) return;

        StartCoroutine(TapRoutine(current));
    }

    // Plays the animation, waits, then resolves a TAP interaction.
    IEnumerator TapRoutine(IInteractable target)
    {
        isInteracting = true;

        // Main body only plays it if explicitly opted in (off by default -> keeps walking/idle).
        if (animatePickupOnMainBody && animator != null && !string.IsNullOrEmpty(target.AnimationTrigger))
            animator.SetTrigger(target.AnimationTrigger);
        // Always tell the first-person hands viewmodel to play it in front of the camera.
        InteractionStarted?.Invoke(target.AnimationTrigger);

        yield return new WaitForSeconds(collectDelay);

        // The player may have walked away during the animation, so re-check before resolving.
        if (target != null && target.CanInteract &&
            Vector3.Distance(transform.position, target.Position) <= interactionRange)
        {
            Complete(target);
        }

        yield return new WaitForSeconds(cooldown);
        isInteracting = false;
    }

    // Runs the interactable's effect and flashes its feedback message.
    void Complete(IInteractable target)
    {
        InteractionResult result = target.Interact(gameObject);
        // One SFX hook for EVERY interaction: a confirm on success, a deny buzz on failure.
        if (result.success) Sfx.Confirm(); else Sfx.Deny();
        if (!string.IsNullOrEmpty(result.message))
        {
            overrideMessage = result.message;
            messageTimer = messageDuration;
        }
    }
}
