using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// The core rule of Darkless: if the player stays in the dark too long, they die.
//
// Each frame we decide whether the player is LIT:
//   * it's daytime (the sun lights the world, the monster is dormant), OR
//   * the player is standing inside any LightZone (campfire now; torches/lanterns later).
// Lit  -> the light meter refills.
// Dark -> it drains. When it hits zero the player dies (for now, the scene reloads).
//
// Put this on the PlayerArmature.
public class PlayerDarkness : MonoBehaviour
{
    [Header("Survival")]
    [Tooltip("Seconds of total darkness the player can survive, starting from full. " +
             "Small enough that lingering kills you, big enough to dash through a dark patch.")]
    public float secondsInDarkToDie = 5f;

    [Tooltip("How much faster the meter refills in the light than it drains in the dark. " +
             "2 = recovers twice as fast as it drains.")]
    public float recoverMultiplier = 2f;

    [Header("Optional feedback")]
    [Tooltip("A full-screen black UI Image. Its transparency rises as you get closer to death, " +
             "so the screen fades to black. Leave empty and the system still works.")]
    public Image darknessOverlay;

    // 1 = fully safe, 0 = dead. Public so a UI meter or other systems can read it later.
    public float Light01 { get; private set; } = 1f;

    DayNightCycle dayNight;
    bool isDead;

    void Start()
    {
        // Find the day/night cycle so we can tell whether it's currently night.
        dayNight = FindFirstObjectByType<DayNightCycle>();
        Light01 = 1f;
        isDead = false;
    }

    void Update()
    {
        if (isDead) return;

        // Drain rate is set so that a full meter empties in exactly secondsInDarkToDie seconds.
        float drainPerSecond = 1f / Mathf.Max(0.01f, secondsInDarkToDie);

        if (IsLit())
            Light01 += drainPerSecond * recoverMultiplier * Time.deltaTime; // recover
        else
            Light01 -= drainPerSecond * Time.deltaTime;                     // drain

        Light01 = Mathf.Clamp01(Light01);

        // Fade the screen toward black as the meter empties (only if an overlay is wired up).
        if (darknessOverlay != null)
        {
            Color c = darknessOverlay.color;
            c.a = 1f - Light01;
            darknessOverlay.color = c;
        }

        if (Light01 <= 0f)
            Die();
    }

    // Is the player safe from the dark right now?
    bool IsLit()
    {
        // Daytime is always safe.
        if (dayNight != null && !dayNight.IsNight)
            return true;

        // At night, safe only inside a light source.
        return LightZone.AnyCovers(transform.position);
    }

    void Die()
    {
        isDead = true;
        // Simplest testable death: restart the scene. A proper game-over screen comes later.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
