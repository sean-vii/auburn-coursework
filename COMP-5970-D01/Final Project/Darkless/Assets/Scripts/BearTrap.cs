using UnityEngine;

// A bear trap: a STATIC environmental HAZARD you place in the level. The player must watch their step —
// walking onto an armed (open) trap snaps it shut and SNARES the player in place for a few seconds
// (survivable by day, but at night being stuck while the Mimic closes in is often fatal). The Mimic
// ignores traps; this is a player-only hazard and is NOT deployable by the player.
//
// Prefab layout: a root (this component + a TRIGGER collider) with two child visuals —
// 'openVisual' (shown while armed) and 'closedVisual' (shown after it snaps). Drop the prefab into the
// scene wherever you want a trap; duplicate for more.
[RequireComponent(typeof(Collider))]
public class BearTrap : MonoBehaviour
{
    [Header("Effect")]
    [Tooltip("Seconds the player is rooted in place (can't move or jump) after stepping on the trap.")]
    public float snareSeconds = 11f;
    [Tooltip("Inner-voice subtitle shown when caught. Empty = none.")]
    public string caughtMessage = "My leg — I'm caught!";

    [Header("Visuals (assigned on the prefab)")]
    [Tooltip("The OPEN trap model — shown while armed and dangerous.")]
    public GameObject openVisual;
    [Tooltip("The CLOSED (snapped-shut) trap model — shown once it triggers.")]
    public GameObject closedVisual;

    [Header("Re-arm")]
    [Tooltip("Seconds after snapping before the trap re-arms (opens again). 0 = one-shot: it stays shut " +
             "and is safe afterwards.")]
    public float rearmSeconds = 0f;

    [Header("Audio")]
    [Tooltip("The metallic snap played when the trap triggers (Assets/Audio/BearTrap.mp3).")]
    public AudioClip snapClip;
    [Range(0f, 1f)] public float snapVolume = 1f;

    bool armed = true;
    float rearmTimer;

    void Awake()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;   // hazards detect a step-over, they don't block
        SetArmed(true);
    }

    void Update()
    {
        if (!armed && rearmSeconds > 0f)
        {
            rearmTimer -= Time.deltaTime;
            if (rearmTimer <= 0f) SetArmed(true);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!armed) return;
        // Only the PLAYER trips a trap (the Mimic ignores them). Identify the player by its PlayerSlow
        // component (the movement authority, on the PlayerArmature).
        var player = other.GetComponentInParent<PlayerSlow>();
        if (player == null) return;
        Snap(player);
    }

    void Snap(PlayerSlow player)
    {
        armed = false;
        rearmTimer = rearmSeconds;
        SetVisualArmed(false);

        if (snapClip != null)
            AudioSource.PlayClipAtPoint(snapClip, transform.position, snapVolume);

        player.Snare(snareSeconds);

        if (!string.IsNullOrEmpty(caughtMessage))
            SubtitleUI.Say(caughtMessage, Mathf.Max(1.5f, snareSeconds));
    }

    void SetArmed(bool a)
    {
        armed = a;
        SetVisualArmed(a);
    }

    void SetVisualArmed(bool a)
    {
        if (openVisual != null) openVisual.SetActive(a);
        if (closedVisual != null) closedVisual.SetActive(!a);
    }
}
