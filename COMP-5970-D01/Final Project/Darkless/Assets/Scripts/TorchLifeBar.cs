using UnityEngine;
using UnityEngine.UI;

// A segmented life meter for the equipped TORCH — the same look as the flashlight's battery bar
// (FlashlightBatteryBar), drawn in the same bottom-left spot. Because you hold only ONE light at a
// time, the two bars are never shown together, so they can share the corner.
//
// It BUILDS ITSELF at runtime under the shared Canvas. The torch has 3 tiers of 13s, so this defaults
// to 3 segments — one per tier. The highest still-lit segment (the current tier) blinks; lower tiers
// are solid; spent tiers are greyed. The bar is hidden unless a torch is actually lit.
public class TorchLifeBar : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("The torch to read. Leave empty to auto-find the one in the scene.")]
    public Torch torch;

    [Header("Segments (one per tier)")]
    [Range(1, 12)] public int segmentCount = 3;
    [Tooltip("Life % (of the TOTAL) left in the active segment, below which it flashes red.")]
    [Range(0f, 0.3f)] public float redThreshold = 0.06f;

    [Header("Colours")]
    public Color fullColor = new Color(1f, 0.72f, 0.35f, 1f);          // lit tiers (warm torch tone)
    public Color depletedColor = new Color(0.25f, 0.25f, 0.25f, 0.5f); // burned-out tiers (greyed)
    public Color lowColor = new Color(1f, 0.15f, 0.15f, 1f);           // active tier when nearly gone

    [Header("Blink")]
    public float blinkPeriod = 0.5f;
    [Range(0f, 1f)] public float blinkDimAlpha = 0.15f;

    [Header("Layout (screen pixels, from the bottom-left)")]
    [Tooltip("These are OVERWRITTEN at Start to match the flashlight battery bar exactly (size, spacing, " +
             "position, colours) so the two HUD meters are identical — only the segment COUNT differs.")]
    public float segmentWidth = 30f;
    public float segmentHeight = 9f;
    public float spacing = 4f;
    [Tooltip("Offset from the bottom-left corner: X = right, Y = up. Matches the flashlight bar.")]
    public Vector2 margin = new Vector2(60f, 60f);
    [Tooltip("Copy the flashlight battery bar's exact size/position/colours at Start (recommended, so " +
             "the torch meter looks identical to the flashlight one). Off = use the values above as-is.")]
    public bool matchFlashlightBar = true;

    RectTransform container;
    Image[] segments;

    void Start()
    {
        if (torch == null) torch = FindFirstObjectByType<Torch>();

        // Make the torch meter identical to the flashlight battery bar (they never show together, so
        // they share the same corner and look the same — only the number of segments differs).
        if (matchFlashlightBar)
        {
            var fb = FindFirstObjectByType<FlashlightBatteryBar>();
            if (fb != null)
            {
                segmentWidth = fb.segmentWidth;
                segmentHeight = fb.segmentHeight;
                spacing = fb.spacing;
                margin = fb.margin;
                fullColor = fb.fullColor;
                depletedColor = fb.depletedColor;
                lowColor = fb.lowColor;
                blinkPeriod = fb.blinkPeriod;
                blinkDimAlpha = fb.blinkDimAlpha;
                redThreshold = fb.redThreshold;
            }
        }

        BuildUI();
    }

    void BuildUI()
    {
        // Parent under the one shared scene Canvas (same pattern as the other self-building HUD bars).
        Transform canvasT = UIRoot.Get() != null ? UIRoot.Get().transform : null;
        if (canvasT == null)
        {
            var canvas = FindFirstObjectByType<Canvas>();
            canvasT = canvas != null ? canvas.transform : null;
        }
        if (canvasT == null) return;

        var containerGO = new GameObject("TorchLifeBar", typeof(RectTransform));
        container = containerGO.GetComponent<RectTransform>();
        container.SetParent(canvasT, false);
        container.anchorMin = container.anchorMax = container.pivot = new Vector2(0f, 0f);
        container.anchoredPosition = margin;
        container.sizeDelta = new Vector2(
            segmentCount * segmentWidth + (segmentCount - 1) * spacing,
            segmentHeight);

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
            img.raycastTarget = false;
            segments[i] = img;
        }
    }

    void Update()
    {
        if (segments == null) return;

        bool lit = torch != null && torch.IsLit;
        if (container.gameObject.activeSelf != lit)
            container.gameObject.SetActive(lit);
        if (!lit) return;

        float life = torch.Life01;                                   // 0..1
        int active = Mathf.Clamp(Mathf.FloorToInt(life * segmentCount), 0, segmentCount - 1);

        float activeSegmentFloor = (float)active / segmentCount;
        bool nearDepletion = (life - activeSegmentFloor) < redThreshold;
        bool blinkOn = Mathf.Repeat(Time.unscaledTime, blinkPeriod) < blinkPeriod * 0.5f;

        for (int i = 0; i < segmentCount; i++)
        {
            Color c;
            if (i < active) c = fullColor;             // lower, still-lit tiers
            else if (i > active) c = depletedColor;    // burned-out tiers
            else
            {
                c = nearDepletion ? lowColor : fullColor;
                if (!blinkOn) c.a *= blinkDimAlpha;
            }
            segments[i].color = c;
        }
    }
}
