using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central state machine for the endless survival mode.
/// Tracks the survival score (built from distance travelled, time survived and
/// platform sections passed), owns the lose state, and handles the restart flow.
/// A single instance is created by <see cref="GameBootstrap"/> at runtime.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool IsGameOver { get; private set; }
    public int Score { get; private set; }
    public float Distance { get; private set; }
    public float TimeSurvived { get; private set; }
    public int SectionsPassed { get; private set; }

    Transform player;
    float startZ;
    float sectionLength = 20f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        IsGameOver = false;
    }

    /// <summary>Called by the bootstrap once the player exists.</summary>
    public void Init(Transform playerTransform, float sectionLengthUnits)
    {
        player = playerTransform;
        startZ = player.position.z;
        sectionLength = Mathf.Max(1f, sectionLengthUnits);
    }

    void Update()
    {
        if (IsGameOver)
        {
            // Restart option after losing (also exposed as a UI button by ScoreUI).
            if (Input.GetKeyDown(KeyCode.R))
            {
                Restart();
            }
            return;
        }

        TimeSurvived += Time.deltaTime;

        if (player != null)
        {
            // Distance only ever counts forward progress.
            Distance = Mathf.Max(Distance, player.position.z - startZ);
            SectionsPassed = Mathf.Max(0, Mathf.FloorToInt(Distance / sectionLength));
        }

        // Survival score blends all three allowed metrics.
        Score = Mathf.FloorToInt(Distance)
              + Mathf.FloorToInt(TimeSurvived * 3f)
              + SectionsPassed * 5;
    }

    /// <summary>Triggered when the player falls off or hits a lethal hazard.</summary>
    public void GameOver()
    {
        if (IsGameOver)
        {
            return;
        }
        IsGameOver = true;
    }

    /// <summary>Reloads the active scene for a clean restart.</summary>
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
