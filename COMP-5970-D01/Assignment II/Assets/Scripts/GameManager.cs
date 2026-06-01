using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Overlay")]
    [SerializeField] private Color overlayColor = new Color(1f, 1f, 1f, 0.1f);

    [Header("Title")]
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color titleOutlineColor = Color.black;
    [SerializeField] private Vector2 titleOutlineDistance = new Vector2(2f, -2f);
    [SerializeField] private int titleFontSize = 64;
    [Tooltip("Vertical offset of the title text from screen center (positive = up).")]
    [SerializeField] private float titleYOffset = 60f;

    [Header("Button")]
    [SerializeField] private Color buttonColor = Color.black;
    [SerializeField] private Color buttonHoverColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color buttonTextColor = Color.white;
    [SerializeField] private int buttonFontSize = 28;
    [Tooltip("X = horizontal padding (each side), Y = vertical padding (top & bottom).")]
    [SerializeField] private Vector2 buttonPadding = new Vector2(20f, 10f);
    [Tooltip("Vertical offset of the button from screen center (negative = down).")]
    [SerializeField] private float buttonYOffset = -60f;

    [Header("Font")]
    [Tooltip("Drag a Font asset (e.g. PressStart2P-Regular.ttf) here. Leave empty for the default sans-serif.")]
    [SerializeField] private Font customFont;

    private GameObject modalRoot;
    private Text titleText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsureEventSystem();
        BuildModal();
        modalRoot.SetActive(false);
    }

    public void ShowGameOver() => Show("Game Over");
    public void ShowWin() => Show("You Win!");

    [ContextMenu("Preview: Game Over")]
    private void PreviewGameOver() => RebuildAndShow("Game Over");

    [ContextMenu("Preview: You Win")]
    private void PreviewYouWin() => RebuildAndShow("You Win!");

    [ContextMenu("Hide Modal")]
    private void HideModal()
    {
        if (modalRoot != null) modalRoot.SetActive(false);
        Time.timeScale = 1f;
    }

    private void RebuildAndShow(string title)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode to preview the modal.");
            return;
        }
        if (modalRoot != null) Destroy(modalRoot);
        BuildModal();
        modalRoot.SetActive(false);
        Show(title);
    }

    private void Show(string title)
    {
        if (modalRoot.activeSelf) return;
        titleText.text = title;
        modalRoot.SetActive(true);
        Time.timeScale = 0f;
    }

    private void PlayAgain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem", typeof(EventSystem));
        es.AddComponent<InputSystemUIInputModule>();
    }

    private void BuildModal()
    {
        Font fontToUse = customFont != null ? customFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // --- Canvas ---
        GameObject canvasGO = new GameObject("GameModalCanvas");
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // --- Full-screen dim overlay (also blocks clicks behind) ---
        GameObject overlay = CreateUIImage("Overlay", canvasGO.transform, overlayColor);
        Stretch(overlay.GetComponent<RectTransform>());

        // --- Title text (centered, outlined) ---
        GameObject title = new GameObject("Title", typeof(Text), typeof(Outline));
        title.transform.SetParent(overlay.transform, false);
        titleText = title.GetComponent<Text>();
        titleText.text = "Game Over";
        titleText.fontSize = titleFontSize;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = titleColor;
        titleText.font = fontToUse;
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        titleText.verticalOverflow = VerticalWrapMode.Overflow;

        Outline titleOutline = title.GetComponent<Outline>();
        titleOutline.effectColor = titleOutlineColor;
        titleOutline.effectDistance = titleOutlineDistance;

        RectTransform titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = titleRT.anchorMax = new Vector2(0.5f, 0.5f);
        titleRT.pivot = new Vector2(0.5f, 0.5f);
        titleRT.sizeDelta = new Vector2(1000, 100);
        titleRT.anchoredPosition = new Vector2(0f, titleYOffset);

        // --- Play Again button (auto-sized to its text + padding) ---
        GameObject btn = CreateUIImage("PlayAgainButton", overlay.transform, buttonColor);
        Button button = btn.AddComponent<Button>();
        ColorBlock cb = button.colors;
        cb.normalColor = buttonColor;
        cb.highlightedColor = buttonHoverColor;
        cb.pressedColor = buttonColor * 0.8f;
        cb.selectedColor = buttonHoverColor;
        button.colors = cb;
        button.onClick.AddListener(PlayAgain);

        RectTransform btnRT = btn.GetComponent<RectTransform>();
        btnRT.anchorMin = btnRT.anchorMax = new Vector2(0.5f, 0.5f);
        btnRT.pivot = new Vector2(0.5f, 0.5f);
        btnRT.anchoredPosition = new Vector2(0f, buttonYOffset);

        HorizontalLayoutGroup hlg = btn.AddComponent<HorizontalLayoutGroup>();
        int padX = Mathf.RoundToInt(buttonPadding.x);
        int padY = Mathf.RoundToInt(buttonPadding.y);
        hlg.padding = new RectOffset(padX, padX, padY, padY);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        ContentSizeFitter csf = btn.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // --- Button label ---
        GameObject label = new GameObject("Label", typeof(Text));
        label.transform.SetParent(btn.transform, false);
        Text labelText = label.GetComponent<Text>();
        labelText.text = "Play Again";
        labelText.fontSize = buttonFontSize;
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = buttonTextColor;
        labelText.font = fontToUse;
        labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
        labelText.verticalOverflow = VerticalWrapMode.Overflow;

        modalRoot = canvasGO;
    }

    private static GameObject CreateUIImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
