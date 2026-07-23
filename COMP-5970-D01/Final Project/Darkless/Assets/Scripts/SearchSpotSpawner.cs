using System.Collections.Generic;
using UnityEngine;

// Darkless — spawns the night-time key SEARCH SPOTS (the slime creatures) AND coordinates the
// guaranteed delivery of the correct car key.
//
// SPAWNING — three modes, in priority order:
//   1) Spawn ZONES (preferred): place SearchSpawnZone objects in the scene (each a sized square).
//      One slime spawns at a random point inside each zone. The NUMBER of search areas == the number
//      of zones you place; each zone's size controls how big its spawn range is.
//   2) Fixed spawn points: a list of hand-placed Transforms (legacy).
//   3) Random ring around camp (legacy fallback).
// The layout is chosen ONCE at Start from a seed and the slimes persist, so it's the same every night
// of a run; a new game (scene reload) rolls a fresh layout.
//
// THE KEY GUARANTEE — the single correct car key is delivered on the player's K-th search, where
// (N = total number of search areas):
//   * K <= min(7, N)      -> found within the first 7 searches, or the LAST area if there are fewer.
//   * K >= 3 when N >= 3   -> the first two searches never give it (you must search at least 3).
// K is rolled once per game from the seed; before it, searches turn up "nothing here."
//
// Put this on GameManager. Assign the slime model, the CarKey reward, and (optionally) a tint material.
public class SearchSpotSpawner : MonoBehaviour
{
    [Header("What to spawn")]
    [Tooltip("The slime creature model to place at each search spot (Assets/Models/Slime/Slime.fbx).")]
    public GameObject searchSpotPrefab;
    [Tooltip("Optional material to tint every spawned slime (e.g. SlimeGreen). Leave empty to keep the " +
             "model's own materials.")]
    public Material spotMaterial;
    [Tooltip("The CORRECT car key granted on the guaranteed search (the CarKey ItemDefinition).")]
    public ItemDefinition rewardItem;
    public int rewardAmount = 1;
    [Range(0f, 1f)]
    [Tooltip("Legacy random produce chance, used ONLY when there is no spawner coordinating the guarantee " +
             "(i.e. a hand-placed SearchSpot with no spawner). With the spawner active, the guarantee " +
             "below decides the key instead and this is ignored.")]
    public float produceChance = 0.7f;

    [Header("Multiple keys — the GDD §13 twist")]
    [Tooltip("The pool of DIFFERENT car keys (e.g. Brass/Silver/Rusty/Iron). At Start ONE is chosen at " +
             "random as the key that actually fits the car; the rest become red herrings scattered among " +
             "the search spots. Leave EMPTY to use the single 'Reward Item' key above (legacy MVP).")]
    public List<ItemDefinition> keyPool = new List<ItemDefinition>();
    [Tooltip("The Car whose lock gets set to the randomly-chosen correct key. Auto-found if left empty.")]
    public Car car;
    [Range(0f, 1f)]
    [Tooltip("Multi-key only: chance a search that ISN'T the guaranteed correct-key search still turns " +
             "up a WRONG (red-herring) key instead of nothing. Higher = more keys to gamble with.")]
    public float redHerringChance = 0.6f;

    [Header("Spawn zones (preferred)")]
    [Tooltip("Place SearchSpawnZone objects in the scene; this spawns ONE slime inside each. The number " +
             "of zones = the number of search areas. Turn OFF to use the legacy fixed-points / ring below.")]
    public bool useSpawnZones = true;

    [Header("Fixed spawn points (legacy)")]
    [Tooltip("Legacy: exact Transforms to spawn slimes at. Used only if Use Spawn Zones is off (or no " +
             "zones exist) and Use Fixed Spawn Points is on.")]
    public List<Transform> spawnPoints = new List<Transform>();
    public bool useFixedSpawnPoints = true;

    [Header("Random-ring fallback (legacy)")]
    [Tooltip("How many slimes to scatter in the ring fallback (used only when no zones/points apply).")]
    public int spotCount = 5;
    public Vector3 areaCenter = new Vector3(490f, 30f, 424f);
    public bool centerOnCampfire = true;
    public float minRadius = 30f;
    public float maxRadius = 130f;
    public float minSeparation = 15f;
    [Tooltip("Uniform scale applied to each slime ('little' creatures).")]
    public float spotScale = 0.6f;

    [Header("Search behaviour (copied onto every spawned spot)")]
    public bool nightOnly = true;
    public float searchDuration = 3f;
    public string prompt = "Hold E to search the creature";
    [Tooltip("Animator trigger for the search animation. 'PickStanding' = the arm-extend reach.")]
    public string animationTrigger = "PickStanding";

