using System.Collections.Generic;
using UnityEngine;

// The player's backpack. It holds RESOURCES (sticks, logs, apples, berries) up to a limited TOTAL
// WEIGHT — capacity is the only limit, and a full pack does NOT slow you down (a Darkless design
// rule). Keys don't count toward the weight (they ride on a weightless keyring). Hand-held tools
// (flashlight, torch) are never stored here at all.
//
// Put this on the PlayerArmature. Other systems talk to it: pickups add to it, the campfire pulls
// wood out of it, the UI shows its weight and contents.
public class Backpack : MonoBehaviour
{
    [Header("Capacity")]
    [Tooltip("Maximum total weight the pack can hold. Heavy logs crowd out food, so this is the " +
             "core 'fuel vs. food' budget. Weight never slows the player.")]
    public float weightCapacity = 30f;

    // What's in the pack right now: how many of each item type. A Dictionary is a fast lookup from
    // an item definition to its count.
    readonly Dictionary<ItemDefinition, int> contents = new Dictionary<ItemDefinition, int>();

    // Fired whenever the contents change, so the UI (or anything else) can refresh.
    public event System.Action Changed;

    // Total weight of everything currently in the pack (keys excluded).
    public float CurrentWeight
    {
        get
        {
            float total = 0f;
            foreach (var pair in contents)
                if (pair.Key != null && pair.Key.CountsTowardWeight)
                    total += pair.Key.weightPerUnit * pair.Value;
            return total;
        }
    }

    // 0..1 how full the pack is by weight (for a UI bar).
    public float Fill01 => weightCapacity > 0f ? Mathf.Clamp01(CurrentWeight / weightCapacity) : 0f;

    // How many of this item we're carrying.
    public int Count(ItemDefinition item)
    {
        if (item == null) return 0;
        return contents.TryGetValue(item, out int n) ? n : 0;
    }

    // How many MORE of this item would fit by weight (keys always "fit" — they're weightless).
    public int SpaceFor(ItemDefinition item)
    {
        if (item == null) return 0;
        if (!item.CountsTowardWeight || item.weightPerUnit <= 0f) return int.MaxValue;
        float free = Mathf.Max(0f, weightCapacity - CurrentWeight);
        return Mathf.FloorToInt(free / item.weightPerUnit);
    }

    // Try to add up to 'qty' of an item. Adds as many as fit and returns how many were ACTUALLY
    // added (0 = pack too full). A pickup uses the return value to know if it worked.
    public int TryAdd(ItemDefinition item, int qty)
    {
        if (item == null || qty <= 0) return 0;

        int toAdd = Mathf.Min(qty, SpaceFor(item));
        if (toAdd <= 0) return 0;

        contents[item] = Count(item) + toAdd;
        Changed?.Invoke();
        return toAdd;
    }

    // Remove up to 'qty' of an item; returns how many were actually removed.
    public int Remove(ItemDefinition item, int qty)
    {
        if (item == null || qty <= 0) return 0;

        int toRemove = Mathf.Min(qty, Count(item));
        if (toRemove <= 0) return 0;

        int left = Count(item) - toRemove;
        if (left > 0) contents[item] = left;
        else contents.Remove(item);

        Changed?.Invoke();
        return toRemove;
    }

    // A read-only view of the contents for the UI to list.
    public IEnumerable<KeyValuePair<ItemDefinition, int>> Items => contents;
}
