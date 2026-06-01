using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Key : MonoBehaviour
{
    public static bool IsCollected { get; private set; }
    public static event System.Action OnCollected;

    private void Awake()
    {
        // Reset whenever a Key is loaded — handles scene reload after death/win.
        IsCollected = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerMovement>() == null) return;

        IsCollected = true;
        OnCollected?.Invoke();
        gameObject.SetActive(false);
    }
}
