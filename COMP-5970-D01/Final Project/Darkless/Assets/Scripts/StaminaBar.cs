using UnityEngine;
using UnityEngine.UI;

// A stamina bar, BUILT AT RUNTIME (like the flashlight battery bar) so there's nothing to hand-place
// — just add this component to a scene object. It auto-matches the battery bar's width/height and
// sits just ABOVE it in the bottom-left. Green when full, ambers as it drops, red when exhausted.
public class StaminaBar : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("The player's stamina. Leave empty to auto-find it in the scene.")]
    public PlayerStamina stamina;

    [Header("Colours")]
    public Color backColor = new Color(0f, 0f, 0f, 0.5f);
    public Color fullColor = new Color(0.30f, 0.85f, 0.35f, 0.95f);
    public Color lowColor = new Color(0.90f, 0.70f, 0.15f, 0.95f);
    public Color exhaustedColor = new Color(0.90f, 0.20f, 0.15f, 1f);

    [Header("Layout (fallback if no battery bar is found to match)")]
    public float width = 234f;
    public float height = 9f;
    public Vector2 margin = new Vector2(60f, 60f);
    [Tooltip("Vertical gap left between this bar and the battery bar above it.")]
    public float gapToBattery = 9f;

    RectTransform back;
    Image fill;

    void Start()
    {
        if (stamina == null) stamina = FindFirstObjectByType<PlayerStamina>();
        MatchBatteryBar();
        BuildUI();
    }

    // Size/position ourselves to match the battery bar and sit right BELOW it.
    void MatchBatteryBar()
    {
        var bat = FindFirstObjectByType<FlashlightBatteryBar>();
        if (bat == null) return;
        width = bat.segmentCount * bat.segmentWidth + (bat.segmentCount - 1) * bat.spacing;
        height = bat.segmentHeight;
        margin = new Vector2(bat.margin.x, bat.margin.y - height - gapToBattery);
    }

    void BuildUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var go = new GameObject("Canvas (auto)", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        var backGO = new GameObject("StaminaBar", typeof(RectTransform), typeof(Image));
        back = backGO.GetComponent<RectTransform>();
        back.SetParent(canvas.transform, false);
        back.anchorMin = back.anchorMax = back.pivot = new Vector2(0f, 0f);
        back.anchoredPosition = margin;
        back.sizeDelta = new Vector2(width, height);
        var backImg = backGO.GetComponent<Image>();
        backImg.color = backColor;
        backImg.raycastTarget = false;

        var fillGO = new GameObject("StaminaFill", typeof(RectTransform), typeof(Image));
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.SetParent(back, false);
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.pivot = new Vector2(0f, 0.5f);
        fillRT.anchoredPosition = Vector2.zero;
        fill = fillGO.GetComponent<Image>();
        fill.color = fullColor;
        fill.raycastTarget = false;
    }

    void Update()
    {
        if (fill == null || stamina == null) return;
        float s = stamina.Stamina01;
        fill.rectTransform.sizeDelta = new Vector2(width * s, 0f);
        fill.color = stamina.IsExhausted ? exhaustedColor : Color.Lerp(lowColor, fullColor, s);
    }
}
