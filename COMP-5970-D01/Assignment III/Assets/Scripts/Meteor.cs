using UnityEngine;

// A hazard that flies in a straight line toward where the player was when it spawned.
// Hitting the player ends the game immediately. Meteors are indestructible (player
// bullets ignore them) so the player must dodge.
public class Meteor : MonoBehaviour
{
    public float moveSpeed = 3.5f;
    public float rotationSpeed = 90f;

    Vector3 moveDir = Vector3.zero;

    // Called by the spawner. Locks in a heading toward the player's current position.
    public void SetDirection(Vector3 dir)
    {
        if (dir.sqrMagnitude > 0.0001f) moveDir = dir.normalized;
    }

    void Start()
    {
        // Fallback: if no direction was provided, aim at the player now.
        if (moveDir == Vector3.zero)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) SetDirection(player.transform.position - transform.position);
            else moveDir = Vector3.up;
        }
    }

    void Update()
    {
        transform.position += moveDir * moveSpeed * Time.deltaTime;
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        // Despawn once it has clearly left the play area.
        Vector3 p = transform.position;
        if (p.y > 7f || p.y < -8f || Mathf.Abs(p.x) > 12f)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null) GameManager.Instance.PlayerHitByMeteor();
            Destroy(gameObject);
        }
    }
}
