using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;

    public float spawnRate = 2f;
    public float minY = 1.5f;
    public float maxY = 4.2f;

    float nextSpawnTime = 0f;

    void Update()
    {
        if (GameManager.IsGameOver) return;

        if (Time.time >= nextSpawnTime)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + spawnRate;
        }
    }

    void SpawnEnemy()
    {
        bool spawnFromLeft = Random.value > 0.5f;

        float x = spawnFromLeft ? -3.5f : 3.5f;
        float y = Random.Range(minY, maxY);

        GameObject enemy = Instantiate(enemyPrefab, new Vector3(x, y, 0f), Quaternion.Euler(0f, 0f, 180f));

        Enemy enemyScript = enemy.GetComponent<Enemy>();

        if (!spawnFromLeft)
        {
            enemyScript.moveSpeed *= -1f;
        }
    }
}