using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject fallingObjectPrefab;
    float spawnInterval = 1f;
    float timer = 0f;

    SafeAreaManager safeArea;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        safeArea = FindFirstObjectByType<SafeAreaManager>();
    }

    void SpawnFallingObject()
    {
        float xPos = Random.Range(safeArea.LeftX, safeArea.RightX);
        Vector3 spawnPos = new Vector3(xPos, transform.position.y, 0f);
        Instantiate(fallingObjectPrefab, spawnPos, Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if(timer >= spawnInterval)
        {
            SpawnFallingObject();
            timer = 0f;
        }
    }
}
