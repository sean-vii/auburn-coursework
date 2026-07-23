using UnityEngine;

// The camp CRAFT STATION — the backpack propped against a log by the fire. Torches are made HERE
// (crafting is a camp ritual, not a field freebie). TAP E to spend sticks and drop a TORCH ITEM into
// your backpack; you then EQUIP the torch from the inventory menu (Tab) to actually light it.
//
// It's a Tap IInteractable, exactly like the Car and the campfire's "feed the fire" action, so it
// needs a Collider for PlayerInteraction to detect it.
[RequireComponent(typeof(Collider))]
public class CraftStation : MonoBehaviour, IInteractable
{
    [Header("Recipe")]
    [Tooltip("The Stick item consumed. Point at Assets/Items/Stick.asset.")]
    public ItemDefinition stickItem;
    [Tooltip("How many sticks one torch costs.")]
    public int sticksPerTorch = 3;
    [Tooltip("The Torch item produced into the pack. Point at Assets/Items/Torch.asset.")]
    public ItemDefinition torchItem;
    [Tooltip("The player's backpack. Auto-found if empty.")]
    public Backpack backpack;

    [Header("Prompts / feedback")]
    public string prompt = "Press E to make a torch";
    public string madeMessage = "Made a torch — equip it from your pack (Tab).";
    public string needSticksMessage = "I need {0} sticks to make a torch.";
    public string packFullMessage = "No room in my pack for a torch.";

    void Awake()
    {
        if (backpack == null) backpack = FindFirstObjectByType<Backpack>();
    }

    // --- IInteractable ---
    public InteractionKind Kind => InteractionKind.Tap;
    public float HoldDuration => 0f;
    public string Prompt => prompt;
    public string AnimationTrigger => "";      // no body animation for rummaging the pack
    public bool CanInteract => true;
    public Vector3 Position => transform.position;

    public InteractionResult Interact(GameObject interactor)
    {
        if (backpack == null)
            backpack = interactor != null ? interactor.GetComponentInParent<Backpack>() : FindFirstObjectByType<Backpack>();
        if (backpack == null || torchItem == null)
            return InteractionResult.Fail("There's nothing to craft with.");

        // Enough sticks?
        if (stickItem != null && backpack.Count(stickItem) < sticksPerTorch)
            return InteractionResult.Fail(string.Format(needSticksMessage, sticksPerTorch));

        // Will the finished torch fit (a torch has weight)?
        if (backpack.SpaceFor(torchItem) < 1)
            return InteractionResult.Fail(packFullMessage);

        if (stickItem != null) backpack.Remove(stickItem, sticksPerTorch);
        backpack.TryAdd(torchItem, 1);
        return InteractionResult.Ok(madeMessage);
    }
}