    [Header("Randomization")]
    [Tooltip("0 = a FRESH random layout AND key position each new game. Non-zero LOCKS both (testing). " +
             "Either way it's chosen ONCE at Start and never reshuffles between nights.")]
    public int seed = 0;

    // The seed actually used this run (useful for reproducing a layout / key roll).
    public int ActiveSeed { get; private set; }

    // --- Key-guarantee state ---
    int totalSpots;       // N: number of search areas spawned
    int searchesDone;     // how many searches the player has completed
    int keySearchIndex;   // K: the search number that yields the correct key
    bool keyGiven;
    System.Random rng;

    // --- Multi-key state (the twist) ---
    bool multiKey;                     // true when keyPool has entries → several key types in play
    ItemDefinition correctKey;         // the one key that fits the car this run
    List<ItemDefinition> herringKeys;  // the rest of the pool (wrong keys)

    void Start()
    {
        if (searchSpotPrefab == null)
        {
            Debug.LogWarning("SearchSpotSpawner: no searchSpotPrefab assigned — nothing to spawn.");
            return;
        }

        // Seed once for this game instance (layout, per-zone point, and the key roll all draw from it).
        ActiveSeed = seed != 0 ? seed : Random.Range(1, int.MaxValue);
        rng = new System.Random(ActiveSeed);

        // Decide which key fits the car this run (the multi-key twist), BEFORE anything spawns.
        PickCorrectKey();

        // MODE 1 — spawn zones (preferred).
        if (useSpawnZones)
        {
            var zones = FindObjectsByType<SearchSpawnZone>(FindObjectsSortMode.None);
            if (zones != null && zones.Length > 0)
            {
                // Deterministic order (by position) so a fixed seed reproduces the same layout.
                System.Array.Sort(zones, ZoneOrder);
                var container = new GameObject("SearchSpots (runtime)");
                int n = 0;
                foreach (var z in zones)
                {
                    if (z == null) continue;
                    n++;
                    Vector3 pos = z.RandomPoint(rng);
                    float yaw = (float)(rng.NextDouble() * 360.0);
                    SpawnAt(pos, Quaternion.Euler(0f, yaw, 0f), container.transform, n, groundSnap: z.snapToTerrain);
                }
                SetupGuarantee(n);
                return;
            }
            Debug.LogWarning("SearchSpotSpawner: Use Spawn Zones is on but no SearchSpawnZone was found — " +
                             "falling back to fixed points / ring. Add SearchSpawnZone objects to the scene.");
        }

        // MODE 2 — fixed spawn points (legacy).
        if (useFixedSpawnPoints && spawnPoints != null && spawnPoints.Count > 0)
        {
            var fixedContainer = new GameObject("SearchSpots (runtime)");
            int n = 0;
            foreach (var t in spawnPoints)
            {
                if (t == null) continue;
                n++;
                SpawnAt(t.position, t.rotation, fixedContainer.transform, n, groundSnap: false);
            }
            SetupGuarantee(n);
            return;
        }

        // MODE 3 — random ring around camp (legacy fallback).
        Vector3 center = areaCenter;
        if (centerOnCampfire)
        {
            var fire = FindFirstObjectByType<Campfire>();
            if (fire != null) center = fire.transform.position;
        }

        var terrain = Terrain.activeTerrain;
        var placed = new List<Vector3>();
        var ringContainer = new GameObject("SearchSpots (runtime)");

        int made = 0, attempts = 0, maxAttempts = spotCount * 40;
        while (made < spotCount && attempts < maxAttempts)
        {
            attempts++;
            float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            float rad = minRadius + (float)(rng.NextDouble() * (maxRadius - minRadius));
            Vector3 pos = center + new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);

            if (terrain != null)
            {
                Vector3 tp = terrain.transform.position;
                Vector3 ts = terrain.terrainData.size;
                if (pos.x < tp.x + 5f || pos.x > tp.x + ts.x - 5f ||
                    pos.z < tp.z + 5f || pos.z > tp.z + ts.z - 5f)
                    continue;
                pos.y = terrain.SampleHeight(pos) + tp.y;
            }

            bool tooClose = false;
            foreach (var q in placed)
                if (Vector3.Distance(pos, q) < minSeparation) { tooClose = true; break; }
            if (tooClose) continue;

            placed.Add(pos);
            made++;
            SpawnAt(pos, Quaternion.Euler(0f, made * 57f, 0f), ringContainer.transform, made, groundSnap: true);
        }

