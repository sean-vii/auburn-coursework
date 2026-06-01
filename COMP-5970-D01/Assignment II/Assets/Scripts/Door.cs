using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class Door : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;

    private SpriteRenderer sr;
    private Collider2D col;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    private void Start()
    {
        UpdateState();
        Key.OnCollected += UpdateState;
    }

    private void OnDestroy()
    {
        Key.OnCollected -= UpdateState;
    }

    private void UpdateState()
    {
        bool open = Key.IsCollected;
        if (sr != null)
        {
            sr.sprite = open ? openSprite : closedSprite;
        }
        // Closed door = solid wall the player runs into.
        // Open door = trigger the player walks through to win.
        col.isTrigger = open;

        // If the door just became a trigger and the player is already standing
        // inside it (e.g. they grabbed the key right next to the door),
        // OnTriggerEnter2D won't fire on its own — check overlap manually.
        if (open && PlayerIsOverlapping())
        {
            TriggerWin();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!Key.IsCollected) return;
        if (other.GetComponentInParent<PlayerMovement>() == null) return;
        TriggerWin();
    }

    private bool PlayerIsOverlapping()
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(col.bounds.center, col.bounds.size, 0f);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == col) continue;
            if (hits[i].GetComponentInParent<PlayerMovement>() != null) return true;
        }
        return false;
    }

    private void TriggerWin()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowWin();
        }
        else
        {
            Debug.Log("Player reached open door — WIN!");
        }
    }
}
