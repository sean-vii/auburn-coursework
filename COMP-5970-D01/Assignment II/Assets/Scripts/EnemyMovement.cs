using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Decisions")]
    [Tooltip("Seconds between movement decisions.")]
    [SerializeField] private float decisionInterval = 2.5f;
    [Range(0f, 1f)] [SerializeField] private float chanceLeft = 0.4f;
    [Range(0f, 1f)] [SerializeField] private float chanceRight = 0.4f;
    // Stay-still chance is whatever is left over (default 0.2).

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float minMoveDistance = 1f;
    [SerializeField] private float maxMoveDistance = 4f;

    [Header("Edge Detection")]
    [Tooltip("How far below the future foot position to look for ground.")]
    [SerializeField] private float edgeCheckDistance = 1f;
    [Tooltip("Leave as 'Nothing' to accept any collider as ground.")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Sprites")]
    [SerializeField] private Sprite idleSprite;
    [Tooltip("Three walk frames, played in order on repeat while moving.")]
    [SerializeField] private Sprite[] walkSprites;
    [SerializeField] private float walkFrameTime = 0.15f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D col;

    private float decisionTimer;
    private float currentDirection;   // -1, 0, +1
    private float remainingDistance;
    private bool facingRight = true;

    private int walkFrame;
    private float walkTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        // Same wall-stick safeguard as the player.
        PhysicsMaterial2D noFriction = new PhysicsMaterial2D("EnemyNoFriction")
        {
            friction = 0f,
            bounciness = 0f
        };
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
        {
            c.sharedMaterial = noFriction;
        }
    }

    private void Start()
    {
        ChooseAction();
    }

    private void Update()
    {
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (remainingDistance > 0f && Mathf.Abs(currentDirection) > 0.01f)
        {
            float step = moveSpeed * Time.fixedDeltaTime;
            if (step >= remainingDistance)
            {
                step = remainingDistance;
                remainingDistance = 0f;
            }
            else
            {
                remainingDistance -= step;
            }
            rb.linearVelocity = new Vector2(currentDirection * moveSpeed, rb.linearVelocity.y);

            if (remainingDistance <= 0f)
            {
                currentDirection = 0f;
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            return;
        }

        // Idle phase between actions: wait for the next decision.
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        decisionTimer += Time.fixedDeltaTime;
        if (decisionTimer >= decisionInterval)
        {
            decisionTimer = 0f;
            ChooseAction();
        }
    }

    private void ChooseAction()
    {
        float roll = Random.value;
        float direction;
        if (roll < chanceLeft) direction = -1f;
        else if (roll < chanceLeft + chanceRight) direction = 1f;
        else direction = 0f;

        if (direction == 0f)
        {
            currentDirection = 0f;
            remainingDistance = 0f;
            return;
        }

        float distance = Random.Range(minMoveDistance, maxMoveDistance);

        // Edge check: if moving this distance in this direction lands on no ground, flip the direction.
        // If the flipped direction is ALSO an edge (e.g. short platform), stay still this round.
        if (WouldFallOffEdge(direction, distance))
        {
            direction = -direction;
            if (WouldFallOffEdge(direction, distance))
            {
                currentDirection = 0f;
                remainingDistance = 0f;
                return;
            }
        }

        currentDirection = direction;
        remainingDistance = distance;
        facingRight = direction > 0f;
    }

    private bool WouldFallOffEdge(float direction, float distance)
    {
        // Cast from just inside the foot, downward through the foot and into the ground below.
        Vector2 origin = new Vector2(
            transform.position.x + direction * distance,
            col.bounds.min.y + 0.05f
        );

        int mask = groundLayer.value == 0 ? ~0 : groundLayer.value;
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, edgeCheckDistance + 0.1f, mask);

        // Any hit that ISN'T our own collider counts as ground.
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider != col) return false;
        }
        return true;
    }

    private void UpdateAnimation()
    {
        bool moving = Mathf.Abs(currentDirection) > 0.01f && remainingDistance > 0f;

        if (moving && walkSprites != null && walkSprites.Length > 0)
        {
            walkTimer += Time.deltaTime;
            if (walkTimer >= walkFrameTime)
            {
                walkTimer -= walkFrameTime;
                walkFrame = (walkFrame + 1) % walkSprites.Length;
            }
            sr.sprite = walkSprites[walkFrame];
        }
        else if (idleSprite != null)
        {
            sr.sprite = idleSprite;
            walkTimer = 0f;
            walkFrame = 0;
        }

        sr.flipX = !facingRight;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<PlayerMovement>() == null) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowGameOver();
        }
        else
        {
            Debug.Log($"Player touched enemy '{name}' — player dies.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D c = col != null ? col : GetComponent<Collider2D>();
        if (c == null) return;

        Gizmos.color = Color.yellow;
        Vector2 leftFoot = new Vector2(transform.position.x - maxMoveDistance, c.bounds.min.y - 0.05f);
        Vector2 rightFoot = new Vector2(transform.position.x + maxMoveDistance, c.bounds.min.y - 0.05f);
        Gizmos.DrawLine(leftFoot, leftFoot + Vector2.down * edgeCheckDistance);
        Gizmos.DrawLine(rightFoot, rightFoot + Vector2.down * edgeCheckDistance);
    }
}
