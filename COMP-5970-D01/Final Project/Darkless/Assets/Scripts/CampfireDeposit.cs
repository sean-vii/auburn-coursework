using System.Collections.Generic;
using UnityEngine;

// Lets the player walk up to the campfire and press E to dump their gathered WOOD into it as fuel.
// This is the real replacement for the campfire's temporary "R to refuel" test key: now the actual
// sticks and logs in the backpack feed the fire (each Fuel item adds its own fuelValue, so a log
// is worth more than a stick). Put this on the campfire, next to the Campfire + LightZone.
[RequireComponent(typeof(Campfire))]
public class CampfireDeposit : MonoBehaviour, IInteractable
{
    [Tooltip("Prompt shown when the player is near the fire.")]
    public string prompt = "Press E to feed the fire";

    Campfire campfire;

    void Awake()
    {
        campfire = GetComponent<Campfire>();
    }

    // --- IInteractable ---
    public InteractionKind Kind => InteractionKind.Tap;
    public float HoldDuration => 0f;
    public string Prompt => prompt;
    public string AnimationTrigger => "";   // no animation for feeding the fire (for now)
    public bool CanInteract => true;
    public Vector3 Position => transform.position;

    public InteractionResult Interact(GameObject interactor)
    {
        Backpack pack = interactor != null ? interactor.GetComponentInParent<Backpack>() : null;
        if (pack == null)
            return InteractionResult.Fail("No backpack");

        // Collect every Fuel stack first (we can't remove from the pack while looping over it).
        var fuelStacks = new List<KeyValuePair<ItemDefinition, int>>();
        foreach (var pair in pack.Items)
            if (pair.Key != null && pair.Key.category == ItemCategory.Fuel && pair.Value > 0)
                fuelStacks.Add(pair);

        float totalFuel = 0f;
        int woodUnits = 0;
        foreach (var stack in fuelStacks)
        {
            int removed = pack.Remove(stack.Key, stack.Value);
            totalFuel += stack.Key.fuelValue * removed;
            woodUnits += removed;
        }

        if (woodUnits <= 0)
            return InteractionResult.Fail("No wood to add");

        campfire.AddFuel(totalFuel);
        return InteractionResult.Ok("Added " + woodUnits + " wood to the fire");
    }
}