        if (made < spotCount)
            Debug.LogWarning($"SearchSpotSpawner: only placed {made}/{spotCount} spots (area too tight?).");
        SetupGuarantee(made);
    }

    // Order zones deterministically by position so a fixed seed reproduces the same layout.
    static int ZoneOrder(SearchSpawnZone a, SearchSpawnZone b)
    {
        Vector3 pa = a.transform.position, pb = b.transform.position;
        int cx = pa.x.CompareTo(pb.x);
        return cx != 0 ? cx : pa.z.CompareTo(pb.z);
    }

    // Roll the K-th-search index once, given N total areas:
    //   lower = (N >= 3) ? 3 : 1    -> must search at least 3 when there are 3+
    //   upper = min(7, N)           -> found within the first 7, or the last area if fewer
    void SetupGuarantee(int count)
    {
        totalSpots = count;
        searchesDone = 0;
        keyGiven = false;

        if (count <= 0) { keySearchIndex = 0; return; }
        int lower = count >= 3 ? 3 : 1;
        int upper = Mathf.Min(7, count);
        if (upper < lower) upper = lower;
        keySearchIndex = rng.Next(lower, upper + 1); // System.Random.Next upper bound is exclusive
        Debug.Log($"SearchSpotSpawner: {count} search areas — the correct car key will appear on search #{keySearchIndex}.");
    }

    // Pick which key fits the car this run, from the keyPool. The rest become red herrings. Also points
    // the Car's lock at the chosen key. No pool (empty) → legacy single-key mode (the 'rewardItem' key).
    void PickCorrectKey()
    {
        // Clean the pool: drop nulls and duplicates.
        var pool = new List<ItemDefinition>();
        if (keyPool != null)
            foreach (var k in keyPool)
                if (k != null && !pool.Contains(k)) pool.Add(k);

        multiKey = pool.Count > 0;
        if (!multiKey) return;

        int idx = rng.Next(pool.Count);
        correctKey = pool[idx];
        herringKeys = new List<ItemDefinition>();
        for (int i = 0; i < pool.Count; i++)
            if (i != idx) herringKeys.Add(pool[i]);

        // The guarantee delivers THIS key; the car's lock accepts only THIS key.
        rewardItem = correctKey;
        if (car == null) car = FindFirstObjectByType<Car>();
        if (car != null) car.correctKey = correctKey;

        Debug.Log($"SearchSpotSpawner: {pool.Count} key types in play — the one that FITS is " +
                  $"'{correctKey.displayName}'. The rest are red herrings.");
    }

    // Called by a SearchSpot when a search completes. Returns true if THIS search yields the correct
    // key. Enforces the guarantee: not before K (and never in the first 2 when there are 3+), and by
    // the last area at the latest. (Kept for compatibility; SearchSpot uses ResolveSearchReward.)
    public bool RegisterSearch()
    {
        searchesDone++;
        int remaining = totalSpots - searchesDone;   // areas still unsearched after this one
        if (!keyGiven && (searchesDone >= keySearchIndex || remaining <= 0))
        {
            keyGiven = true;
            return true;
        }
        return false;
    }

    // Called by a SearchSpot when a search completes. Returns the ITEM this search yields, or null for
    // "nothing here". The correct key is delivered on the guaranteed search (>= K, or by the last area);
    // in multi-key mode, other searches may turn up a random WRONG key (a red herring).
    public ItemDefinition ResolveSearchReward()
    {
        searchesDone++;
        int remaining = totalSpots - searchesDone;   // areas still unsearched after this one
        bool giveCorrect = !keyGiven && (searchesDone >= keySearchIndex || remaining <= 0);

        if (giveCorrect)
        {
            keyGiven = true;
            return multiKey ? correctKey : rewardItem;
        }

        // Not the guaranteed search: in multi-key mode, sometimes hand out a red herring.
        if (multiKey && herringKeys != null && herringKeys.Count > 0
            && rng.NextDouble() <= redHerringChance)
            return herringKeys[rng.Next(herringKeys.Count)];

        return null; // nothing found this time
    }

    void SpawnAt(Vector3 pos, Quaternion rot, Transform parent, int index, bool groundSnap)
    {
        var go = Instantiate(searchSpotPrefab, pos, rot, parent);
        go.name = "SlimeSearchSpot_" + index;
        go.transform.localScale = Vector3.one * spotScale;

        if (groundSnap) GroundSnap(go);
        FitBoxCollider(go);

        if (spotMaterial != null)
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = spotMaterial;

        var spot = go.AddComponent<SearchSpot>();
        spot.coordinator = this;                 // the spot asks us whether it yields the key
        spot.rewardItem = rewardItem;
        spot.rewardAmount = rewardAmount;
        spot.produceChance = produceChance;
        spot.nightOnly = nightOnly;
        spot.searchDuration = searchDuration;
        spot.prompt = prompt;
        spot.animationTrigger = animationTrigger;
    }

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
