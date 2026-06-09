using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Central game state: health, score, HUD, audio and the game-over / restart loop.
// The HUD (score text + health icons + game-over banner) is built entirely in code,
// so no Canvas needs to be wired up in the scene.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool IsGameOver { get; private set; }

    [Header("Health")]
    public int maxHealth = 3;
    public Sprite healthIconSprite;

    [Header("Scoring")]
    public int pointsPerEnemy = 100;

    [Header("Audio")]
    public AudioClip explosionClip;   // played when an enemy is destroyed and when the player loses
    public AudioClip playerHitClip;   // played on a non-fatal enemy-bullet hit

    [Header("Restart")]
    public float restartDelay = 1.5f;

    int currentHealth;
    int score;
    AudioSource audioSource;

    // HUD
    Text scoreText;
    Text gameOverText;
    readonly List<GameObject> healthIcons = new List<GameObject>();
    Font uiFont;

    void Awake()
    {
        Instance = this;
        IsGameOver = false;
        currentHealth = maxHealth;
        score = 0;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        BuildHud();
        UpdateScoreText();
        UpdateHealthIcons();
    }

    // ---------- Public game events ----------

    public void EnemyDestroyed(Vector3 position)
    {
        if (IsGameOver) return;
        score += pointsPerEnemy;
        UpdateScoreText();
        PlayClip(explosionClip);
    }

    public void PlayerHitByBullet()
    {
        if (IsGameOver) return;
        currentHealth--;
        UpdateHealthIcons();
        PlayClip(playerHitClip);
        if (currentHealth <= 0) GameOver();
    }

    // A meteor strike is instantly fatal.
    public void PlayerHitByMeteor()
    {
        if (IsGameOver) return;
        currentHealth = 0;
        UpdateHealthIcons();
        GameOver();
    }

    // ---------- Internal ----------

    void GameOver()
    {
        IsGameOver = true;
        PlayClip(explosionClip);
        if (gameOverText != null) gameOverText.gameObject.SetActive(true);
        Invoke(nameof(Restart), restartDelay);
    }

    void Restart()
    {
        IsGameOver = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void PlayClip(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
    }

    // ---------- HUD construction ----------

    void BuildHud()
    {
        GameObject canvasGO = new GameObject("HUD Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Score (top-left)
        scoreText = CreateText("ScoreText", canvasGO.transform, 48, TextAnchor.UpperLeft);
        RectTransform srt = scoreText.rectTransform;
        srt.anchorMin = srt.anchorMax = new Vector2(0f, 1f);
        srt.pivot = new Vector2(0f, 1f);
        srt.sizeDelta = new Vector2(600f, 90f);
        srt.anchoredPosition = new Vector2(30f, -20f);

        // Health icons (top-right, filling leftward)
        const float iconSize = 64f;
        const float spacing = 14f;
        for (int i = 0; i < maxHealth; i++)
        {
            GameObject iconGO = new GameObject("HealthIcon" + i);
            iconGO.transform.SetParent(canvasGO.transform, false);
            Image img = iconGO.AddComponent<Image>();
            img.sprite = healthIconSprite;
            img.preserveAspect = true;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(iconSize, iconSize);
            rt.anchoredPosition = new Vector2(-30f - i * (iconSize + spacing), -20f);
            healthIcons.Add(iconGO);
        }

        // Game-over banner (center, hidden until needed)
        gameOverText = CreateText("GameOverText", canvasGO.transform, 90, TextAnchor.MiddleCenter);
        RectTransform grt = gameOverText.rectTransform;
        grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0.5f);
        grt.pivot = new Vector2(0.5f, 0.5f);
        grt.sizeDelta = new Vector2(900f, 220f);
        grt.anchoredPosition = Vector2.zero;
        gameOverText.text = "GAME OVER";
        gameOverText.color = new Color(1f, 0.3f, 0.3f, 1f);
        gameOverText.gameObject.SetActive(false);
    }

    Text CreateText(string name, Transform parent, int fontSize, TextAnchor anchor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.font = uiFont;
        t.fontSize = fontSize;
        t.fontStyle = FontStyle.Bold;
        t.alignment = anchor;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    void UpdateScoreText()
    {
        if (scoreText != null) scoreText.text = "Score: " + score;
    }

    void UpdateHealthIcons()
    {
        for (int i = 0; i < healthIcons.Count; i++)
            healthIcons[i].SetActive(i < currentHealth);
    }
}
