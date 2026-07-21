using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Darkless — on-screen SUBTITLES: the character's inner voice / nudges, shown as a caption at the
// bottom of the screen, right UNDER the interaction prompt. It copies the interaction prompt's look
// (centered white ClashGrotesk) but sits on a black caption bar and JITTERS (a tight, fast shake) so
// it reads as a tense, uneasy thought — small enough to stay legible.
//
// Self-building UI like the rest (add this to GameManager; it builds itself under the one shared
// Canvas via UIRoot). Any system triggers a line through the static helpers:
//     SubtitleUI.Say("Where did I drop my keys?");     // show until replaced/cleared
//     SubtitleUI.Say("...", 4f);                        // show for 4 seconds, then auto-hide
//     SubtitleUI.Clear();                               // hide now
public class SubtitleUI : MonoBehaviour
{
    public static SubtitleUI Instance { get; private set; }

    [Header("Placement (just below the interaction prompt)")]
    [Tooltip("Y of the subtitle's TOP edge, in reference px up from the screen bottom. The interaction " +
             "prompt's bottom is ~200, so ~196 sits the subtitle right under it.")]
    public float topEdgeY = 196f;
    [Tooltip("Max width of the caption before the text wraps to another line.")]
    public float maxWidth = 900f;

    [Header("Look (copied from the interaction prompt)")]
    public float fontSize = 28f;
    public Color textColor = Color.white;
    [Tooltip("The black caption bar behind the text.")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.72f);
    [Tooltip("Gap between the text and the edge of the black bar (x = sides, y = top/bottom).")]
    public Vector2 padding = new Vector2(26f, 12f);

    [Header("Shake (violent but tight, so it stays readable)")]
    [Tooltip("Max pixel offset of the jitter each frame. Keep small (a few px) so the text stays legible.")]
    public float shakeAmplitude = 4f;

    [Header("Startup line")]
    [Tooltip("Shown once at the start of the game. Empty = none.")]
    [TextArea] public string startupMessage = "Where did I drop my keys?";
    [Tooltip("Seconds the startup line stays up (0 = until something replaces it).")]
    public float startupSeconds = 5f;
    [Tooltip("Delay before the startup line appears.")]
    public float startupDelay = 0.75f;

    RectTransform root;    // the caption bar we shake (carries the black background Image)
    Image bg;
    TMP_Text label;
    Vector2 basePos;       // the shake pivots around here
    float hideTimer = -1f; // >0 counts down to auto-hide; <=0 = persistent / off
    bool currentShake = true; // whether the currently-shown line jitters (some lines are steady)

    void Awake()
    {
        Instance = this;
        BuildUI();
        Hide();
    }

    void Start()
    {
        if (!string.IsNullOrEmpty(startupMessage))
            Invoke(nameof(ShowStartup), startupDelay);
    }

    void ShowStartup() => Show(startupMessage, startupSeconds);

    // ------------------------------------------------------------------ public API

    // Static so callers don't need a reference: SubtitleUI.Say("..."). No-ops if there's no subtitle
    // system in the scene.
    public static void Say(string message, float autoHideSeconds = 0f, bool shake = true)
    {
        if (Instance != null) Instance.Show(message, autoHideSeconds, shake);
    }

    public static void Clear()
    {
        if (Instance != null) Instance.Hide();
    }

    public void Show(string message, float autoHideSeconds = 0f, bool shake = true)
    {
        if (label == null) return;
        label.text = message;
        currentShake = shake;

        // Size the black bar to hug the (possibly wrapped) text plus padding.
        Vector2 pref = label.GetPreferredValues(message, maxWidth, 0f);
        float w = Mathf.Min(pref.x, maxWidth);
        root.sizeDelta = new Vector2(w + padding.x * 2f, pref.y + padding.y * 2f);

        root.gameObject.SetActive(true);
        root.SetAsLastSibling();     // draw above the HUD / prompt
        hideTimer = autoHideSeconds > 0f ? autoHideSeconds : -1f;
    }

    public void Hide()
    {
        hideTimer = -1f;
        if (root != null) root.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ update (auto-hide + shake)

    void Update()
    {
        if (root == null || !root.gameObject.activeSelf) return;

        if (hideTimer > 0f)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f) { Hide(); return; }
        }

        // Tight, fast jitter around the base position — violent but small, so it stays readable.
        // Steady (no jitter) for calm lines like the daytime "turning back" nudge.
        if (currentShake)
            root.anchoredPosition = basePos + (Vector2)(Random.insideUnitCircle * shakeAmplitude);
        else
            root.anchoredPosition = basePos;
    }

    // ------------------------------------------------------------------ self-built UI

    void BuildUI()
    {
        var go = new GameObject("Subtitle", typeof(RectTransform), typeof(Image));
        root = go.GetComponent<RectTransform>();
        root.SetParent(UIRoot.Get().transform, false);
        // Bottom-centre anchor like the prompt; TOP pivot so the bar grows DOWNWARD from topEdgeY and
        // its top always sits just under the prompt regardless of how many lines the text wraps to.
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0f);
        root.pivot = new Vector2(0.5f, 1f);
        basePos = new Vector2(0f, topEdgeY);
        root.anchoredPosition = basePos;
        root.sizeDelta = new Vector2(maxWidth, 56f);

        bg = go.GetComponent<Image>();
        bg.color = backgroundColor;
        bg.raycastTarget = false;

        var textGO = new GameObject("Text", typeof(RectTransform));
        var trt = textGO.GetComponent<RectTransform>();
        trt.SetParent(root, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(padding.x, padding.y);
        trt.offsetMax = new Vector2(-padding.x, -padding.y);
        label = textGO.AddComponent<TextMeshProUGUI>();   // uses the ClashGrotesk default font
        label.text = "";
        label.fontSize = fontSize;
        label.color = textColor;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.raycastTarget = false;
    }
}
