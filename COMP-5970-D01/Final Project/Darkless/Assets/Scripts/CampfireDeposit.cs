using System.Collections.Generic;
using UnityEngine;

// Lets the player walk up to the campfire and press E to dump LOGS from the backpack into it as fuel.
// (Sticks are NOT used here — they're reserved for crafting torches; only logs feed the fire.) Each
// log adds its fuelValue in SECONDS of illumination, and logs are fed one at a time only until the
// fire hits its 5-minute cap, so you never waste logs overfilling it.
//
// Put this on the campfire, next to the Campfire + LightZone.
[RequireComponent(typeof(Campfire))]
public class CampfireDeposit : MonoBehaviour, IInteractable
{
    [Tooltip("Prompt shown when the player is near the fire.")]
    public string prompt = "Press E to feed the fire";
    [Tooltip("Which items count as fire fuel. Assign the Log item. Leave EMPTY to accept any " +
             "Fuel-category item (the old behaviour, which would also burn sticks).")]
    public List<ItemDefinition> acceptedFuel = new List<ItemDefinition>();

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

    // Is this item allowed as fire fuel? Configured list wins; empty list = any Fuel-category item.
    bool Accepts(ItemDefinition it)
    {
        if (it == null) return false;
        if (acceptedFuel != null && acceptedFuel.Count > 0) return acceptedFuel.Contains(it);
        return it.category == ItemCategory.Fuel;
    }

    public InteractionResult Interact(GameObject interactor)
    {
        Backpack pack = interactor != null ? interactor.GetComponentInParent<Backpack>() : null;
        if (pack == null)
            return InteractionResult.Fail("No backpack");

        if (campfire.IsFull)
            return InteractionResult.Fail("The fire is already roaring.");

        // Snapshot the accepted fuel stacks (can't remove from the pack while looping over it).
        var fuelStacks = new List<KeyValuePair<ItemDefinition, int>>();
        foreach (var pair in pack.Items)
            if (Accepts(pair.Key) && pair.Value > 0)
                fuelStacks.Add(pair);

        // Feed logs ONE AT A TIME, stopping the moment the fire hits its cap so nothing is wasted.
        int added = 0;
        foreach (var stack in fuelStacks)
        {
            for (int i = 0; i < stack.Value; i++)
            {
                if (campfire.IsFull) break;
                if (pack.Remove(stack.Key, 1) <= 0) break;
                campfire.AddFuel(stack.Key.fuelValue);
                added++;
            }
            if (campfire.IsFull) break;
        }

        if (added <= 0)
            return InteractionResult.Fail("No logs to add");

        return InteractionResult.Ok("Added " + added + (added == 1 ? " log" : " logs") + " to the fire");
    }
}
