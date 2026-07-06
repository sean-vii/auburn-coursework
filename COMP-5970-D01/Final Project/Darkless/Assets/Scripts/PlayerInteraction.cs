using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

// Lives on the player. Every frame it finds the nearest harvestable resource in range and
// shows/hides the "Press E" prompt. When the player presses Interact (E), it plays that
// resource's animation, waits, then collects it.
//
// Input: uses the new Input System with the PlayerInput "Send Messages" behavior, so the
// input callback MUST be named OnInteract (On + the action name "Interact").
public class PlayerInteraction : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("How close the player must be for a resource to be detected.")]
    public float interactionRange = 3f;
    [Tooltip("The 'Press E to interact' prompt text (starts disabled).")]
    public TMP_Text promptText;

    [Header("Timing")]
    [Tooltip("Wait before the resource is collected, so the animation has time to play.")]
    public float collectDelay = 0.8f;
    [Tooltip("Cooldown after a harvest before another can start (stops E-spamming).")]
    public float cooldown = 0.3f;

    // The resource we're currently near (or null).
    private InteractableResource currentResource;
    // The player's Animator, used to trigger the harvest animations.
    private Animator animator;
    // True while a harvest is playing, so we don't start a second one on top of it.
    private bool isInteracting;

    void Start()
    {
        animator = GetComponent<Animator>();

        // Prompt stays hidden until we're actually near a resource.
        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    void Update()
    {
        FindNearbyResource();
    }

    // Finds the closest resource within interactionRange and updates the prompt.
    void FindNearbyResource()
    {
        // An invisible sphere around the player returns every collider inside the range.
        Collider[] hits = Physics.OverlapSphere(transform.position, interactionRange);

        InteractableResource closestResource = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            InteractableResource resource = hit.GetComponent<InteractableResource>();

            // Ignore colliders that aren't harvestable resources (terrain, the player, etc.).
            if (resource == null)
                continue;

            float distance = Vector3.Distance(transform.position, resource.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestResource = resource;
            }
        }

        currentResource = closestResource;

        // While harvesting, the coroutine owns the prompt — don't fight it here.
        if (isInteracting)
            return;

        if (promptText != null)
        {
            if (currentResource != null)
            {
                promptText.text = "Press E to interact";
                promptText.gameObject.SetActive(true);
            }
            else
            {
                promptText.gameObject.SetActive(false);
            }
        }
    }

    // Fired by the new Input System (Send Messages) when the Interact action is pressed.
    public void OnInteract(InputValue value)
    {
        // Ignore the key-release half of the press.
        if (!value.isPressed)
            return;

        // Nothing nearby, or we're already mid-harvest -> do nothing.
        if (currentResource == null || isInteracting)
            return;

        StartCoroutine(InteractRoutine());
    }

    // Plays the animation, waits, collects the resource, then cools down.
    IEnumerator InteractRoutine()
    {
        isInteracting = true;

        // Hide the prompt while the action plays.
        if (promptText != null)
            promptText.gameObject.SetActive(false);

        // Fire this resource's own trigger (apple tree -> PickFruit, ore rock -> GatherOre),
        // so the same script plays different animations per resource type.
        if (animator != null && !string.IsNullOrEmpty(currentResource.animationTrigger))
            animator.SetTrigger(currentResource.animationTrigger);

        // Give the animation a moment before the resource is collected/removed.
        yield return new WaitForSeconds(collectDelay);

        // The player may have walked away during the animation, so re-check.
        if (currentResource != null)
            currentResource.Interact();

        // Small cooldown before the next harvest can begin.
        yield return new WaitForSeconds(cooldown);

        isInteracting = false;
    }
}
