using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

// Darkless — the VICTORY presentation. The mirror of GameOverScreen: instead of the dark or the
// Mimic taking you, you found the key that fits and drove away. Same self-building, top-most Canvas
// pattern as GameOverScreen / BackpackBar — nothing to hand-place in the Editor, just add this
// component to a scene object (e.g. GameManager) and (optionally) assign a victory sound clip.
//
// Car.cs calls Show() when the player tries the correct key at the car. We FREEZE the game
// (Time.timeScale = 0) so the moment lands, hold a beat, fade UP to a warm screen, then show
// "YOU ESCAPED" + a Play Again / Quit button. All timing uses UNSCALED time because the game is
// frozen.
public class WinScreen : MonoBehaviour
{
    [Header("Timing (real seconds — the game is frozen)")]
    [Tooltip("Beat before the fade begins.")]
    public float holdSeconds = 0.6f;
    [Tooltip("How long the fade to the victory screen takes.")]
    public float fadeSeconds = 1.4f;

    [Header("Audio")]
    [Tooltip("Played once at the moment of escape (e.g. an engine start / a hopeful sting).")]
    public AudioClip winSound;
    [Range(0f, 1f)] public float winVolume = 1f;

    [Header("Text")]
    public string titleMessage = "YOU ESCAPED";
    public string subtitle = "You found the key that fit. You drove into the dawn.";

    bool shown;
    CanvasGroup group;
    TMP_Text titleText;
    TMP_Text subtitleText;
    GameObject buttonsRow;
    AudioSource audioSource;

    // So other systems can ask "did we already win?" (e.g. to stop the monster killing you the same
    // frame you escape).
    public bool HasWon => shown;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;          // 2D — you always hear your own escape
        audioSource.ignoreListenerPause = true;

        BuildUI();
        group.alpha = 0f;                        // invisible until we win, so the HUD shows normally
        group.gameObject.SetActive(false);
    }

    // Called by Car.cs when the correct key is tried. Freezes the game, plays the sting, then runs
    // the hold -> fade -> buttons sequence.
    public void Show()
    {
        if (shown) return;
        shown = true;

        if (winSound != null) audioSource.PlayOneShot(winSound, winVolume);

        Time.timeScale = 0f; // freeze the world on the moment of escape
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
        buttonsRow.SetActive(false);
        titleText.gameObject.SetActive(true);
        subtitleText.gameObject.SetActive(true);
        group.alpha = 0f;
        yield return new WaitForSecondsRealtime(holdSeconds);

        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(t / fadeSeconds);
            yield return null;
        }
        group.alpha = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        buttonsRow.SetActive(true);
    }

    public void PlayAgain()
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
        var canvasGO = new GameObject("WinCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // above the gameplay HUD
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var rootGO = new GameObject("WinPanel", typeof(RectTransform), typeof(CanvasGroup));
        var root = rootGO.GetComponent<RectTransform>();
        root.SetParent(canvas.transform, false);
        Stretch(root);
        group = rootGO.GetComponent<CanvasGroup>();

        // Full-screen background — a deep warm near-black (relief, not the cold death-black).
        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.SetParent(root, false);
        Stretch(bgRT);
        bg.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.05f, 1f);

        titleText = MakeText("Title", root, titleMessage, 84f, new Vector2(0f, 120f), new Vector2(1400f, 140f));
        titleText.color = new Color(0.55f, 0.85f, 0.5f, 1f); // warm green — safe at last
        titleText.fontStyle = FontStyles.Bold;

        subtitleText = MakeText("Subtitle", root, subtitle, 32f, new Vector2(0f, 20f), new Vector2(1300f, 80f));
        subtitleText.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        buttonsRow = new GameObject("Buttons", typeof(RectTransform));
        var rowRT = buttonsRow.GetComponent<RectTransform>();
        rowRT.SetParent(root, false);
        rowRT.anchorMin = rowRT.anchorMax = rowRT.pivot = new Vector2(0.5f, 0.5f);
        rowRT.anchoredPosition = new Vector2(0f, -120f);
        rowRT.sizeDelta = new Vector2(520f, 60f);

        MakeButton(rowRT, "Play Again", new Vector2(-130f, 0f), PlayAgain);
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
        var t = go.AddComponent<TextMeshProUGUI>();
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
        img.color = new Color(0.12f, 0.16f, 0.12f, 1f);

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.12f, 0.16f, 0.12f, 1f);
        colors.highlightedColor = new Color(0.20f, 0.38f, 0.20f, 1f);
        colors.pressedColor = new Color(0.28f, 0.52f, 0.28f, 1f);
        colors.selectedColor = colors.normalColor;
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        var txt = MakeText("Text", rt, label, 26f, Vector2.zero, new Vector2(220f, 56f));
        txt.color = Color.white;
    }
}
