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

    // Draws the radius as a wire sphere in the Scene view when selected, so you can size it.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.4f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
