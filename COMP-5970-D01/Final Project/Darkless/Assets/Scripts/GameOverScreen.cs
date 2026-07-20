using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

// Darkless — the game-over presentation. ONE screen serves BOTH death causes (the dark, the monster)
// through PlayerDarkness.Die(cause).
//
// The key trick (Tier 2): on death we FREEZE the game (Time.timeScale = 0) and DON'T reload. The
// last live frame — usually the pitch-black Mimic lunging right on top of you — becomes a held
// tableau. We hold on it for a beat (so the scare lands), then fade to black and show the cause of
// death + a Restart button.
//
// Self-building UI (like BackpackBar / FlashlightBatteryBar): it creates its own top-most Canvas at
// runtime, so there's nothing to hand-place in the Editor — just add this component to a scene
// object (e.g. GameManager) and assign the death sound clip.
//
// All timing uses UNSCALED time and WaitForSecondsRealtime because the game is frozen (timeScale 0).
public class GameOverScreen : MonoBehaviour
{
    [Header("Timing (real seconds — the game is frozen)")]
    [Tooltip("Beat spent on the frozen tableau before the fade begins (lets the scare land).")]
    public float holdSeconds = 0.8f;
    [Tooltip("How long the fade to black takes.")]
    public float fadeSeconds = 1.2f;

    [Header("Audio")]
    [Tooltip("Played once at the moment of death (e.g. the bone-break).")]
    public AudioClip deathSound;
    [Range(0f, 1f)] public float deathVolume = 1f;

    [Header("Text")]
    public string titleMessage = "YOU DIED";
    [Tooltip("Shown if a death has no specific cause message.")]
    public string defaultCause = "You died.";

    bool shown;
    CanvasGroup group;      // fades the whole screen in
    TMP_Text titleText;
    TMP_Text causeText;
    GameObject buttonsRow;
    AudioSource audioSource;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;          // 2D — you always hear your own death
        audioSource.ignoreListenerPause = true; // still audible even if audio ever gets paused

        BuildUI();
        group.alpha = 0f;                       // invisible until death, so the HUD shows normally
        group.gameObject.SetActive(false);
    }

    // Called by PlayerDarkness.Die(cause). Freezes the game, plays the death sting, then runs the
    // hold -> fade -> buttons sequence.
    public void Show(string cause)
    {
        if (shown) return;
        shown = true;

        if (deathSound != null) audioSource.PlayOneShot(deathSound, deathVolume);

        Time.timeScale = 0f; // freeze — the last frame becomes the tableau
        causeText.text = string.IsNullOrEmpty(cause) ? defaultCause : cause;

        // First-person play locks & hides the cursor and eats mouse-look. Free the cursor so the
        // buttons are clickable, and kill player input so the camera can't be spun on the death
        // screen (movement is already frozen by timeScale, but mouse-look isn't).
        ReleaseCursorAndInput();

        group.gameObject.SetActive(true);
        StartCoroutine(Sequence());
    }

    void ReleaseCursorAndInput()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var pi = player.GetComponent<PlayerInput>();
            if (pi != null) pi.enabled = false; // stops StarterAssets move + look
        }
    }

    IEnumerator Sequence()
    {
        // 1) Hold on the frozen scene — everything hidden (alpha 0), the tableau shows through.
        buttonsRow.SetActive(false);
        titleText.gameObject.SetActive(true);
        causeText.gameObject.SetActive(true);
        group.alpha = 0f;
        yield return new WaitForSecondsRealtime(holdSeconds);

        // 2) Fade the black screen + text in (UNSCALED — the game is frozen).
        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(t / fadeSeconds);
            yield return null;
        }
        group.alpha = 1f;

        // 3) Offer the buttons. Re-assert the free cursor in case anything re-locked it during the hold.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        buttonsRow.SetActive(true);
    }

    public void Restart()
    {
        Time.timeScale = 1f; // ALWAYS unfreeze before leaving, or the reloaded scene stays frozen
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Quit()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ------------------------------------------------------------------ self-built UI

    void BuildUI()
    {
        // A dedicated top-most overlay Canvas so we cover the whole HUD (mini-map, bars, prompts).
        var canvasGO = new GameObject("GameOverCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // above the gameplay HUD Canvas
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // Root panel (full-screen) with a CanvasGroup so we can fade EVERYTHING at once.
        var rootGO = new GameObject("GameOverPanel", typeof(RectTransform), typeof(CanvasGroup));
        var root = rootGO.GetComponent<RectTransform>();
        root.SetParent(canvas.transform, false);
        Stretch(root);
        group = rootGO.GetComponent<CanvasGroup>();

        // Full-screen black background (its alpha is driven by the CanvasGroup).
        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.SetParent(root, false);
        Stretch(bgRT);
        bg.GetComponent<Image>().color = Color.black;

        // Title ("YOU DIED"), large, upper-centre.
        titleText = MakeText("Title", root, titleMessage, 84f, new Vector2(0f, 120f), new Vector2(1200f, 140f));
        titleText.color = new Color(0.75f, 0.05f, 0.05f, 1f); // blood red
        titleText.fontStyle = FontStyles.Bold;

        // Cause of death, smaller, just under the title.
        causeText = MakeText("Cause", root, defaultCause, 34f, new Vector2(0f, 20f), new Vector2(1200f, 60f));
        causeText.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        // Buttons row (hidden until the fade finishes).
        buttonsRow = new GameObject("Buttons", typeof(RectTransform));
        var rowRT = buttonsRow.GetComponent<RectTransform>();
        rowRT.SetParent(root, false);
        rowRT.anchorMin = rowRT.anchorMax = rowRT.pivot = new Vector2(0.5f, 0.5f);
        rowRT.anchoredPosition = new Vector2(0f, -120f);
        rowRT.sizeDelta = new Vector2(520f, 60f);

        MakeButton(rowRT, "Restart", new Vector2(-130f, 0f), Restart);
        MakeButton(rowRT, "Quit", new Vector2(130f, 0f), Quit);

        buttonsRow.SetActive(false);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static TMP_Text MakeText(string name, Transform parent, string text, float size, Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        var t = go.AddComponent<TextMeshProUGUI>(); // auto-uses the project's default TMP font
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    void MakeButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(220f, 56f);

        var img = go.GetComponent<Image>();
        img.color = new Color(0.14f, 0.14f, 0.14f, 1f);

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.14f, 0.14f, 0.14f, 1f);
        colors.highlightedColor = new Color(0.35f, 0.06f, 0.06f, 1f);
        colors.pressedColor = new Color(0.55f, 0.10f, 0.10f, 1f);
        colors.selectedColor = colors.normalColor;
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        var txt = MakeText("Text", rt, label, 26f, Vector2.zero, new Vector2(220f, 56f));
        txt.color = Color.white;
    }
}
