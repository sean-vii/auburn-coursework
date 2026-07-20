using System.Collections.Generic;
using UnityEngine;

// Darkless — scatters the night-time key **search spots** (the slime creatures) at RANDOM positions
// around camp, but with a layout that's STABLE for the whole playthrough.
//
// The rule the design wants: search spots appear randomly, yet **every night of a single game they're
// in the same places.** We get that for free by generating the layout **once** at the start of the game
// (Start) from a seeded random number generator, and letting the spawned slimes persist. The scene is
// one continuous day/night cycle — it never reloads between nights — so "spawn once, keep them" already
// means "same spots every night." A NEW game (Play Again reloads the scene) re-runs Start with a fresh
// seed → a brand-new layout. That's exactly "random per game instance, consistent within it."
//
// Put this on GameManager. Assign the slime model, the reward key, and the tint material.
public class SearchSpotSpawner : MonoBehaviour
{
    [Header("What to spawn")]
    [Tooltip("The slime creature model to place at each search spot (Assets/Models/Slime/Slime.fbx).")]
    public GameObject searchSpotPrefab;
    [Tooltip("Optional material to tint every spawned slime (e.g. SlimeGreen). Leave empty to keep the " +
             "model's own materials.")]
    public Material spotMaterial;
    [Tooltip("Item granted when a spot is searched. MVP: the CarKey. (The multi-key twist will vary this.)")]
    public ItemDefinition rewardItem;
    public int rewardAmount = 1;
    [Range(0f, 1f)]
    [Tooltip("Chance a completed search produces its item (0.7 = 70%). Copied onto every spawned spot.")]
    public float produceChance = 0.7f;

    [Header("How many / where")]
    [Tooltip("How many search-spot slimes to scatter.")]
    public int spotCount = 5;
    [Tooltip("Center of the scatter area. If 'Center On Campfire' is on, the campfire's position is used " +
             "instead at Start.")]
    public Vector3 areaCenter = new Vector3(490f, 30f, 424f);
    public bool centerOnCampfire = true;
    [Tooltip("Nearest a spot can spawn to the center (keeps them out of camp).")]
    public float minRadius = 30f;
    [Tooltip("Farthest a spot can spawn from the center (keep inside the searchable area).")]
    public float maxRadius = 130f;
    [Tooltip("Minimum distance between two spots, so they don't clump.")]
    public float minSeparation = 15f;
    [Tooltip("Uniform scale applied to each slime ('little' creatures).")]
    public float spotScale = 0.6f;

    [Header("Search behaviour (copied onto every spawned spot)")]
    public bool nightOnly = true;
    public float searchDuration = 3f;
    public string prompt = "Hold E to search the creature";
    [Tooltip("Animator trigger for the search animation. 'PickStanding' = the arm-extend reach (PickFruit_Standing).")]
    public string animationTrigger = "PickStanding";

    [Header("Randomization")]
    [Tooltip("0 = pick a FRESH random layout each new game. Set a non-zero number to LOCK one specific " +
             "layout (handy for testing / comparing runs). Either way the layout is chosen ONCE at the " +
             "start of the game and never reshuffles between nights.")]
    public int seed = 0;

    // The seed actually used this run (useful for debugging / reproducing a layout).
    public int ActiveSeed { get; private set; }

