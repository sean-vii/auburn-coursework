using UnityEngine;

// Darkless — a SQUARE spawn RANGE for a single key search-spot (slime). Place one of these empty
// objects wherever you want a searchable area to be able to appear; the SearchSpotSpawner spawns
// exactly ONE slime at a random point inside this square. The number of search areas in the game
// equals the number of these zones you place, and each zone's 'size' controls how large its
// spawn range is (vary it per zone for tighter or looser placement).
//
// How to use (Editor): create an empty GameObject, add this component, move it where you want the
// area, and set Size. The green square gizmo shows the range. Repeat for each search area you want.
[DisallowMultipleComponent]
public class SearchSpawnZone : MonoBehaviour
{
    [Tooltip("Side length (in metres) of the square spawn range, centred on this object. A single " +
             "search slime spawns at a random point inside it. Bigger = the slime could appear " +
             "anywhere in a wider area; set to 0 to pin it exactly to this object's position.")]
    public float size = 20f;

    [Tooltip("Drop the spawned slime onto the terrain height. Leave on for normal ground placement.")]
    public bool snapToTerrain = true;

    // A random point inside the square (on the XZ plane), optionally snapped to the terrain. Uses the
    // supplied seeded rng so the same game seed reproduces the same point.
    public Vector3 RandomPoint(System.Random rng)
    {
        float half = Mathf.Max(0f, size) * 0.5f;
        float rx = (float)(rng.NextDouble() * 2.0 - 1.0) * half;
        float rz = (float)(rng.NextDouble() * 2.0 - 1.0) * half;
        Vector3 p = transform.position + new Vector3(rx, 0f, rz);

        if (snapToTerrain)
        {
            var terrain = Terrain.activeTerrain;
            if (terrain != null)
                p.y = terrain.SampleHeight(p) + terrain.transform.position.y;
        }
        return p;
    }

    void OnDrawGizmos()
    {
        Vector3 c = transform.position;
        Vector3 s = new Vector3(Mathf.Max(0f, size), 0.15f, Mathf.Max(0f, size));
        Gizmos.color = new Color(0.25f, 1f, 0.45f, 0.15f);
        Gizmos.DrawCube(c, s);
        Gizmos.color = new Color(0.25f, 1f, 0.45f, 0.9f);
        Gizmos.DrawWireCube(c, s);
    }
}
