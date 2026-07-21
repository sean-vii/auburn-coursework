using UnityEngine;

// Darkless — the rectangular PLAYABLE REGION of the level, on the XZ (ground) plane. This one object
// is the single source of truth for two things:
//   1. The handheld MAP's coordinate frame (world position -> a 0..1 spot on the drawn map art).
//   2. The OUT-OF-BOUNDS boundary (leave the box -> you die; see OutOfBounds.cs).
//
// Put this on ONE empty GameObject placed at the CENTER of your play area (e.g. "PlayArea"). Set its
// width/depth in the Inspector; the yellow wire box in the Scene view shows exactly where the edge is.
// Draw your map art to match this box (square recommended, up = north = world +Z).
[DisallowMultipleComponent]
public class PlayArea : MonoBehaviour
{
    // The one PlayArea in the scene, so other scripts (the map, the out-of-bounds check) can find it
    // cheaply without a per-frame search.
    public static PlayArea Instance { get; private set; }

    [Header("Size (world units, centered on this object)")]
    [Tooltip("Full width (X) and depth (Z) of the play area, in world units. The box is centered on " +
             "this object's position. Keep it SQUARE (x == y) to match a square map image.")]
    public Vector2 size = new Vector2(200f, 200f);

    [Tooltip("Extra distance OUTSIDE the box the player may stray before actually dying. 0 = die right " +
             "at the edge. A few metres of slack can feel fairer than an invisible hard wall.")]
    public float deathMargin = 0f;

    void Awake() { Instance = this; }
    void OnEnable() { Instance = this; }

    // Center of the box (this object's world position).
    public Vector3 Center => transform.position;

    // Is a world position inside the play area? 'extraMargin' widens the box (used for the death slack).
    public bool Contains(Vector3 world, float extraMargin = 0f)
    {
        Vector3 c = Center;
        float hx = size.x * 0.5f + extraMargin;
        float hz = size.y * 0.5f + extraMargin;
        return Mathf.Abs(world.x - c.x) <= hx && Mathf.Abs(world.z - c.z) <= hz;
    }

    // True once the player has strayed past the death boundary (edge + deathMargin).
    public bool IsOutOfBounds(Vector3 world) => !Contains(world, deathMargin);

    // How far INSIDE the play area a position is from the NEAREST edge, in world units: positive =
    // inside by this much, 0 = on the edge, negative = outside past it. Used to warn the player with a
    // subtitle as they approach the boundary.
    public float InsideDistanceToEdge(Vector3 world)
    {
        Vector3 c = Center;
        float dx = size.x * 0.5f - Mathf.Abs(world.x - c.x);
        float dz = size.y * 0.5f - Mathf.Abs(world.z - c.z);
        return Mathf.Min(dx, dz);
    }

    // Map a world position to 0..1 across the play area for the map:
    //   (0,0) = the -X,-Z corner (south-west),  (1,1) = the +X,+Z corner (north-east).
    // Values outside [0,1] mean the position is outside the area.
    public Vector2 WorldToNormalized(Vector3 world)
    {
        Vector3 c = Center;
        float nx = (world.x - (c.x - size.x * 0.5f)) / Mathf.Max(0.0001f, size.x);
        float nz = (world.z - (c.z - size.y * 0.5f)) / Mathf.Max(0.0001f, size.y);
        return new Vector2(nx, nz);
    }

    // Draw the boundary in the Scene view so it's easy to size the play area (yellow = edge / map frame,
    // red = the death line if deathMargin > 0). Y height is cosmetic — only X/Z matter.
    void OnDrawGizmos()
    {
        Vector3 s = new Vector3(size.x, 2f, size.y);
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(transform.position, s);
        if (deathMargin > 0f)
        {
            Gizmos.color = new Color(1f, 0.25f, 0.2f, 0.85f);
            Gizmos.DrawWireCube(transform.position, s + new Vector3(deathMargin * 2f, 0f, deathMargin * 2f));
        }
    }
}