    void Start()
    {
        if (searchSpotPrefab == null)
        {
            Debug.LogWarning("SearchSpotSpawner: no searchSpotPrefab assigned — nothing to spawn.");
            return;
        }

        // Pick the seed ONCE for this game instance. A fixed non-zero seed locks the layout; 0 means a
        // fresh random layout each new game (Unity's Random is engine-seeded at Start).
        ActiveSeed = seed != 0 ? seed : Random.Range(1, int.MaxValue);
        var rng = new System.Random(ActiveSeed);

        Vector3 center = areaCenter;
        if (centerOnCampfire)
        {
            var fire = FindFirstObjectByType<Campfire>();
            if (fire != null) center = fire.transform.position;
        }

        var terrain = Terrain.activeTerrain;
        var placed = new List<Vector3>();
        var container = new GameObject("SearchSpots (runtime)");

        int made = 0, attempts = 0, maxAttempts = spotCount * 40;
        while (made < spotCount && attempts < maxAttempts)
        {
            attempts++;

            // Random point in a ring [minRadius, maxRadius] around center. Every draw comes from the
            // seeded rng, so the same seed always yields the same layout.
            float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            float rad = minRadius + (float)(rng.NextDouble() * (maxRadius - minRadius));
            Vector3 pos = center + new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);

            // Keep inside the terrain (with a small margin) and drop onto the ground.
            if (terrain != null)
            {
                Vector3 tp = terrain.transform.position;
                Vector3 ts = terrain.terrainData.size;
                if (pos.x < tp.x + 5f || pos.x > tp.x + ts.x - 5f ||
                    pos.z < tp.z + 5f || pos.z > tp.z + ts.z - 5f)
                    continue;
                pos.y = terrain.SampleHeight(pos) + tp.y;
            }

            // Don't clump.
            bool tooClose = false;
            foreach (var q in placed)
                if (Vector3.Distance(pos, q) < minSeparation) { tooClose = true; break; }
            if (tooClose) continue;

            placed.Add(pos);
            made++;
            SpawnOne(pos, container.transform, made);
        }

        if (made < spotCount)
            Debug.LogWarning($"SearchSpotSpawner: only placed {made}/{spotCount} spots (area too tight?).");
    }

    void SpawnOne(Vector3 pos, Transform parent, int index)
    {
        var go = Instantiate(searchSpotPrefab, pos, Quaternion.Euler(0f, index * 57f, 0f), parent);
        go.name = "SlimeSearchSpot_" + index;
        go.transform.localScale = Vector3.one * spotScale;

        GroundSnap(go);
        FitBoxCollider(go);

        if (spotMaterial != null)
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = spotMaterial;

        var spot = go.AddComponent<SearchSpot>();
        spot.rewardItem = rewardItem;
        spot.rewardAmount = rewardAmount;
        spot.produceChance = produceChance;
        spot.nightOnly = nightOnly;
        spot.searchDuration = searchDuration;
        spot.prompt = prompt;
        spot.animationTrigger = animationTrigger;
    }

    // Drop 'go' so the bottom of its renderer bounds rests on the terrain at its XZ.
    static void GroundSnap(GameObject go)
    {
        var terrain = Terrain.activeTerrain;
        if (terrain == null) return;
        Vector3 p = go.transform.position;
        float h = terrain.SampleHeight(p) + terrain.transform.position.y;
        go.transform.position = new Vector3(p.x, h, p.z);
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;
        Bounds b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);
        go.transform.position += new Vector3(0f, h - b.min.y, 0f);
    }

    static void FitBoxCollider(GameObject go)
    {
        var box = go.AddComponent<BoxCollider>();

        // Fit the collider to the actual RENDERED world bounds, then convert into local space. We can't
        // use mesh.bounds here: for a SKINNED (rigged) mesh like the slime, sharedMesh.bounds comes back
        // near-zero, which gave a useless 2cm collider you couldn't interact with. renderer.bounds is
        // the real posed size.
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;
        Bounds wb = rends[0].bounds;
        foreach (var r in rends) wb.Encapsulate(r.bounds);

        Vector3 ls = go.transform.lossyScale;
        box.center = go.transform.InverseTransformPoint(wb.center);
        box.size = new Vector3(
            Mathf.Abs(ls.x) > 1e-4f ? wb.size.x / Mathf.Abs(ls.x) : wb.size.x,
            Mathf.Abs(ls.y) > 1e-4f ? wb.size.y / Mathf.Abs(ls.y) : wb.size.y,
            Mathf.Abs(ls.z) > 1e-4f ? wb.size.z / Mathf.Abs(ls.z) : wb.size.z);
    }
}
