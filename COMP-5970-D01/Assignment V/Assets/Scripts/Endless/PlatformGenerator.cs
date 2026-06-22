using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Endless procedural platform generator. Spawns platform sections ahead of the
/// player and recycles (destroys) sections that fall behind. Sections form a
/// continuous, always-connected lane: the walkable floor always covers a column
/// around <see cref="laneCenter"/>, which only drifts a little per section, so
/// consecutive platforms always overlap. Four distinct platform "prefabs" (built
/// procedurally) provide variety, and hazards from <see cref="Hazard"/> are stamped
/// onto the real floor with rising difficulty.
/// </summary>
public class PlatformGenerator : MonoBehaviour
{
    public const float SectionLength = 20f;

    [Header("Streaming distances")]
    public float spawnAhead = 90f;
    public float despawnBehind = 30f;

    [Header("Difficulty / layout")]
    public int safeSections = 2;     // first sections are flat with no hazards
    public float laneBound = 6f;     // how far the lane may drift from centre
    public float maxDrift = 1.2f;    // lateral drift per section (small => connected)

    Transform player;
    float nextStartZ;
    int sectionIndex;
    float laneCenter;
    bool useKenney;

    class Section
    {
        public GameObject root;
        public float endZ;
    }

    readonly List<Section> sections = new List<Section>();

    Material matFloorA, matFloorB, matFloorC, matFloorD;
    Material matKill, matSlow, matBumper, matReverse, matBoost;

    /// <summary>Called by the bootstrap. Sets the player and fills the start runway.</summary>
    public void Initialize(Transform playerTransform)
    {
        player = playerTransform;
        laneCenter = 0f;
        BuildMaterials();
        useKenney = KenneyAssets.Available;

        nextStartZ = -SectionLength;   // one section behind the player's start
        FillAhead();
    }

    void BuildMaterials()
    {
        matFloorA = GameBootstrap.MakeMaterial(new Color(0.27f, 0.45f, 0.68f)); // steel blue
        matFloorB = GameBootstrap.MakeMaterial(new Color(0.25f, 0.58f, 0.55f)); // teal
        matFloorC = GameBootstrap.MakeMaterial(new Color(0.62f, 0.45f, 0.72f)); // violet
        matFloorD = GameBootstrap.MakeMaterial(new Color(0.78f, 0.55f, 0.32f)); // amber

        matKill = GameBootstrap.MakeMaterial(new Color(0.85f, 0.18f, 0.18f));   // red
        matSlow = GameBootstrap.MakeMaterial(new Color(0.20f, 0.55f, 0.95f));   // blue
        matBumper = GameBootstrap.MakeMaterial(new Color(0.95f, 0.80f, 0.15f)); // yellow
        matReverse = GameBootstrap.MakeMaterial(new Color(0.70f, 0.20f, 0.85f));// magenta
        matBoost = GameBootstrap.MakeMaterial(new Color(0.25f, 0.85f, 0.35f));  // green
    }

    void Update()
    {
        if (player == null)
        {
            return;
        }

        FillAhead();
        Recycle();
    }

    void FillAhead()
    {
        while (nextStartZ < player.position.z + spawnAhead)
        {
            SpawnSection(nextStartZ, sectionIndex);
            nextStartZ += SectionLength;
            sectionIndex++;
        }
    }

    void Recycle()
    {
        float cutoff = player.position.z - despawnBehind;
        for (int i = sections.Count - 1; i >= 0; i--)
        {
            if (sections[i].endZ < cutoff)
            {
                Destroy(sections[i].root);
                sections.RemoveAt(i);
            }
        }
    }

    void SpawnSection(float startZ, int index)
    {
        GameObject root = new GameObject("Section_" + index);
        root.transform.SetParent(transform, false);
        root.transform.position = new Vector3(0f, 0f, startZ);

        // Drift the lane only after the safe runway. The small step keeps the
        // walkable column of consecutive sections overlapping (always connected).
        if (index >= safeSections)
        {
            laneCenter = Mathf.Clamp(laneCenter + Random.Range(-maxDrift, maxDrift), -laneBound, laneBound);
        }

        int variant = (index < safeSections) ? 0 : Random.Range(0, 4);

        float width;
        Material mat;
        switch (variant)
        {
            case 1: width = 4f; mat = matFloorB; break;   // narrow lane
            case 2: width = 3f; mat = matFloorC; break;   // plank
            case 3: width = 5f; mat = matFloorD; break;   // lane + detached side ledge
            default: width = 10f; mat = matFloorA; break; // wide lane
        }

        // Main walkable floor: always centred on laneCenter so there is a path.
        AddFloor(root.transform, laneCenter, width, mat);

        // The "split" variant adds a separate ledge to one side with a visible gap.
        if (variant == 3)
        {
            float side = (Random.value < 0.5f) ? -1f : 1f;
            float ledgeCenter = Mathf.Clamp(laneCenter + side * (width * 0.5f + 3.5f), -laneBound - 4f, laneBound + 4f);
            AddFloor(root.transform, ledgeCenter, 3f, mat);
        }

        if (index >= safeSections)
        {
            AddHazards(root.transform, index, laneCenter, width);
        }

        sections.Add(new Section { root = root, endZ = startZ + SectionLength });
    }

