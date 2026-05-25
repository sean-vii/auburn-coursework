using UnityEngine;

public class FallingObject : MonoBehaviour
{
    float fallSpeed = 5f;
    float destroyY = -5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        if(transform.position.y < destroyY)
        {
            Destroy(gameObject);
        }
    }
}
