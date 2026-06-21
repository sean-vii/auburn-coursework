using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Endless rolling-ball controller. The ball is driven forward automatically and
/// the player steers left/right. Hazards can temporarily change the player's speed
/// or invert their steering, and falling below <see cref="fallY"/> is a lose
/// condition. Reads input through the new Input System (<see cref="Keyboard"/>) so
/// it works regardless of the project's active input handling setting.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public float forwardSpeed = 9f;
    public float sideSpeed = 7f;
    public float fallY = -6f;

    Rigidbody rb;
    float currentSide;
    float sideVel;

    float speedMultiplier = 1f;
    float speedTimer;

    float controlSign = 1f;
    float controlTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    // --- Hazard / pickup effects -------------------------------------------

    /// <summary>Multiply forward speed (e.g. slow field &lt; 1, boost pad &gt; 1).</summary>
    public void SetSpeedMultiplier(float multiplier, float duration)
    {
        speedMultiplier = multiplier;
        speedTimer = duration;
    }

    /// <summary>Invert steering for a few seconds.</summary>
    public void ApplyReverseControls(float duration)
    {
        controlSign = -1f;
        controlTimer = duration;
    }

    /// <summary>Instant knock (used by bumper hazards).</summary>
    public void Push(Vector3 velocityChange)
    {
        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    // -----------------------------------------------------------------------

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            return;
        }

        if (speedTimer > 0f)
        {
            speedTimer -= Time.deltaTime;
            if (speedTimer <= 0f)
            {
                speedMultiplier = 1f;
            }
        }

        if (controlTimer > 0f)
        {
            controlTimer -= Time.deltaTime;
            if (controlTimer <= 0f)
            {
                controlSign = 1f;
            }
        }

        // Falling off the platforms = lose.
        if (transform.position.y < fallY && GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            // Stop driving forward; let the ball fall/settle.
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        float h = ReadSteerInput() * controlSign;
        currentSide = Mathf.SmoothDamp(currentSide, h, ref sideVel, 0.1f);

        Vector3 velocity = new Vector3(
            currentSide * sideSpeed,
            rb.linearVelocity.y,
            forwardSpeed * speedMultiplier
        );
        rb.linearVelocity = velocity;
    }

    float ReadSteerInput()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null)
        {
            return 0f;
        }

        float h = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
        return h;
    }
}
