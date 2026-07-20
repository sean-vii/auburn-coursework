using UnityEngine;

// Darkless — the escape. The car at camp is the win object (GDD §3, §13). You walk up and press E to
// "try your keys." If your backpack holds the key that fits THIS car, you escape (WinScreen). If you
// have keys but none fit, it tells you so; if you have no keys at all, it nudges you to go search.
//
// It's a Tap IInteractable, exactly like feeding the campfire (CampfireDeposit) — PlayerInteraction
// finds it, shows the prompt, and calls Interact() on the E press.
//
// MVP (now): one designated `correctKey` — assign the CarKey item asset. Phase 5 twist (later): a
// small manager will pick which of several key ItemDefinitions is `correctKey` at random each run,
// and hand the others out to search spots as red herrings. Nothing here changes for that — the twist
// just sets `correctKey` differently at Start.
[RequireComponent(typeof(Collider))]
public class Car : MonoBehaviour, IInteractable
{
    [Header("Win condition")]
    [Tooltip("The ONE key that actually starts this car. Point it at the CarKey item asset. " +
             "Any other key the player finds is a red herring that won't fit.")]
    public ItemDefinition correctKey;

    [Tooltip("The victory screen to show on escape. Left empty, it's found automatically in the scene.")]
    public WinScreen winScreen;

    [Header("Prompts / feedback")]
    public string prompt = "Press E to try your keys";
    [Tooltip("Shown when you try the car with keys that don't fit.")]
    public string wrongKeyMessage = "None of these keys fit.";
    [Tooltip("Shown when you try the car with no keys at all.")]
    public string noKeysMessage = "The car is locked. You need to find the keys.";
    [Tooltip("Shown for a beat as you escape, before the win screen fades up.")]
    public string successMessage = "The key turns — the engine catches!";

    bool won;

    void Awake()
    {
        if (winScreen == null)
            winScreen = FindFirstObjectByType<WinScreen>();
    }

    // --- IInteractable ---
    public InteractionKind Kind => InteractionKind.Tap;
    public float HoldDuration => 0f;
    public string Prompt => prompt;
    public string AnimationTrigger => "";     // no body animation for getting in the car
    public bool CanInteract => !won;
    public Vector3 Position => transform.position;

    public InteractionResult Interact(GameObject interactor)
    {
        if (won) return InteractionResult.Fail();

        Backpack pack = interactor != null ? interactor.GetComponentInParent<Backpack>() : null;

        // Do we hold the key that fits THIS car?
        if (correctKey != null && pack != null && pack.Count(correctKey) > 0)
        {
            won = true;
            if (winScreen != null) winScreen.Show();
            else Debug.LogWarning("Car: correct key used but no WinScreen in the scene to show!");
            return InteractionResult.Ok(successMessage);
        }

        // No win — figure out which "it didn't work" message to give.
        bool hasAnyKey = pack != null && HasAnyKey(pack);
        return InteractionResult.Fail(hasAnyKey ? wrongKeyMessage : noKeysMessage);
    }

    // True if the pack holds at least one item of the Key category (a red herring or the real one).
    static bool HasAnyKey(Backpack pack)
    {
        foreach (var pair in pack.Items)
            if (pair.Key != null && pair.Key.category == ItemCategory.Key && pair.Value > 0)
                return true;
        return false;
    }
}
