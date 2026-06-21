using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Endless procedural platform generator. Spawns platform sections ahead of the
/// player and recycles (destroys) sections that fall behind. Four distinct
/// platform "prefabs" (built procedurally from primitives) are used, and hazards
/// from <see cref="Hazard"/> are stamped onto them with rising difficulty.
/// </summary>
public class PlatformGenerator : MonoBehaviour
{
    public const float SectionLength = 20f;

    [Header("Streaming distances")]
    public float spawnAhead = 90f;
    public float despawnBehind = 30f;

    [Header("Difficulty")]
    public int safeSections = 2;   // first sections are flat with no hazards

    Transform player;
    float nextStartZ;
    int sectionIndex;

    class Section
    {
        public GameObject root;
        public float endZ;
    }

    readonly List<Section> sections = new List<Section>();

    // Cached materials so we are not allocating one per object.
    Material matFloorA, matFloorB, matFloorC, matFloorD;
    Material matKill, matSlow, matBumper, matReverse, matBoost;

    /// <summary>Called by the bootstrap. Sets the player and fills the start runway.</summary>
    public void Initialize(Transform playerTransform)
    {
        player = playerTransform;
        BuildMaterials();

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

        // Variant 0 is reserved for the safe starting runway; afterwards pick at random.
        int variant = (index < safeSections) ? 0 : Random.Range(0, 4);

        switch (variant)
        {
            case 0: BuildFlat(root.transform, matFloorA); break;
            case 1: BuildNarrow(root.transform, matFloorB); break;
            case 2: BuildSplit(root.transform, matFloorC); break;
            default: BuildOffset(root.transform, matFloorD); break;
        }

        if (index >= safeSections)
        {
            AddHazards(root.transform, variant, index);
        }

        sections.Add(new Section { root = root, endZ = startZ + SectionLength });
    }

    // --- Four platform variants --------------------------------------------

    void BuildFlat(Transform parent, Material mat)
    {
        AddFloor(parent, 0f, 10f, mat);
    }

    void BuildNarrow(Transform parent, Material mat)
    {
        AddFloor(parent, 0f, 4f, mat);
    }

    // Two side strips with a deadly gap down the middle.
    void BuildSplit(Transform parent, Material mat)
    {
        AddFloor(parent, -3.5f, 3f, mat);
        AddFloor(parent, 3.5f, 3f, mat);
    }

    // A medium lane shoved to one side, forcing the player to steer over.
    void BuildOffset(Transform parent, Material mat)
    {
        float offset = (Random.value < 0.5f) ? -3f : 3f;
        AddFloor(parent, offset, 5f, mat);
    }

    void AddFloor(Transform parent, float centerX, float width, Material mat)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(parent, false);
        // Section parent sits at the section's start; lay the floor along +Z.
        floor.transform.localPosition = new Vector3(centerX, -0.5f, SectionLength * 0.5f);
        floor.transform.localScale = new Vector3(width, 1f, SectionLength);
        floor.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // --- Hazard placement --------------------------------------------------

    void AddHazards(Transform parent, int variant, int index)
    {
        // Valid lane centers depend on the variant so hazards land on real floor.
        float[] lanes;
        switch (variant)
        {
            case 1: lanes = new float[] { -1f, 0f, 1f }; break;       // narrow
            case 2: lanes = new float[] { -3.5f, 3.5f }; break;       // split strips
            case 3: lanes = new float[] { -3f, 3f }; break;           // offset (either side it might be)
            default: lanes = new float[] { -3f, 0f, 3f }; break;      // flat
        }

        int count = Mathf.Clamp(1 + index / 5, 1, 3);
        float zStart = 4f;
        float zStep = (SectionLength - 8f) / count;

        for (int i = 0; i < count; i++)
        {
            float z = zStart + zStep * i + Random.Range(0f, zStep * 0.4f);
            float x = lanes[Random.Range(0, lanes.Length)];
            Hazard.HazardType type = PickHazard();
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
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Hazard_" + type;
        go.transform.SetParent(parent, false);

        Renderer renderer = go.GetComponent<Renderer>();
        Collider col = go.GetComponent<Collider>();
        Hazard hazard = go.AddComponent<Hazard>();
        hazard.type = type;

        switch (type)
        {
            case Hazard.HazardType.Kill:
                // Solid upright obstacle the player crashes into.
                go.transform.localScale = new Vector3(1.4f, 1.6f, 1.4f);
                localPos.y = 0.8f;
                col.isTrigger = false;
                renderer.sharedMaterial = matKill;
                break;

            case Hazard.HazardType.Bumper:
                // Upright pad that knocks the player aside on contact.
                go.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                localPos.y = 0.6f;
                col.isTrigger = true;
                renderer.sharedMaterial = matBumper;
                break;

            case Hazard.HazardType.Slow:
                renderer.sharedMaterial = matSlow;
                MakePad(go, col, ref localPos);
                break;

            case Hazard.HazardType.Reverse:
                renderer.sharedMaterial = matReverse;
                MakePad(go, col, ref localPos);
                break;

            case Hazard.HazardType.Boost:
                renderer.sharedMaterial = matBoost;
                MakePad(go, col, ref localPos);
                break;
        }

        go.transform.localPosition = localPos;
    }

    // Flat trigger pad the ball rolls across.
    void MakePad(GameObject go, Collider col, ref Vector3 localPos)
    {
        go.transform.localScale = new Vector3(2.5f, 0.15f, 2.5f);
        localPos.y = 0.08f;
        col.isTrigger = true;
    }
}
