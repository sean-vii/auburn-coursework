using UnityEngine;

// How the player triggers an interaction:
//   Tap  -> a single press of E (pick an apple, feed the fire).
//   Hold -> press AND HOLD E for a length of time (search a leaf pile). Letting go or walking
//           away cancels it.
public enum InteractionKind { Tap, Hold }

// The result of an interaction, so the player script can flash quick feedback ("Pack full").
public struct InteractionResult
{
    public bool success;
    public string message;   // optional note to show the player (may be null)

    public static InteractionResult Ok(string message = null)
        => new InteractionResult { success = true, message = message };
    public static InteractionResult Fail(string message = null)
        => new InteractionResult { success = false, message = message };
}

// Anything the player can walk up to and interact with implements this: harvestable resources,
// searchable leaf piles, the campfire's "feed the fire" action. PlayerInteraction finds the
// nearest one and handles the prompt, the tap/hold input, and the animation the same way for all
// of them — so adding a new interactable is just writing a new class that implements this.
public interface IInteractable
{
    // Tap or Hold.
    InteractionKind Kind { get; }
    // For Hold interactions: seconds E must be held. Ignored for Tap.
    float HoldDuration { get; }
    // Text for the on-screen prompt, e.g. "Press E to gather Apple" / "Hold E to search".
    string Prompt { get; }
    // Optional Animator trigger the player fires (e.g. "PickFruit"). Empty = no animation.
    string AnimationTrigger { get; }
    // False when it's used up / not currently usable, so the player ignores it.
    bool CanInteract { get; }
    // World position, used to pick the nearest interactable and to check the player is still close.
    Vector3 Position { get; }
    // Do the thing. 'interactor' is the player GameObject (so we can reach its Backpack).
    InteractionResult Interact(GameObject interactor);
}
