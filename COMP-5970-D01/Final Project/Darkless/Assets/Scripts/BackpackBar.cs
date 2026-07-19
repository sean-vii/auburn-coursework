using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// A backpack readout, BUILT AT RUNTIME (like the flashlight battery bar) so there's nothing to
// hand-place in the Editor — just add this component to any object in the scene. It shows a weight
// fill bar (how full the pack is) plus a line listing what you're carrying, and refreshes itself
// whenever the pack changes. Turns red when the pack is full.
public class BackpackBar : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("The backpack to read. Leave empty to auto-find the one in the scene.")]
    public Backpack backpack;

    [Header("Colours")]
    public Color barBackColor = new Color(0f, 0f, 0f, 0.5f);
    public Color barFillColor = new Color(0.8f, 0.7f, 0.35f, 0.9f);
    public Color barFullColor = new Color(0.9f, 0.25f, 0.2f, 0.95f);
    public Color textColor = Color.white;

    [Header("Layout (screen pixels, from the top-left)")]
    public Vector2 margin = new Vector2(20f, 20f);
    public float barWidth = 220f;
    public float barHeight = 16f;
    public float fontSize = 16f;

    RectTransform container;
    Image fill;
    TextMeshProUGUI label;

    void Start()
    {
        if (backpack == null) backpack = FindFirstObjectByType<Backpack>();
        BuildUI();
        if (backpack != null) backpack.Changed += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (backpack != null) backpack.Changed -= Refresh;
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

        // Container anchored to the screen's top-left corner.
        var cGO = new GameObject("BackpackBar", typeof(RectTransform));
        container = cGO.GetComponent<RectTransform>();
        container.SetParent(canvas.transform, false);
        container.anchorMin = container.anchorMax = container.pivot = new Vector2(0f, 1f);
        container.anchoredPosition = new Vector2(margin.x, -margin.y);
        container.sizeDelta = new Vector2(barWidth, barHeight + 24f);

        // Bar background.
        var backGO = new GameObject("BarBack", typeof(RectTransform), typeof(Image));
        var backRT = backGO.GetComponent<RectTransform>();
        backRT.SetParent(container, false);
        backRT.anchorMin = backRT.anchorMax = backRT.pivot = new Vector2(0f, 1f);
        backRT.anchoredPosition = Vector2.zero;
        backRT.sizeDelta = new Vector2(barWidth, barHeight);
        var backImg = backGO.GetComponent<Image>();
        backImg.color = barBackColor;
        backImg.raycastTarget = false;

        // Bar fill — anchored to the left edge and stretched vertically, so we scale its WIDTH by
        // how full the pack is.
        var fillGO = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.SetParent(backRT, false);
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.pivot = new Vector2(0f, 0.5f);
        fillRT.anchoredPosition = Vector2.zero;
        fill = fillGO.GetComponent<Image>();
        fill.color = barFillColor;
        fill.raycastTarget = false;

        // Contents label under the bar.
        var lblGO = new GameObject("Contents", typeof(RectTransform));
        var lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.SetParent(container, false);
        lblRT.anchorMin = lblRT.anchorMax = lblRT.pivot = new Vector2(0f, 1f);
        lblRT.anchoredPosition = new Vector2(0f, -(barHeight + 2f));
        lblRT.sizeDelta = new Vector2(barWidth + 200f, 22f);
        label = lblGO.AddComponent<TextMeshProUGUI>();  // auto-uses the project's default TMP font
        label.fontSize = fontSize;
        label.color = textColor;
        label.raycastTarget = false;
    }

    void Refresh()
    {
        if (backpack == null || fill == null) return;

        float f = backpack.Fill01;
        fill.rectTransform.sizeDelta = new Vector2(barWidth * f, 0f);  // width = fullness
        fill.color = f >= 0.999f ? barFullColor : barFillColor;

        var sb = new StringBuilder();
        sb.Append(Mathf.RoundToInt(backpack.CurrentWeight)).Append('/')
          .Append(Mathf.RoundToInt(backpack.weightCapacity)).Append(" kg");
        foreach (var pair in backpack.Items)
        {
            if (pair.Value <= 0 || pair.Key == null) continue;
            sb.Append("   ").Append(pair.Key.displayName).Append(" x").Append(pair.Value);
        }
        if (label != null) label.text = sb.ToString();
    }
}
