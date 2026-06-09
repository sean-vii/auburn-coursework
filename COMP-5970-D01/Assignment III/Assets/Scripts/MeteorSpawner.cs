using UnityEngine;

// Spawns meteor hazards from the lower half of the screen (entering from the
// lower-left / lower-right) and aims each one at the player.
public class MeteorSpawner : MonoBehaviour
{
    public GameObject meteorPrefab;
    public float spawnRate = 2.5f;

    [Header("Spawn band (lower half of the screen)")]
    public float edgeX = 6f;     // horizontal distance from center to spawn from (screen edge-ish)
    public float minY = -4.8f;   // bottom of the lower half
    public float maxY = -0.5f;   // up to roughly screen center

    float nextSpawnTime;

    void Update()
    {
        if (GameManager.IsGameOver) return;

        if (Time.time >= nextSpawnTime)
        {
            SpawnMeteor();
            nextSpawnTime = Time.time + spawnRate;
        }
    }

    void SpawnMeteor()
    {
        if (meteorPrefab == null) return;

        bool fromLeft = Random.value > 0.5f;
        float x = fromLeft ? -edgeX : edgeX;
        float y = Random.Range(minY, maxY);
        Vector3 spawnPos = new Vector3(x, y, 0f);

        GameObject meteor = Instantiate(meteorPrefab, spawnPos, Quaternion.identity);

        Meteor m = meteor.GetComponent<Meteor>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (m != null && player != null)
            m.SetDirection(player.transform.position - spawnPos);
    }
}
