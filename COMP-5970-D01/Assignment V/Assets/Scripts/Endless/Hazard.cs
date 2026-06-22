using UnityEngine;

/// <summary>
/// A single hazard / pickup behaviour driven by <see cref="type"/>. The platform
/// generator stamps these onto sections. Four distinct types are provided (the
/// assignment requires at least three besides falling off):
///   * Kill    - lethal obstacle, instant game over on contact.
///   * Slow    - field that slows the player's forward speed.
///   * Bumper  - knocks the player sideways, often off the platform.
///   * Reverse - inverts the player's steering for a few seconds.
/// A non-hazard Boost pad is also included as a bonus pickup.
/// </summary>
public class Hazard : MonoBehaviour
{
    public enum HazardType { Kill, Slow, Bumper, Reverse, Boost }

    public HazardType type = HazardType.Kill;

    public float slowMultiplier = 0.4f;
    public float slowDuration = 1.5f;
    public float reverseDuration = 2.5f;
    public float boostMultiplier = 1.7f;
    public float boostDuration = 2f;
    public float pushSpeed = 9f;

    void Update()
    {
        // Spin the boost coin so it reads as a collectible.
        if (type == HazardType.Boost)
        {
            transform.Rotate(0f, 120f * Time.deltaTime, 0f, Space.Self);
        }
    }

    // Kill obstacles are solid (collision); zones/pads are triggers.
    void OnCollisionEnter(Collision collision)
    {
        Affect(collision.collider);
    }

    void OnTriggerEnter(Collider other)
    {
        Affect(other);
    }

    void Affect(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        PlayerController player = other.GetComponent<PlayerController>();

        switch (type)
        {
            case HazardType.Kill:
                ShowEffect("CRASH!", new Color(0.95f, 0.30f, 0.30f));
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.GameOver();
                }
                break;

            case HazardType.Slow:
                if (player != null)
                {
                    player.SetSpeedMultiplier(slowMultiplier, slowDuration);
                }
                ShowEffect("SLOW!", new Color(0.40f, 0.70f, 1f));
                break;

            case HazardType.Bumper:
                if (player != null)
                {
                    float dir = (other.transform.position.x >= transform.position.x) ? 1f : -1f;
                    player.Push(new Vector3(dir * pushSpeed, 3f, 0f));
                }
                ShowEffect("BUMP!", new Color(1f, 0.85f, 0.30f));
                break;

            case HazardType.Reverse:
                if (player != null)
                {
                    player.ApplyReverseControls(reverseDuration);
                }
                ShowEffect("REVERSED!", new Color(0.85f, 0.45f, 1f));
                break;

            case HazardType.Boost:
                if (player != null)
                {
                    player.SetSpeedMultiplier(boostMultiplier, boostDuration);
                }
                ShowEffect("BOOST!", new Color(0.40f, 1f, 0.50f));
                Destroy(gameObject); // consume the one-shot bonus
                break;
        }
    }

    static void ShowEffect(string message, Color color)
    {
        if (ScoreUI.Instance != null)
        {
            ScoreUI.Instance.ShowEffect(message, color);
        }
    }
}
