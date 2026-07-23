using UnityEngine;

// A held TORCH — the player's everyday light. A torch is a CONSUMABLE ITEM crafted at camp (see
// CraftStation) and carried in the Backpack; you EQUIP one from the inventory menu (Tab) to light it.
// You can hold only ONE light at a time: equipping a torch puts the flashlight away, and pressing F
// for the flashlight puts the torch away — you then re-equip the torch from your pack.
//
// LIFESPAN: a lit torch lasts 'maxLife' seconds (39), split into 'tiers' stages (3 x 13s). It dims a
// stage at a time as it burns and, when spent, is gone (equip another from the pack). Life only ticks
// while the torch is actually held/lit — switching to the flashlight PAUSES it, and re-equipping
// resumes the SAME torch without spending another.
//
// While lit it counts as safe light via a LightZone (keeps the darkness meter up, slows the Mimic).
// Put this on a Torch object that is a CHILD of the player's camera root (next to the Flashlight).
[RequireComponent(typeof(LightZone))]
public class Torch : MonoBehaviour
{
    [Header("Lifespan (3 tiers x 13s = 39s)")]
    [Tooltip("Total seconds a torch stays lit before it's spent.")]
    public float maxLife = 78f;
    [Tooltip("Brightness tiers the life is split into (the torch dims a step at each). 3 tiers over " +
             "39s = 13s per tier.")]
    [Range(1, 6)] public int tiers = 3;

    [Header("Safe light")]
    [Tooltip("LightZone radius while lit. This is the SAFE radius that keeps the darkness meter topped " +
             "up — it stays CONSTANT while lit (like the flashlight), so a lit torch always protects you. " +
             "Only the VISIBLE light (below) dims per tier.")]
    public float onRadius = 7f;

    [Header("Fire VFX (Full Opaque Fire on the torch tip)")]
    [Tooltip("The VFX_FireController on the torch's flame. Driven on/off + dimmed per tier with the torch.")]
    public YourNamespace.VFX_FireController fireVfx;
    [Tooltip("Flame intensity at full brightness (scales down per tier).")]
    public float fireIntensity = 0.6f;
    [Tooltip("Point-light brightness at full brightness (scales down per tier).")]
    public float lightIntensity = 45f;
    [Tooltip("Point-light reach at full brightness (scales down per tier).")]
    public float lightRange = 16f;

    [Header("Held model")]
    [Tooltip("The visible torch mesh in the player's hand.")]
    public GameObject heldModel;
    [Tooltip("Hide the torch model while unlit (only appears in hand while held). Off = always held.")]
    public bool hideModelWhenOff = true;

    [Header("Item / links")]
    [Tooltip("The Torch item asset consumed from the pack when you equip a FRESH torch.")]
    public ItemDefinition torchItem;
    [Tooltip("The player's backpack. Auto-found if empty.")]
    public Backpack backpack;
    [Tooltip("The flashlight, so equipping a torch can put it away (one light at a time). Auto-found.")]
    public Flashlight flashlight;

    [Header("Messages")]
    public string equippedMessage = "Torch lit.";
    public string relitMessage = "Torch relit.";
    public string noTorchMessage = "No torch in my pack.";
    public string spentMessage = "My torch burned out.";

    [Header("State (read-only)")]
    [Tooltip("Currently holding + burning this torch.")]
    public bool held = false;
    [Tooltip("Seconds of life left on the CURRENT torch (0 = none in hand).")]
    public float lifeLeft = 0f;

    LightZone zone;
    Light fireLight;

    // 1 = fresh, 0 = spent. Read by the torch life bar UI.
    public float Life01 => maxLife > 0f ? Mathf.Clamp01(lifeLeft / maxLife) : 0f;
    // True while a lit torch is in hand.
    public bool IsLit => held && lifeLeft > 0f;
    // Current brightness tier, 1..tiers (0 when spent).
    public int Tier => lifeLeft <= 0f ? 0 : Mathf.Clamp(Mathf.CeilToInt(lifeLeft / (maxLife / tiers)), 1, tiers);

    void Awake()
    {
        zone = GetComponent<LightZone>();
        if (fireVfx != null) fireLight = fireVfx.GetComponentInChildren<Light>(true);
        if (backpack == null) backpack = FindFirstObjectByType<Backpack>();
        if (flashlight == null) flashlight = FindFirstObjectByType<Flashlight>();
        ApplyState();
    }

    void Update()
    {
        // A held torch burns down; when spent it's consumed (you must equip another from the pack).
        if (held && lifeLeft > 0f)
        {
            lifeLeft = Mathf.Max(0f, lifeLeft - Time.deltaTime);
            if (lifeLeft <= 0f)
            {
                held = false;
                if (!string.IsNullOrEmpty(spentMessage)) SubtitleUI.Say(spentMessage, 3f);
            }
        }
        ApplyState();
    }

    // Equip / light a torch from the inventory. Puts the flashlight away first (one light at a time).
    // Resumes a partly-burned torch for free; otherwise consumes one Torch item from the pack.
    public bool Equip(out string message)
    {
        // One light at a time — stow the flashlight.
        if (flashlight == null) flashlight = FindFirstObjectByType<Flashlight>();
        if (flashlight != null && flashlight.isOn) flashlight.ForceOff();

        // A partly-burned torch we put away earlier: just pick it back up (no new item spent).
        if (lifeLeft > 0f)
        {
            held = true;
            message = relitMessage;
            return true;
        }

        // Otherwise we need a fresh torch from the pack.
        if (backpack == null) backpack = FindFirstObjectByType<Backpack>();
        if (torchItem != null && backpack != null && backpack.Count(torchItem) > 0)
        {
            backpack.Remove(torchItem, 1);
            lifeLeft = maxLife;
            held = true;
            message = equippedMessage;
            return true;
        }

        message = noTorchMessage;
        return false;
    }

    // Put the torch away WITHOUT losing it — its remaining life is kept, so re-equipping resumes it.
    // Called when the flashlight is switched on (one light at a time).
    public void Unequip()
    {
        held = false;
        ApplyState();
    }

    void ApplyState()
    {
        bool lit = held && lifeLeft > 0f;
        // Brightness fraction steps down a tier at a time (tier/tiers): e.g. 3/3, 2/3, 1/3.
        float f = lit ? (float)Tier / Mathf.Max(1, tiers) : 0f;

        // SAFE radius stays at full whenever lit (like the flashlight) so it reliably keeps the darkness
        // meter topped up — even at the last tier or when the camera-mounted torch swings as you look
        // around. Only the VISIBLE light (VFX + point light below) dims per tier.
        zone.radius = lit ? onRadius : 0f;

        if (fireVfx != null) fireVfx.SetFireIntensity(fireIntensity * f);
        if (fireLight != null)
        {
            fireLight.enabled = lit;
            fireLight.intensity = lightIntensity * f;
            fireLight.range = lightRange * f;
        }

        if (heldModel != null)
            heldModel.SetActive(hideModelWhenOff ? lit : true);
    }
}
