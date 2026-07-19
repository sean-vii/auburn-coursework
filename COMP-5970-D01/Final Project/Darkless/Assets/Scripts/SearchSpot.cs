using UnityEngine;

// A searchable spot on the forest floor — a bundle of leaves you HOLD E on for a few seconds to
// search. This is the Darkless "investigate spot" (GDD §13): at night these are where you find the
// car keys. For now it grants a configurable reward item so we can build and test the
// hold-to-search feel; in Phase 5 the reward becomes the randomized keys.
//
// It's a HOLD interaction: PlayerInteraction fills a progress bar while E is held and calls
// Interact() only when the hold finishes. Letting go or walking away cancels the search.
public class SearchSpot : MonoBehaviour, IInteractable
{
    [Header("Search")]
    [Tooltip("Seconds the player must hold E to finish searching this spot.")]
    public float searchDuration = 3f;
    [Tooltip("Prompt shown while standing on the spot.")]
    public string prompt = "Hold E to search";
    [Tooltip("Animator trigger played when the search starts (e.g. a kneel/ground animation). " +
             "Empty = none.")]
    public string animationTrigger = "GatherOre";

    [Header("Reward (placeholder until Phase 5 keys)")]
    [Tooltip("Item granted when the search finishes. Point it at wood/fruit to test now; swap to a " +
             "Key later. Leave empty to grant nothing (just the search action).")]
    public ItemDefinition rewardItem;
    public int rewardAmount = 1;

    [Header("Lifetime")]
    [Tooltip("Can only be searched once, then it's spent.")]
    public bool onceOnly = true;
    [Tooltip("Hide the leaf bundle once searched.")]
    public bool hideWhenSearched = true;

    bool searched;

    // --- IInteractable ---
    public InteractionKind Kind => InteractionKind.Hold;
    public float HoldDuration => searchDuration;
    public string Prompt => prompt;
    public string AnimationTrigger => animationTrigger;
    public bool CanInteract => !(onceOnly && searched);
    public Vector3 Position => transform.position;

    // Called when the player finishes holding E long enough to complete the search.
    public InteractionResult Interact(GameObject interactor)
    {
        if (onceOnly && searched)
            return InteractionResult.Fail();

        searched = true;

        string message = "Searched — nothing here";

        // Grant the reward (if any) into the backpack. Keys are weightless, so they always fit.
        if (rewardItem != null && rewardAmount > 0)
        {
            Backpack pack = interactor != null ? interactor.GetComponentInParent<Backpack>() : null;
            int added = pack != null ? pack.TryAdd(rewardItem, rewardAmount) : 0;
            message = added > 0
                ? "Found " + rewardItem.displayName
                : "Found " + rewardItem.displayName + " — but the pack is full!";
        }

        if (hideWhenSearched)
            gameObject.SetActive(false);

        return InteractionResult.Ok(message);
    }
}
