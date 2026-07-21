using UnityEngine;

// Darkless — the play-area boundary behaves differently by time of day (GDD §12):
//   NIGHT: the dark owns everything past the edge — crossing = instant death (routed through the ONE
//          death path, PlayerDarkness.Die -> freeze/fade game-over), and nearing the edge shakes a
//          warning subtitle.
//   DAY:   safe, but pointless to leave — the player is physically held inside by an invisible "soft
//          wall" (we clamp them back at the edge) and a calm, steady subtitle nudges them to turn back.
//
// Put this on the PlayerArmature, next to PlayerDarkness. Uses LateUpdate so the day clamp runs AFTER
// the character controller has moved this frame.
[RequireComponent(typeof(PlayerDarkness))]
public class OutOfBounds : MonoBehaviour
{
    [Header("Night — the dark kills you")]
    [Tooltip("Message shown on the game-over screen when you cross the boundary AT NIGHT.")]
    public string deathMessage = "You strayed too far from camp. The dark took you.";
    [Tooltip("At night, start the (shaking) warning subtitle when this close (world units) to the edge.")]
    public float warnDistance = 15f;
    [Tooltip("The subtitle shown at night as the player nears the boundary.")]
    [TextArea] public string warnMessage = "This is too far from camp.. I know my keys are somewhere on the map.";

    [Header("Day — a soft wall, no death")]
    [Tooltip("By DAY the player physically can't cross the boundary — they're held just inside it, and " +
             "this steady (no-shake) line nudges them back.")]
    [TextArea] public string dayBlockMessage = "My keys can't be over here, I'm turning back.";
    [Tooltip("How far inside the edge the player is held when blocked (a small inset avoids jitter).")]
    public float blockInset = 0.5f;
    [Tooltip("Clear the day 'turning back' line once the player walks back at least this far from the edge.")]
    public float clearDistance = 3f;

    PlayerDarkness darkness;
    DayNightCycle dayNight;
    bool warning;   // night: approach-warning subtitle is up
    bool blocked;   // day: soft-wall subtitle is up

    void Awake() { darkness = GetComponent<PlayerDarkness>(); }
    void Start() { dayNight = Object.FindAnyObjectByType<DayNightCycle>(); }

    // LateUpdate so the DAY soft-wall clamp runs AFTER the character controller has moved this frame.
    void LateUpdate()
    {
        if (darkness == null || darkness.IsDead) return;

        var area = PlayArea.Instance;
        if (area == null) return;

        // Fail to "night" if there's no clock, matching PlayerDarkness / SearchSpot.
        bool night = dayNight == null || dayNight.IsNight;
        if (night) TickNight(area);
        else       TickDay(area);
    }

    // NIGHT: crossing the edge kills you; nearing it shakes a warning subtitle.
    void TickNight(PlayArea area)
    {
        if (blocked) { blocked = false; SubtitleUI.Clear(); }   // leaving the day-block state

        if (area.IsOutOfBounds(transform.position))
        {
            warning = false;
            SubtitleUI.Clear();
            darkness.Die(deathMessage);
            return;
        }

        float dEdge = area.InsideDistanceToEdge(transform.position);
        if (!warning && dEdge <= warnDistance)
        {
            warning = true;
            SubtitleUI.Say(warnMessage, 0f, true);   // shaking
        }
        else if (warning && dEdge > warnDistance + 3f)
        {
            warning = false;
            SubtitleUI.Clear();
        }
    }

    // DAY: an invisible wall — hold the player inside the play area and calmly nudge them to turn back.
    void TickDay(PlayArea area)
    {
        if (warning) { warning = false; SubtitleUI.Clear(); }   // leaving the night-warning state

        Vector3 p = transform.position;
        Vector3 c = area.Center;
        float hx = area.size.x * 0.5f - blockInset;
        float hz = area.size.y * 0.5f - blockInset;
        float cx = Mathf.Clamp(p.x, c.x - hx, c.x + hx);
        float cz = Mathf.Clamp(p.z, c.z - hz, c.z + hz);

        bool hitWall = !Mathf.Approximately(cx, p.x) || !Mathf.Approximately(cz, p.z);
        if (hitWall)
        {
            p.x = cx; p.z = cz;
            transform.position = p;   // teleport back inside — the soft wall
        }

        // Show the steady "turning back" line the moment they press into the wall; clear it once they
        // walk comfortably back inside.
        if (!blocked && hitWall)
        {
            blocked = true;
            SubtitleUI.Say(dayBlockMessage, 0f, false);   // NO shake
        }
        else if (blocked && !hitWall && area.InsideDistanceToEdge(transform.position) > clearDistance)
        {
            blocked = false;
            SubtitleUI.Clear();
        }
    }
}
