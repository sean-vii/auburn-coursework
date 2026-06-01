using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    [Header("Ground Check (optional)")]
    [Tooltip("Optional. If left empty, ground is detected via collision contacts (any surface beneath the player).")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [Tooltip("Leave as 'Nothing' to accept any collider beneath the player.")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Sprites")]
    [Tooltip("tile_0260 1")]
    [SerializeField] private Sprite idleSprite;
    [Tooltip("tile_0261 1, tile_0262 1, tile_0263 1 (in order)")]
    [SerializeField] private Sprite[] walkSprites;
    [Tooltip("tile_0264 1")]
    [SerializeField] private Sprite jumpSprite;

    [Header("Animation")]
    [SerializeField] private float walkFrameTime = 0.12f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private float horizontal;
    private bool grounded;
    private bool facingRight = true;
    private int walkFrame;
    private float walkTimer;

    // Contact-based grounding/wall tracking from collision normals.
    private readonly HashSet<Collider2D> groundContacts = new HashSet<Collider2D>();
    private readonly HashSet<Collider2D> leftWallContacts = new HashSet<Collider2D>();   // wall to the player's left (normal points right)
    private readonly HashSet<Collider2D> rightWallContacts = new HashSet<Collider2D>();  // wall to the player's right (normal points left)

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        // Frictionless material so the player doesn't stick to walls when pressed against them mid-air.
        PhysicsMaterial2D noFriction = new PhysicsMaterial2D("PlayerNoFriction")
        {
            friction = 0f,
            bounciness = 0f
        };
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
        {
            col.sharedMaterial = noFriction;
        }
    }

    private void Update()
    {
        ReadInput();
        UpdateFacing();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        grounded = CheckGrounded();

        // Don't push horizontally into a wall while airborne — otherwise the wall's
        // normal force generates friction that holds the player in mid-air.
        float h = horizontal;
        if (!grounded)
        {
            if (h > 0f && rightWallContacts.Count > 0) h = 0f;
            else if (h < 0f && leftWallContacts.Count > 0) h = 0f;
        }

        rb.linearVelocity = new Vector2(h * moveSpeed, rb.linearVelocity.y);
    }

    private void ReadInput()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null)
        {
            horizontal = 0f;
            return;
        }

        float h = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
        horizontal = h;

        bool jumpPressed =
            kb.spaceKey.wasPressedThisFrame ||
            kb.wKey.wasPressedThisFrame ||
            kb.upArrowKey.wasPressedThisFrame;

        if (jumpPressed && grounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    private bool CheckGrounded()
    {
        if (groundCheck != null && groundLayer.value != 0)
        {
            if (Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer))
                return true;
        }
        return groundContacts.Count > 0;
    }

    private void OnCollisionEnter2D(Collision2D collision) => EvaluateContact(collision);
    private void OnCollisionStay2D(Collision2D collision) => EvaluateContact(collision);

    private void OnCollisionExit2D(Collision2D collision)
    {
        groundContacts.Remove(collision.collider);
        leftWallContacts.Remove(collision.collider);
        rightWallContacts.Remove(collision.collider);
    }

    private void EvaluateContact(Collision2D collision)
    {
        // Wall contacts that occur near the player's feet are almost always tilemap-seam
        // corner hits, not real walls — filter them out so they don't block movement.
        Bounds myBounds = collision.otherCollider.bounds;
        float feetCutoff = myBounds.min.y + myBounds.size.y * 0.2f;

        bool isGround = false;
        bool isLeftWall = false;
        bool isRightWall = false;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            Vector2 n = contact.normal;

            if (n.y > 0.7f)
            {
                isGround = true;
                continue;
            }
            if (contact.point.y < feetCutoff) continue;
            if (n.x > 0.7f) isLeftWall = true;
            else if (n.x < -0.7f) isRightWall = true;
        }

        if (isGround) groundContacts.Add(collision.collider);
        else groundContacts.Remove(collision.collider);

        if (isLeftWall) leftWallContacts.Add(collision.collider);
        else leftWallContacts.Remove(collision.collider);

        if (isRightWall) rightWallContacts.Add(collision.collider);
        else rightWallContacts.Remove(collision.collider);
    }

    private void UpdateFacing()
    {
        if (horizontal > 0.01f) facingRight = true;
        else if (horizontal < -0.01f) facingRight = false;
        sr.flipX = !facingRight;
    }

    private void UpdateAnimation()
    {
        if (!grounded && jumpSprite != null)
        {
            sr.sprite = jumpSprite;
            walkTimer = 0f;
            walkFrame = 0;
            return;
        }

        if (Mathf.Abs(horizontal) > 0.01f && walkSprites != null && walkSprites.Length > 0)
        {
            walkTimer += Time.deltaTime;
            if (walkTimer >= walkFrameTime)
            {
                walkTimer -= walkFrameTime;
                walkFrame = (walkFrame + 1) % walkSprites.Length;
            }
            sr.sprite = walkSprites[walkFrame];
            return;
        }

        if (idleSprite != null)
        {
            sr.sprite = idleSprite;
        }
        walkTimer = 0f;
        walkFrame = 0;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
