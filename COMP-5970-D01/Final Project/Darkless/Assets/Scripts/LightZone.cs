using System.Collections.Generic;
using UnityEngine;

// Marks a GameObject as a source of "safe" light — the campfire for now, and later
// torches, lanterns, the car headlights, etc. Anything standing within `radius` of an
// active LightZone counts as LIT and is safe from the darkness (see PlayerDarkness).
//
// We keep a shared list of every active zone so the player can check "am I lit?" cheaply
// each frame instead of searching the whole scene.
[DisallowMultipleComponent]
public class LightZone : MonoBehaviour
{
    [Tooltip("How far the safe light reaches, in metres. Size it in the Scene view using the " +
             "yellow wire sphere shown when this object is selected.")]
    public float radius = 6f;

    [Header("Effect on the Mimic")]
    [Tooltip("How strongly this light SLOWS the Mimic while it's inside (0 = none, 1 = fully stopped). " +
             "This is the light's 'intensity' as far as the monster is concerned — brighter tools slow " +
             "it more. The flashlight is strong, a torch medium. The slow is temporary (only while the " +
             "Mimic is in the light) and is itself weakened by the monster's aggression.")]
    [Range(0f, 1f)] public float slowStrength = 0.5f;

    [Tooltip("HARD BARRIER: the Mimic will NOT push into this light while attacking — it repositions " +
             "back to the dark instead. Reserve this for the CAMPFIRE (the one true safe zone). Leave " +
             "off for torches / flashlight / lanterns, which only SLOW it.")]
    public bool blocksMimic = false;

    // Every LightZone that is currently enabled. 'static' means one shared list for all of them.
    public static readonly List<LightZone> Active = new List<LightZone>();

    void OnEnable() { Active.Add(this); }
    void OnDisable() { Active.Remove(this); }

    // Is the given world point inside this zone's radius right now?
    public bool Covers(Vector3 point)
    {
        // Compare squared distances — avoids a slower square-root, same result.
        return (transform.position - point).sqrMagnitude <= radius * radius;
    }

    // Is the point lit by ANY active zone in the scene?
    public static bool AnyCovers(Vector3 point)
    {
        for (int i = 0; i < Active.Count; i++)
            if (Active[i] != null && Active[i].Covers(point))
                return true;
        return false;
    }

    // The strongest slow acting on a point (max slowStrength among the zones covering it), 0..1. This
    // is the "how bright is the light on the Mimic here" value that drives its attack slowdown.
    public static float MaxSlowAt(Vector3 point)
    {
        float max = 0f;
        for (int i = 0; i < Active.Count; i++)
        {
            var z = Active[i];
            if (z != null && z.Covers(point) && z.slowStrength > max)
                max = z.slowStrength;
        }
        return max;
    }

    // Is the point inside a HARD-BARRIER zone (the campfire) — the one light the Mimic won't enter?
    public static bool AnyBarrierCovers(Vector3 point)
    {
        for (int i = 0; i < Active.Count; i++)
        {
            var z = Active[i];
            if (z != null && z.blocksMimic && z.Covers(point))
                return true;
        }
        return false;
    }

    // Draws the radius as a wire sphere in the Scene view when selected, so you can size it.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.4f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
