using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Builds and drives the runtime UI: a live survival-score readout while playing
/// and a game-over panel with the final score and a restart button. Uses uGUI
/// Text with a dynamically created OS font so no font asset import is required.
/// The restart button is hit-tested manually against the new Input System mouse,
/// so it works without an EventSystem / input module being configured.
/// </summary>
public class ScoreUI : MonoBehaviour
{
    public static ScoreUI Instance { get; private set; }

    Text scoreText;
    GameObject gameOverPanel;
    Text finalText;
    RectTransform restartRect;
    Canvas hudCanvas;
    RectTransform hudRect;
    Font font;

    void Awake()
    {
        Instance = this;
    }

    public void Build()
    {
        font = Font.CreateDynamicFontFromOSFont(
            new[] { "Arial", "Segoe UI", "Liberation Sans", "Helvetica", "Verdana" }, 28);

        // --- Canvas ---------------------------------------------------------
        GameObject canvasGo = new GameObject("HUD Canvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        hudCanvas = canvas;
        hudRect = canvasGo.GetComponent<RectTransform>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // --- Live score (top-left) -----------------------------------------
        scoreText = MakeText(canvasGo.transform, "ScoreText", 32, TextAnchor.UpperLeft);
        RectTransform scoreRt = scoreText.rectTransform;
        scoreRt.anchorMin = new Vector2(0f, 1f);
        scoreRt.anchorMax = new Vector2(0f, 1f);
        scoreRt.pivot = new Vector2(0f, 1f);
        scoreRt.anchoredPosition = new Vector2(30f, -24f);
        scoreRt.sizeDelta = new Vector2(900f, 200f);

        // --- Game-over panel ------------------------------------------------
        gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.SetParent(canvasGo.transform, false);
        Image bg = gameOverPanel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);
        RectTransform panelRt = bg.rectTransform;
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        finalText = MakeText(gameOverPanel.transform, "FinalText", 48, TextAnchor.MiddleCenter);
        RectTransform finalRt = finalText.rectTransform;
        finalRt.anchorMin = new Vector2(0.5f, 0.5f);
        finalRt.anchorMax = new Vector2(0.5f, 0.5f);
        finalRt.pivot = new Vector2(0.5f, 0.5f);
        finalRt.anchoredPosition = new Vector2(0f, 80f);
        finalRt.sizeDelta = new Vector2(1200f, 400f);

        BuildRestartButton(gameOverPanel.transform);

        gameOverPanel.SetActive(false);
    }

    void BuildRestartButton(Transform parent)
    {
        GameObject btnGo = new GameObject("RestartButton");
        btnGo.transform.SetParent(parent, false);
        Image img = btnGo.AddComponent<Image>();
        img.color = new Color(0.25f, 0.7f, 0.35f, 1f);

        restartRect = img.rectTransform;
        restartRect.anchorMin = new Vector2(0.5f, 0.5f);
        restartRect.anchorMax = new Vector2(0.5f, 0.5f);
        restartRect.pivot = new Vector2(0.5f, 0.5f);
        restartRect.anchoredPosition = new Vector2(0f, -120f);
        restartRect.sizeDelta = new Vector2(360f, 90f);

        Text label = MakeText(btnGo.transform, "Label", 32, TextAnchor.MiddleCenter);
        label.text = "RESTART";
        RectTransform labelRt = label.rectTransform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
    }

    Text MakeText(Transform parent, string name, int size, TextAnchor anchor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = true;
        return text;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            return;
        }

        scoreText.text =
            "<b>Score:</b> " + gm.Score +
            "\n<b>Time:</b> " + Mathf.FloorToInt(gm.TimeSurvived) + "s" +
            "\n<b>Distance:</b> " + Mathf.FloorToInt(gm.Distance) + "m" +
            "\n<b>Sections:</b> " + gm.SectionsPassed;

        if (gm.IsGameOver)
        {
            if (!gameOverPanel.activeSelf)
            {
                gameOverPanel.SetActive(true);
                finalText.text =
                    "<b>GAME OVER</b>\n\n" +
                    "Score: " + gm.Score + "\n" +
                    "Distance: " + Mathf.FloorToInt(gm.Distance) + "m   " +
                    "Time: " + Mathf.FloorToInt(gm.TimeSurvived) + "s\n\n" +
                    "<size=26>Press R or click Restart</size>";
            }

            HandleRestartClick(gm);
        }
    }

    void HandleRestartClick(GameManager gm)
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
        {
            return;
        }

        Vector2 screenPoint = mouse.position.ReadValue();
        // Canvas is Screen Space - Overlay, so the camera argument is null.
        if (RectTransformUtility.RectangleContainsScreenPoint(restartRect, screenPoint, null))
        {
            gm.Restart();
        }
    }

    // --- Floating effect text ----------------------------------------------

    /// <summary>
    /// Pops a short label above the ball that fades/slides in then drifts up and
    /// fades out. Called by hazards whenever an effect is applied to the player.
    /// </summary>
    public void ShowEffect(string message, Color color)
    {
        if (hudCanvas == null || font == null)
        {
            return;
        }

        Vector2 anchored = GetBallAnchoredPosition();

        GameObject go = new GameObject("Effect");
        go.transform.SetParent(hudCanvas.transform, false);

        Text text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = 44;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = true;
        text.text = message;

        RectTransform rt = text.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(500f, 100f);

        CanvasGroup group = go.AddComponent<CanvasGroup>();
        StartCoroutine(AnimateEffect(rt, group, anchored));
    }

    Vector2 GetBallAnchoredPosition()
    {
        GameManager gm = GameManager.Instance;
        Camera cam = Camera.main;
        if (gm != null && gm.Player != null && cam != null && hudRect != null)
        {
            Vector3 worldAbove = gm.Player.position + Vector3.up * 1.6f;
            Vector3 screen = cam.WorldToScreenPoint(worldAbove);
            if (screen.z > 0f)
            {
                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        hudRect, screen, null, out local))
                {
                    return local;
                }
            }
        }
        return new Vector2(0f, 120f); // fallback: just above centre
    }

    IEnumerator AnimateEffect(RectTransform rt, CanvasGroup group, Vector2 start)
    {
        const float duration = 1.1f;
        const float rise = 80f;
        float t = 0f;

        while (rt != null && t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;

            // Slide up over the lifetime.
            rt.anchoredPosition = start + new Vector2(0f, Mathf.Lerp(0f, rise, k));

            // Fade in quickly, then fade out.
            group.alpha = (k < 0.15f) ? (k / 0.15f) : (1f - (k - 0.15f) / 0.85f);

            yield return null;
        }

        if (rt != null)
        {
            Destroy(rt.gameObject);
        }
    }
}
