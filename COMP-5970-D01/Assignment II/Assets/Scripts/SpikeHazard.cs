using UnityEngine;

// Attach to any GameObject whose Collider2D / TilemapCollider2D should kill the player on contact.
// Works whether the collider is a trigger (OnTriggerEnter2D) or solid (OnCollisionEnter2D).
public class SpikeHazard : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryKill(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryKill(collision.gameObject);
    }

    private void TryKill(GameObject other)
    {
        if (other.GetComponent<PlayerMovement>() == null) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowGameOver();
        }
        else
        {
            Debug.Log($"Player touched hazard '{name}' — player dies.");
        }
    }
}
