using UnityEngine;
using UnityEngine.UI;

// A segmented battery meter for the flashlight, drawn bottom-left. It BUILDS ITSELF at runtime
// (7 little UI Images under a Canvas) so there's nothing to hand-place in the Editor — just add
// this component to any GameObject in the scene.
//
// Rules (all tunable below):
//   * The whole meter is HIDDEN unless the flashlight is on.
//   * 7 skinny segments in a horizontal strip, left(1)->right(7); each = 1/7 of the battery.
//   * The highest still-powered segment is the "active" one and BLINKS.
//   * Segments to the RIGHT of the active one are greyed out (used up).
//   * Segments to the LEFT of the active one are solid white (still full).
//   * The active segment is white, but FLASHES RED when it's within ~3% of total battery of
//     greying out (its last sliver of charge).
public class FlashlightBatteryBar : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("The flashlight to read. Leave empty to auto-find the one in the scene.")]
    public Flashlight flashlight;

    [Header("Segments")]
    [Tooltip("How many segments the battery is split into.")]
    [Range(2, 20)] public int segmentCount = 7;
    [Tooltip("Battery % (of the TOTAL) left in the active segment, below which it flashes red. " +
             "0.03 = flashes red in its last 3% of the whole battery.")]
    [Range(0f, 0.14f)] public float redThreshold = 0.03f;

    [Header("Colours")]
    public Color fullColor = Color.white;               // still-charged segments (and active, normally)
    public Color depletedColor = new Color(0.25f, 0.25f, 0.25f, 0.5f); // used-up (greyed) segments
    public Color lowColor = new Color(1f, 0.15f, 0.15f, 1f);           // active segment when nearly gone

    [Header("Blink")]
    [Tooltip("Seconds for one full blink (on + off) of the active segment.")]
    public float blinkPeriod = 0.5f;
    [Tooltip("How dim the active segment goes on the 'off' half of a blink (0 = invisible).")]
    [Range(0f, 1f)] public float blinkDimAlpha = 0.15f;

    [Header("Layout (screen pixels, from the bottom-left)")]
    [Tooltip("Length of each segment along the bar (bar runs left->right, segment 1 on the left).")]
    public float segmentWidth = 30f;
    [Tooltip("Thickness of the bar. Small = skinny horizontal strip.")]
    public float segmentHeight = 9f;
    public float spacing = 4f;
    [Tooltip("Offset from the bottom-left corner: X = right, Y = up.")]
    public Vector2 margin = new Vector2(60f, 60f);

    RectTransform container;
    Image[] segments;

    void Start()
    {
        if (flashlight == null)
            flashlight = FindFirstObjectByType<Flashlight>();

        BuildUI();
    }

    // Creates the container + segment Images under a Canvas (found or created).
    void BuildUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasGO = new GameObject("Canvas (auto)", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        // Container, anchored to the screen's bottom-left corner.
        var containerGO = new GameObject("FlashlightBatteryBar", typeof(RectTransform));
        container = containerGO.GetComponent<RectTransform>();
        container.SetParent(canvas.transform, false);
        container.anchorMin = container.anchorMax = container.pivot = new Vector2(0f, 0f);
        container.anchoredPosition = margin;
        container.sizeDelta = new Vector2(
            segmentCount * segmentWidth + (segmentCount - 1) * spacing,
            segmentHeight);

        // One Image per segment, laid out left(0) -> right in a skinny horizontal strip.
        segments = new Image[segmentCount];
        for (int i = 0; i < segmentCount; i++)
        {
            var segGO = new GameObject("Segment_" + (i + 1), typeof(RectTransform), typeof(Image));
            var rt = segGO.GetComponent<RectTransform>();
            rt.SetParent(container, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(segmentWidth, segmentHeight);
            rt.anchoredPosition = new Vector2(i * (segmentWidth + spacing), 0f);

            var img = segGO.GetComponent<Image>();
            img.raycastTarget = false; // display only — never eats clicks
            segments[i] = img;
        }
    }

    void Update()
    {
        if (segments == null) return;

        // Only show while the flashlight is actually on (and has charge).
        bool lit = flashlight != null && flashlight.isOn && flashlight.Battery01 > 0f;
        if (container.gameObject.activeSelf != lit)
            container.gameObject.SetActive(lit);
        if (!lit) return;

        float battery = flashlight.Battery01;                        // 0..1
        int active = Mathf.Clamp(Mathf.FloorToInt(battery * segmentCount), 0, segmentCount - 1);

        // How much charge is left inside the active segment, as a fraction of the WHOLE battery.
        float activeSegmentFloor = (float)active / segmentCount;
        bool nearDepletion = (battery - activeSegmentFloor) < redThreshold;

        // Blink phase: true for the first half of each period, false for the second.
        bool blinkOn = Mathf.Repeat(Time.unscaledTime, blinkPeriod) < blinkPeriod * 0.5f;

        for (int i = 0; i < segmentCount; i++)
        {
            Color c;
            if (i < active)
            {
                c = fullColor;                    // below the active segment: still full
            }
            else if (i > active)
            {
                c = depletedColor;                // above the active segment: used up / greyed
            }
            else
            {
                // The active segment: white normally, red when nearly gone — and it blinks.
                c = nearDepletion ? lowColor : fullColor;
                if (!blinkOn) c.a *= blinkDimAlpha;
            }
            segments[i].color = c;
        }
    }
}
