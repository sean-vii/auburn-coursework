using UnityEngine;

// What kind of item this is. It decides how the item is USED and whether it takes up pack weight:
//   Food -> eaten for stamina (stored in the backpack, has weight)
//   Fuel -> fed to the campfire / used to build a torch later (stored in the backpack, has weight)
//   Key  -> tried at the car to win (rides weightless on a keyring, NOT in the weighted pack)
public enum ItemCategory { Food, Fuel, Key }

// A data asset describing ONE type of item (an apple, a stick, a log, a key...). Create these via
// right-click in the Project window -> Create -> Darkless -> Item. Because it's a ScriptableObject
// the data lives in a shared asset file, not on any one scene object, so every apple in the world
// points at the same "Apple" definition (one weight, one name, one icon). This is the clean,
// data-driven way to define items in Unity.
[CreateAssetMenu(fileName = "New Item", menuName = "Darkless/Item")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Short unique id, e.g. \"apple\", \"stick\", \"log\". Handy for code/UI lookups.")]
    public string id = "item";
    [Tooltip("Name shown to the player, e.g. \"Apple\".")]
    public string displayName = "Item";
    [Tooltip("Optional icon for the inventory UI. Leave empty and the UI just shows the name.")]
    public Sprite icon;

    [Header("Carrying")]
    [Tooltip("Weight of ONE unit. The backpack has a limited TOTAL weight, so heavy logs crowd out " +
             "food. Keys ignore this (weightless keyring).")]
    public float weightPerUnit = 1f;
    [Tooltip("What this item is — decides how it's used and whether it counts toward pack weight.")]
    public ItemCategory category = ItemCategory.Fuel;

    [Header("Category values")]
    [Tooltip("FUEL only: campfire fuel one unit adds when fed to the fire (sticks small, logs large).")]
    public float fuelValue = 10f;
    [Tooltip("FOOD only: stamina eating one unit restores (used once the stamina system exists).")]
    public float staminaValue = 25f;

    // Keys ride on a weightless keyring; everything else counts toward the pack's weight limit.
    public bool CountsTowardWeight => category != ItemCategory.Key;
}
