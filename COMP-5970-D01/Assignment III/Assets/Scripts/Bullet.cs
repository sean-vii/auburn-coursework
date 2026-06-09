using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f; 

    void Update()
    {
        transform.Translate(Vector3.up * speed * Time.deltaTime);

        if (transform.position.y > 10f)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.EnemyDestroyed(other.transform.position);
            Destroy(other.gameObject);
            Destroy(gameObject);
        }
    }
}