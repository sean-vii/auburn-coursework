using UnityEngine;

// Sits on each harvestable resource (apple tree, berry bush, stick pile). It says WHICH item it
// gives and how much, how many harvests it has left, and which animation the player plays. It's a
// TAP interaction: one press of E collects into the backpack. PlayerInteraction drives it.
public class InteractableResource : MonoBehaviour, IInteractable
{
    [Header("What this resource gives")]
    [Tooltip("The item added to the backpack when harvested (an ItemDefinition asset).")]
    public ItemDefinition item;
    [Tooltip("How many units are added each harvest.")]
    public int amountPerCollect = 1;
    [Tooltip("How many times this can be harvested before it's empty.")]
    public int usesRemaining = 1;

    [Header("Interaction")]
    [Tooltip("Animator trigger the player fires for this resource, e.g. \"PickFruit\". Empty = none.")]
    public string animationTrigger = "PickFruit";
    [Tooltip("Disappear once its uses run out.")]
    public bool destroyWhenEmpty = true;

    // --- IInteractable ---
    public InteractionKind Kind => InteractionKind.Tap;
    public float HoldDuration => 0f;
    public string AnimationTrigger => animationTrigger;
    public bool CanInteract => usesRemaining > 0;
    public Vector3 Position => transform.position;

    public string Prompt
    {
        get
        {
            string name = item != null ? item.displayName : "item";
            return "Press E to gather " + name;
        }
    }

    // Called when the player finishes the tap interaction on this resource.
    public InteractionResult Interact(GameObject interactor)
    {
        if (usesRemaining <= 0)
            return InteractionResult.Fail();

        // Find the player's backpack and try to add the item — this can be refused if the pack is
        // too full by weight, in which case we DON'T spend a use (you can come back for it).
        Backpack pack = interactor != null ? interactor.GetComponentInParent<Backpack>() : null;
        if (pack == null)
            return InteractionResult.Fail("No backpack");

        int added = pack.TryAdd(item, amountPerCollect);
        if (added <= 0)
            return InteractionResult.Fail("Pack full");

        usesRemaining--;

        // Used up: hide it (also drops its collider out of the player's detection).
        if (usesRemaining <= 0 && destroyWhenEmpty)
            gameObject.SetActive(false);

        string name = item != null ? item.displayName : "item";
        return InteractionResult.Ok("+" + added + " " + name);
    }
}
