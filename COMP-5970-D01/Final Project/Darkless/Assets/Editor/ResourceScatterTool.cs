using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// EDITOR TOOL (not gameplay code). Lives in Assets/Editor/ so Unity compiles it only for the Editor,
// never into a build. It scatters resource/scenery prefabs across the terrain as real individual
// GameObjects, so they can be edited/removed later.
//
// Two ways to feed it prefabs:
//   * PALETTE (recommended for scenery) — a LIST of prefabs; each placement picks one at RANDOM, so you
//     can scatter a varied forest / rockfield in one go. Drag a folder into "Prefab Folder" and click
//     "Add All Prefabs In Folder" to fill it fast (e.g. Assets/Environment/Trees/Prefabs).
//   * SINGLE PREFAB — the old behaviour; used only when the palette is empty.
//
// Open it via the top menu: Tools > Terrain > Resource Scatter Tool.
public class ResourceScatterTool : EditorWindow
{
    private Terrain terrain;
    private Transform parent;

    // The palette: a random mix of these is scattered. Empty => fall back to the single prefab.
    [SerializeField] private List<GameObject> palette = new List<GameObject>();
    private DefaultAsset prefabFolder;   // drag a project folder here for the quick-add button
    private GameObject prefab;           // single-prefab fallback (old behaviour)

    private int amount = 50;
    private float minDistance = 5f;
    private Vector2 scaleRange = new Vector2(1f, 1f);   // random uniform scale per placement
    private bool randomYaw = true;
    private bool alignToNormal = false;                 // tilt to match the ground slope
    private bool confineToPlayArea = true;              // only scatter inside the PlayArea box
    private PlayArea playArea;                          // auto-found if left empty
    private Vector2 scroll;

