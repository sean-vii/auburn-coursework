using UnityEngine;
using UnityEngine.UI;

// A little progress bar that fills while you HOLD E to search a spot. Built at runtime and shown
// only during a hold — just add this component to any object in the scene. Reads
// PlayerInteraction.HoldProgress01 (0..1 while searching, or -1 when idle).
public class InteractionProgressUI : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("The player's PlayerInteraction. Leave empty to auto-find it.")]
    public PlayerInteraction player;

    [Header("Look")]
    public Color backColor = new Color(0f, 0f, 0f, 0.6f);
    public Color fillColor = new Color(0.85f, 0.85f, 0.9f, 0.95f);
    public Vector2 size = new Vector2(160f, 12f);
    [Tooltip("Vertical offset from screen center (negative = below center, toward the prompt).")]
    public float yOffset = -70f;

    RectTransform container;
    Image fill;

    void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerInteraction>();
        BuildUI();
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

        // Container (the bar background) centered on screen.
        var cGO = new GameObject("InteractionProgress", typeof(RectTransform), typeof(Image));
        container = cGO.GetComponent<RectTransform>();
        container.SetParent(canvas.transform, false);
        container.anchorMin = container.anchorMax = container.pivot = new Vector2(0.5f, 0.5f);
        container.anchoredPosition = new Vector2(0f, yOffset);
        container.sizeDelta = size;
        var back = cGO.GetComponent<Image>();
        back.color = backColor;
        back.raycastTarget = false;

        // Fill — left-anchored, stretched vertically, width scaled by progress.
        var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.SetParent(container, false);
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.pivot = new Vector2(0f, 0.5f);
        fillRT.anchoredPosition = Vector2.zero;
        fill = fillGO.GetComponent<Image>();
        fill.color = fillColor;
        fill.raycastTarget = false;

        container.gameObject.SetActive(false);
    }

    void Update()
    {
        if (player == null || container == null) return;

        float p = player.HoldProgress01;
        bool show = p >= 0f;
        if (container.gameObject.activeSelf != show)
            container.gameObject.SetActive(show);
        if (!show) return;

        fill.rectTransform.sizeDelta = new Vector2(size.x * Mathf.Clamp01(p), 0f);
    }
}
