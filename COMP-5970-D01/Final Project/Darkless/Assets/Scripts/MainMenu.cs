using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

// A title / start screen shown when the game loads: the game name, a one-line hook, a short how-to,
// and PLAY / QUIT buttons. It PAUSES the game (freezes time, frees the cursor, disables player input)
// until you press Play. Self-building under the shared Canvas (UIRoot), drawn on top. Put on GameManager.
public class MainMenu : MonoBehaviour
{
    [Header("Text")]
    public string titleText = "DARKLESS";
    public string taglineText = "Stranded after dark. Find the key that starts your car — and survive the night.";
    [TextArea(4, 10)]
    public string howToText =
        "GATHER by day, SURVIVE the night.\n" +
        "• Keep your CAMPFIRE fed (E) — light keeps the dark thing away.\n" +
        "• SEARCH the creatures at night for car keys — only one fits.\n" +
        "• Make TORCHES at your pack (5 sticks); FLASHLIGHT (F) is your panic button.\n" +
        "• Bring a key to the CAR and try it. Escape before the nights get deadly.";

    [Header("Behaviour")]
    public bool showAtStart = true;

    [Header("Menu camera")]
    [Tooltip("A dedicated camera shown behind the menu (a scenic view of the camp) so the menu isn't the " +
             "live first-person game view. Auto-found by name 'MenuCamera' if empty. Disabled on Play. It's " +
             "Untagged so the player's Main Camera stays Camera.main (interaction/AI keep working).")]
    public Camera menuCamera;

    [Header("Colours")]
    public Color dimColor = new Color(0f, 0f, 0f, 0.55f);
    public Color titleColor = new Color(0.9f, 0.25f, 0.2f, 1f);

    RectTransform root;
    PlayerInput playerInput;
    bool shown;
    Camera playerCam;                                   // the player's Main Camera — turned OFF while the menu
                                                        // is up so only the MenuCamera renders (no double-render)
    Transform uiRoot;                                   // the shared Canvas that holds all UI
    readonly System.Collections.Generic.List<GameObject> hidden = new System.Collections.Generic.List<GameObject>();

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerInput = p.GetComponent<PlayerInput>();
        if (menuCamera == null)
        {
            var mc = GameObject.Find("MenuCamera");
            if (mc != null) menuCamera = mc.GetComponent<Camera>();
        }
        playerCam = Camera.main;   // the player's camera (MenuCamera is Untagged, so this is the game cam)
        Build();
        if (showAtStart) Show();
        else { Hide(); if (menuCamera != null) menuCamera.gameObject.SetActive(false); }
    }

    void Show()
    {
        if (root == null) return;
        shown = true;
        root.gameObject.SetActive(true);
        root.SetAsLastSibling();               // draw over the HUD
        if (menuCamera != null) menuCamera.gameObject.SetActive(true);
        if (playerCam != null) playerCam.enabled = false;   // stop the game camera rendering (no double-render)
        ApplyPaused();
    }

    void Hide() { if (root != null) root.gameObject.SetActive(false); }

    // The paused menu state. Re-asserted every frame while shown (see Update) because other
    // self-building UIs set Time.timeScale in their own Start(), which would un-pause behind the menu.
    void ApplyPaused()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (playerInput != null) playerInput.enabled = false;
    }

    void Update()
    {
        if (shown) { ApplyPaused(); HideGameplayUI(); }
    }

    // Hide every other UI element under the shared Canvas while the menu is up (so the gameplay HUD —
    // objective, bars, subtitles, vignette — doesn't show over the menu). Done each frame so HUD that
    // builds itself AFTER the menu is caught too. Remembers what it hid so Play can restore exactly those.
    void HideGameplayUI()
    {
        if (uiRoot == null) return;
        for (int i = 0; i < uiRoot.childCount; i++)
        {
            GameObject child = uiRoot.GetChild(i).gameObject;
            if (root != null && child == root.gameObject) continue;   // never hide the menu itself
            if (child.activeSelf)
            {
                child.SetActive(false);
                if (!hidden.Contains(child)) hidden.Add(child);
            }
        }
    }

    void RestoreGameplayUI()
    {
        foreach (GameObject h in hidden) if (h != null) h.SetActive(true);
        hidden.Clear();
    }

    void Play()
    {
        Sfx.Click();
        shown = false;
        Hide();
        RestoreGameplayUI();
        if (menuCamera != null) menuCamera.gameObject.SetActive(false);
        if (playerCam != null) playerCam.enabled = true;    // hand rendering back to the game camera
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (playerInput != null) playerInput.enabled = true;
        SubtitleUI.Say("Where did I drop those keys...?", 5f);
    }

    void Quit()
    {
        Sfx.Click();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ------------------------------------------------------------------ self-built UI

    void Build()
    {
        var canvas = UIRoot.Get();
        if (canvas == null) return;

        var go = new GameObject("MainMenu", typeof(RectTransform));
        root = go.GetComponent<RectTransform>();
        root.SetParent(canvas.transform, false);
        uiRoot = canvas.transform;
        Stretch(root);

        // Full-screen dim backdrop that also eats clicks.
        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(root, false);
        Stretch(dim.GetComponent<RectTransform>());
        dim.GetComponent<Image>().color = dimColor;

        // Title.
        var title = MakeText("Title", root, titleText, 92f, TextAlignmentOptions.Center);
        var trt = title.rectTransform;
        trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = new Vector2(0f, 260f);
        trt.sizeDelta = new Vector2(1200f, 120f);
        title.color = titleColor;
        title.fontStyle = FontStyles.Bold;

        // Tagline.
        var tag = MakeText("Tagline", root, taglineText, 26f, TextAlignmentOptions.Center);
        var trt2 = tag.rectTransform;
        trt2.anchorMin = trt2.anchorMax = trt2.pivot = new Vector2(0.5f, 0.5f);
        trt2.anchoredPosition = new Vector2(0f, 170f);
        trt2.sizeDelta = new Vector2(1000f, 60f);
        tag.color = new Color(1f, 1f, 1f, 0.85f);

        // How-to block.
        var how = MakeText("HowTo", root, howToText, 22f, TextAlignmentOptions.TopLeft);
        var hrt = how.rectTransform;
        hrt.anchorMin = hrt.anchorMax = hrt.pivot = new Vector2(0.5f, 0.5f);
        hrt.anchoredPosition = new Vector2(0f, -20f);
        hrt.sizeDelta = new Vector2(760f, 260f);
        how.color = new Color(1f, 1f, 1f, 0.8f);

        // Buttons.
        MakeButton(root, "PLAY", new Vector2(0f, -230f), Play);
        MakeButton(root, "QUIT", new Vector2(0f, -300f), Quit);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    TMP_Text MakeText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.alignment = align; t.color = Color.white;
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
        rt.sizeDelta = new Vector2(260f, 54f);
        go.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.18f, 1f);

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.16f, 0.16f, 0.18f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.28f, 0.18f, 1f);
        colors.pressedColor = new Color(0.4f, 0.36f, 0.2f, 1f);
        colors.selectedColor = colors.normalColor;
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        var txt = MakeText("Text", rt, label, 26f, TextAlignmentOptions.Center);
        Stretch(txt.rectTransform);
    }
}