    [MenuItem("Tools/Terrain/Resource Scatter Tool")]
    static void ShowWindow() => GetWindow<ResourceScatterTool>("Resource Scatter Tool");

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        GUILayout.Label("Scatter Settings", EditorStyles.boldLabel);
        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);
        parent = (Transform)EditorGUILayout.ObjectField("Parent Object", parent, typeof(Transform), true);

        EditorGUILayout.Space();
        GUILayout.Label("Palette — scatters a RANDOM MIX (empty = use the single prefab below)", EditorStyles.boldLabel);

        prefabFolder = (DefaultAsset)EditorGUILayout.ObjectField("Prefab Folder", prefabFolder, typeof(DefaultAsset), false);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add All Prefabs In Folder")) AddFolderPrefabs();
            if (GUILayout.Button("Clear Palette")) palette.Clear();
        }

        int removeAt = -1;
        for (int i = 0; i < palette.Count; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                palette[i] = (GameObject)EditorGUILayout.ObjectField(palette[i], typeof(GameObject), false);
                if (GUILayout.Button("X", GUILayout.Width(22))) removeAt = i;
            }
        }
        if (removeAt >= 0) palette.RemoveAt(removeAt);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Empty Slot")) palette.Add(null);
            GUILayout.Label($"Palette: {palette.Count} prefab(s)", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space();
        prefab = (GameObject)EditorGUILayout.ObjectField("Single Prefab (fallback)", prefab, typeof(GameObject), true);

        EditorGUILayout.Space();
        amount = EditorGUILayout.IntField("Amount", amount);
        minDistance = EditorGUILayout.FloatField("Min Distance", minDistance);
        scaleRange = EditorGUILayout.Vector2Field("Random Scale (min, max)", scaleRange);
        randomYaw = EditorGUILayout.Toggle("Random Y Rotation", randomYaw);
        alignToNormal = EditorGUILayout.Toggle("Align To Ground Slope", alignToNormal);

        EditorGUILayout.Space();
        confineToPlayArea = EditorGUILayout.Toggle("Confine To Play Area", confineToPlayArea);
        if (confineToPlayArea)
            playArea = (PlayArea)EditorGUILayout.ObjectField("Play Area (auto if empty)", playArea, typeof(PlayArea), true);

        EditorGUILayout.Space();
        if (GUILayout.Button("Scatter Resources")) ScatterResources();

        EditorGUILayout.EndScrollView();
    }

    // Fill the palette with every prefab found in the assigned project folder.
    void AddFolderPrefabs()
    {
        if (prefabFolder == null) { Debug.LogError("Resource Scatter Tool: assign a Prefab Folder first."); return; }
        string folder = AssetDatabase.GetAssetPath(prefabFolder);
        if (!AssetDatabase.IsValidFolder(folder)) { Debug.LogError("Resource Scatter Tool: that asset isn't a folder."); return; }

        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
        int added = 0;
        foreach (var g in guids)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g));
            if (p != null && !palette.Contains(p)) { palette.Add(p); added++; }
        }
        Debug.Log($"Resource Scatter Tool: added {added} prefab(s) from {folder} (palette now {palette.Count}).");
    }

    // The prefabs actually used this run: the non-null palette entries, or the single prefab if empty.
    List<GameObject> ActivePrefabs()
    {
        var list = new List<GameObject>();
        foreach (var p in palette) if (p != null) list.Add(p);
        if (list.Count == 0 && prefab != null) list.Add(prefab);
        return list;
    }

    void ScatterResources()
    {
        var prefabs = ActivePrefabs();
        if (terrain == null || parent == null || prefabs.Count == 0)
        {
            Debug.LogError("Resource Scatter Tool: assign the Terrain, the Parent, and at least one prefab (palette or single) before scattering.");
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 size = terrainData.size;

        // Region to scatter within: the PlayArea box (if confining) or the whole terrain.
        PlayArea area = confineToPlayArea ? (playArea != null ? playArea : Object.FindAnyObjectByType<PlayArea>()) : null;
        if (confineToPlayArea && area == null)
            Debug.LogWarning("Resource Scatter Tool: 'Confine To Play Area' is on but no PlayArea was found — scattering over the whole terrain instead.");

        int placed = 0, attempts = 0, maxAttempts = amount * 20;
        while (placed < amount && attempts < maxAttempts)
        {
            attempts++;

            // Pick a world XZ either inside the play area or across the whole terrain.
            float worldX, worldZ;
            if (area != null)
            {
                Vector3 c = area.Center;
                worldX = Random.Range(c.x - area.size.x * 0.5f, c.x + area.size.x * 0.5f);
                worldZ = Random.Range(c.z - area.size.y * 0.5f, c.z + area.size.y * 0.5f);
            }
            else
            {
                worldX = terrainPosition.x + Random.Range(0f, size.x);
                worldZ = terrainPosition.z + Random.Range(0f, size.z);
            }

            float nx = Mathf.Clamp01((worldX - terrainPosition.x) / size.x);
            float nz = Mathf.Clamp01((worldZ - terrainPosition.z) / size.z);
            float height = terrainData.GetInterpolatedHeight(nx, nz);
            Vector3 spawnPosition = new Vector3(worldX, terrainPosition.y + height, worldZ);

            if (!FarEnough(spawnPosition)) continue;

            // Pick a random prefab from the palette so the scatter is varied.
            GameObject pick = prefabs[Random.Range(0, prefabs.Count)];

            // InstantiatePrefab keeps the copy linked to the prefab asset (edits to the prefab
            // propagate to every scattered copy).
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(pick, parent);
            Undo.RegisterCreatedObjectUndo(instance, "Scatter Resource");
            instance.transform.position = spawnPosition;

            Quaternion rot = Quaternion.identity;
            if (alignToNormal)
                rot = Quaternion.FromToRotation(Vector3.up, terrainData.GetInterpolatedNormal(nx, nz));
            if (randomYaw)
                rot = rot * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            instance.transform.rotation = rot;

            float s = Random.Range(Mathf.Min(scaleRange.x, scaleRange.y), Mathf.Max(scaleRange.x, scaleRange.y));
            if (s > 0f && !Mathf.Approximately(s, 1f))
                instance.transform.localScale = instance.transform.localScale * s;

            placed++;
        }

        Debug.Log($"Resource Scatter Tool: placed {placed} of {amount} from {prefabs.Count} prefab(s) (in {attempts} attempts).");
    }

    // Returns false if 'position' is closer than minDistance to any object already under 'parent'.
    bool FarEnough(Vector3 position)
    {
        foreach (Transform child in parent)
            if (Vector3.Distance(position, child.position) < minDistance) return false;
        return true;
    }
}
