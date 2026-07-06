using UnityEngine;
using UnityEditor;

// EDITOR TOOL (not gameplay code). Lives in Assets/Editor/ so Unity compiles it only for
// the Editor, never into a build. It adds a custom window under Tools > Terrain that
// scatters a resource prefab (apple trees, ore rocks, ...) across the terrain as real
// individual GameObjects, so gameplay scripts can detect and remove them later.
//
// Open it via the top menu: Tools > Terrain > Resource Scatter Tool.
public class ResourceScatterTool : EditorWindow
{
    [Tooltip("The terrain to scatter objects onto.")]
    private Terrain terrain;
    [Tooltip("The resource prefab to scatter (e.g. the AppleTree or OreRock prefab).")]
    private GameObject prefab;
    [Tooltip("The parent object the spawned copies go under, to keep the hierarchy tidy.")]
    private Transform parent;
    [Tooltip("How many copies to place.")]
    private int amount = 50;
    [Tooltip("Closest two placed objects are allowed to be, in world units.")]
    private float minDistance = 5f;

    // Registers the menu entry that opens this window.
    [MenuItem("Tools/Terrain/Resource Scatter Tool")]
    static void ShowWindow()
    {
        // GetWindow finds an existing window of this type or creates one; the string is its title.
        GetWindow<ResourceScatterTool>("Resource Scatter Tool");
    }

    // OnGUI draws the window's controls every time it repaints (this is Editor UI, not game UI).
    void OnGUI()
    {
        GUILayout.Label("Scatter Settings", EditorStyles.boldLabel);

        // ObjectField lets us drag scene/asset references into the tool.
        // The trailing 'true' means scene objects (not just project assets) are allowed.
        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);
        prefab = (GameObject)EditorGUILayout.ObjectField("Prefab To Scatter", prefab, typeof(GameObject), true);
        parent = (Transform)EditorGUILayout.ObjectField("Parent Object", parent, typeof(Transform), true);

        amount = EditorGUILayout.IntField("Amount", amount);
        minDistance = EditorGUILayout.FloatField("Min Distance", minDistance);

        if (GUILayout.Button("Scatter Resources"))
        {
            ScatterResources();
        }
    }

    // Places 'amount' copies of the prefab at random spots on the terrain, spaced out by minDistance.
    void ScatterResources()
    {
        // Bail out early with a clear console error if anything is unassigned.
        if (terrain == null || prefab == null || parent == null)
        {
            Debug.LogError("Resource Scatter Tool: assign the Terrain, Prefab, and Parent before scattering.");
            return;
        }

        // TerrainData holds the height map; terrainPosition is the terrain's world origin;
        // size is how big the terrain is on each axis. We need all three to place objects
        // on the actual ground surface instead of floating or buried.
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 size = terrainData.size;

        int placed = 0;
        int attempts = 0;
        // Cap the attempts so a too-strict minDistance can't loop forever.
        int maxAttempts = amount * 20;

        while (placed < amount && attempts < maxAttempts)
        {
            attempts++;

            // Pick a random spot within the terrain footprint.
            float randomX = Random.Range(0f, size.x);
            float randomZ = Random.Range(0f, size.z);

            // GetInterpolatedHeight takes normalized 0-1 coordinates and returns the ground
            // height there, so the object sits on the surface.
            float height = terrainData.GetInterpolatedHeight(randomX / size.x, randomZ / size.z);

            Vector3 spawnPosition = terrainPosition + new Vector3(randomX, height, randomZ);

            // Skip this spot if it crowds an already-placed object.
            if (!FarEnough(spawnPosition))
                continue;

            // InstantiatePrefab (not plain Instantiate) keeps the copy linked to the prefab
            // asset, so later edits to the prefab (e.g. adding the InteractableResource script
            // in a later video) automatically apply to every scattered copy.
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);

            // Let Ctrl+Z undo the whole scatter if we don't like the result.
            Undo.RegisterCreatedObjectUndo(instance, "Scatter Resource");

            instance.transform.position = spawnPosition;
            // Random turn around the vertical axis so they don't all face the same way.
            instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            placed++;
        }

        Debug.Log($"Resource Scatter Tool: placed {placed} of {amount} '{prefab.name}' (in {attempts} attempts).");
    }

    // Returns false if 'position' is closer than minDistance to any object already under 'parent'.
    bool FarEnough(Vector3 position)
    {
        foreach (Transform child in parent)
        {
            if (Vector3.Distance(position, child.position) < minDistance)
                return false;
        }
        return true;
    }
}
