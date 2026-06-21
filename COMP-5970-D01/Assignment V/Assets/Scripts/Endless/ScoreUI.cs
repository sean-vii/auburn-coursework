using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds and drives the runtime UI: a live survival-score readout while playing
/// and a game-over panel with the final score and a restart button. Uses uGUI
/// Text with a dynamically created OS font so no font asset import is required.
/// </summary>
public class ScoreUI : MonoBehaviour
{
    Text scoreText;
    GameObject gameOverPanel;
    Text finalText;
    Font font;

    public void Build()
    {
        font = Font.CreateDynamicFontFromOSFont(
            new[] { "Arial", "Segoe UI", "Liberation Sans", "Helvetica", "Verdana" }, 28);

        // --- Canvas ---------------------------------------------------------
        GameObject canvasGo = new GameObject("HUD Canvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
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
        Button button = btnGo.AddComponent<Button>();
        button.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Restart();
            }
        });

        RectTransform rt = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -120f);
        rt.sizeDelta = new Vector2(360f, 90f);

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

        if (gm.IsGameOver && !gameOverPanel.activeSelf)
        {
            gameOverPanel.SetActive(true);
            finalText.text =
                "<b>GAME OVER</b>\n\n" +
                "Score: " + gm.Score + "\n" +
                "Distance: " + Mathf.FloorToInt(gm.Distance) + "m   " +
                "Time: " + Mathf.FloorToInt(gm.TimeSurvived) + "s\n\n" +
                "<size=26>Press R or click Restart</size>";
        }
    }
}