    void AddFloor(Transform parent, float centerX, float width, Material mat)
    {
        GameObject floor = new GameObject("Floor");
        floor.transform.SetParent(parent, false);
        // Section parent sits at the section's start; lay the floor along +Z.
        floor.transform.localPosition = new Vector3(centerX, -0.5f, SectionLength * 0.5f);

        // Gameplay collider is an exact box; the visual is fitted to match.
        BoxCollider col = floor.AddComponent<BoxCollider>();
        col.size = new Vector3(width, 1f, SectionLength);

        MakeVisual(floor.transform, "block-grass", Vector3.zero,
                   new Vector3(width, 1f, SectionLength), false, mat);
    }

    // --- Hazard placement (always on the real floor) -----------------------

    void AddHazards(Transform parent, int index, float center, float width)
    {
        int count = (width <= 4f) ? 1 : Mathf.Clamp(1 + index / 6, 1, 3);

        // Keep hazards clear of the very edge so they sit fully on the platform.
        float usableHalf = Mathf.Max(0.2f, width * 0.5f - 1.0f);

        float zStart = 4f;
        float zStep = (SectionLength - 8f) / count;

        for (int i = 0; i < count; i++)
        {
            float z = zStart + zStep * i + Random.Range(0f, zStep * 0.4f);
            Hazard.HazardType type = PickHazard();

            float x;
            if (type == Hazard.HazardType.Kill && width <= 5f)
            {
                // On tight lanes, push the lethal block to one side so a gap remains.
                float side = (Random.value < 0.5f) ? -1f : 1f;
                x = center + side * usableHalf;
            }
            else
            {
                x = center + Random.Range(-usableHalf, usableHalf);
            }

            SpawnHazard(parent, type, new Vector3(x, 0f, z));
        }
    }

    Hazard.HazardType PickHazard()
    {
        // Weighted: lethal and disruptive hazards common, boost a rare bonus.
        int roll = Random.Range(0, 100);
        if (roll < 35) return Hazard.HazardType.Kill;
        if (roll < 60) return Hazard.HazardType.Slow;
        if (roll < 80) return Hazard.HazardType.Bumper;
        if (roll < 93) return Hazard.HazardType.Reverse;
        return Hazard.HazardType.Boost;
    }

    void SpawnHazard(Transform parent, Hazard.HazardType type, Vector3 localPos)
    {
        GameObject go = new GameObject("Hazard_" + type);
        go.transform.SetParent(parent, false);

        // The gameplay collider is an explicit box; visuals are added separately.
        BoxCollider col = go.AddComponent<BoxCollider>();
        Hazard hazard = go.AddComponent<Hazard>();
        hazard.type = type;

        switch (type)
        {
            case Hazard.HazardType.Kill:
                // Solid spikes the player crashes into.
                col.size = new Vector3(1.4f, 1.6f, 1.4f);
                col.isTrigger = false;
                localPos.y = 0.8f;
                go.transform.localPosition = localPos;
                MakeVisual(go.transform, "spike-block", Vector3.zero,
                           new Vector3(1.4f, 1.6f, 1.4f), true, matKill);
                break;

            case Hazard.HazardType.Bumper:
                // Spring that knocks the player aside on contact.
                col.size = new Vector3(1.2f, 1.4f, 1.2f);
                col.isTrigger = true;
                localPos.y = 0.7f;
                go.transform.localPosition = localPos;
                MakeVisual(go.transform, "spring", Vector3.zero,
                           new Vector3(1.2f, 1.4f, 1.2f), true, matBumper);
                break;

            case Hazard.HazardType.Boost:
                // Collectible coin that grants a speed boost (spins via Hazard).
                col.size = new Vector3(1.6f, 1.4f, 1.6f);
                col.isTrigger = true;
                localPos.y = 0.6f;
                go.transform.localPosition = localPos;
                MakeVisual(go.transform, "coin-gold", Vector3.zero,
                           new Vector3(1.4f, 1.4f, 1.4f), true, matBoost);
                break;

            case Hazard.HazardType.Slow:
                MakePad(go, col, matSlow, ref localPos);
                break;

            case Hazard.HazardType.Reverse:
                MakePad(go, col, matReverse, ref localPos);
                break;
        }
    }

    // Flat coloured trigger pad the ball rolls across (status-effect zones).
    void MakePad(GameObject go, BoxCollider col, Material mat, ref Vector3 localPos)
    {
        col.size = new Vector3(2.0f, 0.3f, 2.0f);
        col.isTrigger = true;
        localPos.y = 0.1f;
        go.transform.localPosition = localPos;
        MakeVisual(go.transform, "", Vector3.zero, new Vector3(2.0f, 0.15f, 2.0f), false, mat);
    }

    // Builds a piece's visual: a fitted Kenney model when available, otherwise a
    // primitive cube with a solid colour (also used for the flat effect pads).
    void MakeVisual(Transform parent, string modelName, Vector3 localCenter, Vector3 size, bool uniform, Material fallback)
    {
        if (useKenney && !string.IsNullOrEmpty(modelName))
        {
            GameObject model = KenneyAssets.SpawnFitted(modelName, parent, localCenter, size, uniform);
            if (model != null)
            {
                return;
            }
        }

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Visual";
        Destroy(cube.GetComponent<Collider>());
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localCenter;
        cube.transform.localScale = size;
        cube.GetComponent<Renderer>().sharedMaterial = fallback;
    }
}
