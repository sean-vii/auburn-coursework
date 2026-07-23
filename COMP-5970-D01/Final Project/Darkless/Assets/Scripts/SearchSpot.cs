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
    [Tooltip("Animator trigger played when the search starts. 'PickStanding' = the arm-extend/reach " +
             "animation (PickFruit_Standing). Empty = none.")]
    public string animationTrigger = "PickStanding";

    [Header("Reward")]
    [Tooltip("Item granted when this search yields the key (the CarKey). Set by the spawner.")]
    public ItemDefinition rewardItem;
    public int rewardAmount = 1;
    [Range(0f, 1f)]
    [Tooltip("Legacy random produce chance — used ONLY when there is no coordinator (a hand-placed " +
             "SearchSpot with no SearchSpotSpawner). With a coordinator, the spawner's key GUARANTEE " +
             "decides the outcome and this is ignored.")]
    public float produceChance = 0.7f;
    [Tooltip("The spawner that coordinates the guaranteed-key logic. Assigned automatically when the " +
             "spot is spawned. If null, this spot falls back to the random produceChance above.")]
    public SearchSpotSpawner coordinator;

    [Header("When")]
    [Tooltip("Keys are only findable at NIGHT (GDD §13). When on, this spot can't be searched during " +
             "the day — no prompt shows until nightfall. Turn off to allow searching any time (testing).")]
    public bool nightOnly = true;

    [Header("Lifetime")]
    [Tooltip("Can only be searched once, then it's spent.")]
    public bool onceOnly = true;
    [Tooltip("Hide the leaf bundle once searched.")]
    public bool hideWhenSearched = true;

    bool searched;
    DayNightCycle dayNight;
    Renderer[] renderers;    // the slime's visuals, hidden during the day
    Collider spotCollider;   // disabled during the day so it can't even be detected
    bool? shownState;        // last-applied visibility, so we only toggle on change

    void Awake()
    {
        // Cache the day/night clock so we can gate searching to nighttime.
        dayNight = FindFirstObjectByType<DayNightCycle>();
        renderers = GetComponentsInChildren<Renderer>(true);
        spotCollider = GetComponent<Collider>();
    }

    void Update()
    {
        // Search spots are a NIGHT phenomenon: they only APPEAR at night (GDD §13). During the day the
        // slime is hidden and can't be detected; at night it shows and becomes searchable. Spots that
        // ignore the night rule (nightOnly off) are always visible.
        if (nightOnly)
            SetVisible(IsNight);
    }

    // Toggle the slime's renderers + collider so it's only present at night.
    void SetVisible(bool visible)
    {
        if (shownState == visible) return;   // no-op unless it actually changed
        shownState = visible;
        if (renderers != null)
            foreach (var r in renderers) if (r != null) r.enabled = visible;
        if (spotCollider != null) spotCollider.enabled = visible;
    }

    // True when it's currently night (or if there's no day/night system to ask — fail open so the
    // spot still works in a bare test scene).
    bool IsNight => dayNight == null || dayNight.IsNight;

    // --- IInteractable ---
    public InteractionKind Kind => InteractionKind.Hold;
    public float HoldDuration => searchDuration;
    public string Prompt => prompt;
    public string AnimationTrigger => animationTrigger;
    // Usable only if not already spent AND (it's night, or this spot ignores the night rule).
    public bool CanInteract => !(onceOnly && searched) && (!nightOnly || IsNight);
    public Vector3 Position => transform.position;

    // Called when the player finishes holding E long enough to complete the search.
    public InteractionResult Interact(GameObject interactor)
    {
        if (onceOnly && searched)
            return InteractionResult.Fail();

        searched = true;

        string message = "Searched — nothing here";

        // What does THIS search yield? With a coordinator (the normal case), the spawner decides WHICH
        // item — the correct car key on the guaranteed search, a random red-herring key on some others
        // (the multi-key twist), or null for "nothing". Without a coordinator (a lone hand-placed spot),
        // fall back to the random produceChance granting this spot's own rewardItem. Keys are weightless.
        ItemDefinition granted;
        if (coordinator != null)
            granted = coordinator.ResolveSearchReward();
        else
            granted = (rewardItem != null && rewardAmount > 0 && Random.value <= produceChance)
                ? rewardItem : null;

        if (granted != null && rewardAmount > 0)
        {
            Backpack pack = interactor != null ? interactor.GetComponentInParent<Backpack>() : null;
            int added = pack != null ? pack.TryAdd(granted, rewardAmount) : 0;
            message = added > 0
                ? "Found " + granted.displayName + "!"
                : "Found " + granted.displayName + " — but the pack is full!";
        }

        if (hideWhenSearched)
            gameObject.SetActive(false);

        return InteractionResult.Ok(message);
    }
}
