using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

// Darkless — the backpack/inventory menu. Press Tab to open a scrollable LIST of everything in the
// pack; click an item to open a small action menu for it. Every item can be DROPPED; Food items can
// also be EATEN. (Only food exists right now; other categories get their own actions later.)
//
// The pack's weight readout lives at the TOP-LEFT of this menu (it replaces the old screen-corner
// BackpackBar HUD).
//
// Self-building UI (like BackpackBar / GameOverScreen): add this component to a scene object
// (GameManager) and it creates its own Canvas at runtime — nothing to hand-place in the Editor.
public class InventoryMenu : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("The backpack to show. Leave empty to auto-find the one in the scene.")]
    public Backpack backpack;

    [Header("Open / close")]
    [Tooltip("Key that toggles the inventory open/closed.")]
    public Key toggleKey = Key.Tab;
    [Tooltip("Freeze the game while the inventory is open. (Survival-horror could turn this off later " +
             "so browsing your pack leaves you exposed — for now, pausing keeps it simple.)")]
    public bool pauseWhileOpen = true;

    [Header("Colours")]
    public Color panelColor = new Color(0.06f, 0.06f, 0.07f, 0.96f);
    public Color dimColor = new Color(0f, 0f, 0f, 0.6f);
    public Color rowColor = new Color(0.14f, 0.14f, 0.16f, 1f);
    public Color rowHoverColor = new Color(0.24f, 0.22f, 0.16f, 1f);
    public Color barBackColor = new Color(0f, 0f, 0f, 0.5f);
    public Color barFillColor = new Color(0.8f, 0.7f, 0.35f, 0.95f);
    public Color barFullColor = new Color(0.9f, 0.25f, 0.2f, 0.95f);

    const float PanelW = 460f;
    const float PanelH = 620f;
    const float Pad = 16f;
    const float HeaderH = 78f;
    const float RowH = 44f;

    bool open;
    Canvas canvas;
    GameObject dim;
    RectTransform contentRoot;   // the scroll list's content (rows go here)
    TMP_Text weightLabel;
    Image weightFill;
    RectTransform weightBarBack;

    // The per-item action menu.
    GameObject actionPanel;
    TMP_Text actionTitle;
    Button eatButton;
    ItemDefinition selectedItem;

    Backpack pack => backpack;
    PlayerInput playerInput;
    PlayerDarkness playerDarkness;
    PlayerStamina playerStamina;

    void Start()
    {
        if (backpack == null) backpack = FindFirstObjectByType<Backpack>();
        playerDarkness = FindFirstObjectByType<PlayerDarkness>();

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerInput = player.GetComponent<PlayerInput>();
            playerStamina = player.GetComponent<PlayerStamina>();
        }

        BuildUI();
        if (backpack != null) backpack.Changed += OnPackChanged;
        SetOpen(false);
    }

    void OnDestroy()
    {
        if (backpack != null) backpack.Changed -= OnPackChanged;
    }

    void Update()
    {
        // Never open the inventory over the death screen.
        if (playerDarkness != null && playerDarkness.IsDead) return;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb[toggleKey].wasPressedThisFrame)
            SetOpen(!open);
    }

    void OnPackChanged()
    {
        RefreshWeight();
        BuildList();
        // If the selected item ran out, close its action menu.
        if (selectedItem != null && backpack != null && backpack.Count(selectedItem) <= 0)
            CloseAction();
    }

    // ------------------------------------------------------------------ open / close

    void SetOpen(bool value)
    {
        open = value;
        canvas.gameObject.SetActive(open);
        if (open)
        {
            RefreshWeight();
            BuildList();
            CloseAction();
        }

        if (pauseWhileOpen) Time.timeScale = open ? 0f : 1f;

        // Free the cursor to click while open; re-lock it for first-person play on close.
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;

        // Stop player movement/look while browsing (and restore it on close).
        if (playerInput != null) playerInput.enabled = !open;
    }

    // ------------------------------------------------------------------ item actions

    void OnRowClicked(ItemDefinition item)
    {
        selectedItem = item;
        actionTitle.text = item.displayName;
        // Only Food can be eaten (for now food is all we have; other categories add actions later).
        eatButton.gameObject.SetActive(item.category == ItemCategory.Food);
        actionPanel.SetActive(true);
    }

    void Eat()
    {
        if (selectedItem == null || backpack == null) return;
        if (playerStamina != null) playerStamina.Restore(selectedItem.staminaValue); // food -> stamina
        backpack.Remove(selectedItem, 1); // fires Changed -> list + weight refresh
    }

    void Drop()
    {
        if (selectedItem == null || backpack == null) return;
        // For now dropping just discards one unit (frees weight). Later: spawn a recoverable
        // world pickup at the player's feet.
        backpack.Remove(selectedItem, 1);
    }

    void CloseAction()
    {
        selectedItem = null;
        if (actionPanel != null) actionPanel.SetActive(false);
    }

    // ------------------------------------------------------------------ list + weight

    void BuildList()
    {
        if (contentRoot == null) return;

        // Clear old rows.
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);

        if (backpack == null) return;

        foreach (var pairKV in backpack.Items)
        {
            ItemDefinition item = pairKV.Key;
            int count = pairKV.Value;
            if (item == null || count <= 0) continue;
            MakeRow(item, count);
        }
    }

    void MakeRow(ItemDefinition item, int count)
    {
        var rowGO = new GameObject("Row_" + item.displayName,
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        rowGO.transform.SetParent(contentRoot, false);
        rowGO.GetComponent<LayoutElement>().preferredHeight = RowH;

        var img = rowGO.GetComponent<Image>();
        img.color = rowColor;

        var btn = rowGO.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = rowColor;
        colors.highlightedColor = rowHoverColor;
        colors.pressedColor = rowHoverColor;
        colors.selectedColor = rowColor;
        btn.colors = colors;
        ItemDefinition captured = item;
        btn.onClick.AddListener(() => OnRowClicked(captured));

        float weight = item.CountsTowardWeight ? item.weightPerUnit * count : 0f;
        string line = item.CountsTowardWeight
            ? $"{item.displayName}   x{count}      {weight:0.#} kg"
            : $"{item.displayName}   x{count}";
        var txt = MakeText("Label", rowGO.transform, line, 20f, TextAlignmentOptions.MidlineLeft);
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(14f, 0f); trt.offsetMax = new Vector2(-14f, 0f);
    }

    void RefreshWeight()
    {
        if (weightLabel == null || backpack == null) return;
        weightLabel.text = $"{backpack.CurrentWeight:0.#} / {backpack.weightCapacity:0.#} kg";
        float f = backpack.Fill01;
        float barW = weightBarBack.sizeDelta.x;
        weightFill.rectTransform.sizeDelta = new Vector2(barW * f, 0f);
        weightFill.color = f >= 0.999f ? barFullColor : barFillColor;
    }

    // ------------------------------------------------------------------ self-built UI

    void BuildUI()
    {
        var canvasGO = new GameObject("InventoryCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // above the HUD, below the game-over screen (999)
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // Dim backdrop (also eats clicks so nothing behind the menu is hit).
        dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(canvas.transform, false);
        Stretch(dim.GetComponent<RectTransform>());
        dim.GetComponent<Image>().color = dimColor;

        // Centre panel.
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        var panelRT = panel.GetComponent<RectTransform>();
        panelRT.SetParent(canvas.transform, false);
        panelRT.anchorMin = panelRT.anchorMax = panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(PanelW, PanelH);
        panelRT.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = panelColor;

        // --- Weight display, TOP-LEFT of the panel ---
        weightLabel = MakeText("Weight", panelRT, "0 / 0 kg", 22f, TextAlignmentOptions.MidlineLeft);
        var wlRT = weightLabel.rectTransform;
        wlRT.anchorMin = wlRT.anchorMax = wlRT.pivot = new Vector2(0f, 1f);
        wlRT.anchoredPosition = new Vector2(Pad, -Pad);
        wlRT.sizeDelta = new Vector2(PanelW - Pad * 2f, 28f);
        weightLabel.fontStyle = FontStyles.Bold;

        var barBackGO = new GameObject("WeightBarBack", typeof(RectTransform), typeof(Image));
        weightBarBack = barBackGO.GetComponent<RectTransform>();
        weightBarBack.SetParent(panelRT, false);
        weightBarBack.anchorMin = weightBarBack.anchorMax = weightBarBack.pivot = new Vector2(0f, 1f);
        weightBarBack.anchoredPosition = new Vector2(Pad, -(Pad + 34f));
        weightBarBack.sizeDelta = new Vector2(PanelW - Pad * 2f, 12f);
        barBackGO.GetComponent<Image>().color = barBackColor;

        var fillGO = new GameObject("WeightBarFill", typeof(RectTransform), typeof(Image));
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.SetParent(weightBarBack, false);
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.pivot = new Vector2(0f, 0.5f);
        fillRT.anchoredPosition = Vector2.zero;
        weightFill = fillGO.GetComponent<Image>();
        weightFill.color = barFillColor;

        BuildScrollList(panelRT);
        BuildActionMenu();
    }

    void BuildScrollList(RectTransform panelRT)
    {
        var svGO = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        var svRT = svGO.GetComponent<RectTransform>();
        svRT.SetParent(panelRT, false);
        svRT.anchorMin = new Vector2(0f, 0f);
        svRT.anchorMax = new Vector2(1f, 1f);
        svRT.offsetMin = new Vector2(Pad, Pad);        // left, bottom
        svRT.offsetMax = new Vector2(-Pad, -HeaderH);  // right, top (below the header)
        svGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);

        var scroll = svGO.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        var vpGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        var vpRT = vpGO.GetComponent<RectTransform>();
        vpRT.SetParent(svRT, false);
        Stretch(vpRT);
        vpRT.pivot = new Vector2(0f, 1f);
        scroll.viewport = vpRT;

        var contentGO = new GameObject("Content",
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentRoot = contentGO.GetComponent<RectTransform>();
        contentRoot.SetParent(vpRT, false);
        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.pivot = new Vector2(0.5f, 1f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = new Vector2(0f, 0f);

        var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.spacing = 6f;
        vlg.padding = new RectOffset(6, 6, 6, 6);

        var fitter = contentGO.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRoot;
    }

    void BuildActionMenu()
    {
        actionPanel = new GameObject("ActionMenu", typeof(RectTransform), typeof(Image));
        var apRT = actionPanel.GetComponent<RectTransform>();
        apRT.SetParent(canvas.transform, false);
        apRT.anchorMin = apRT.anchorMax = apRT.pivot = new Vector2(0.5f, 0.5f);
        apRT.sizeDelta = new Vector2(300f, 250f);
        apRT.anchoredPosition = Vector2.zero;
        actionPanel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 0.99f);

        actionTitle = MakeText("Title", apRT, "Item", 26f, TextAlignmentOptions.Center);
        var tRT = actionTitle.rectTransform;
        tRT.anchorMin = tRT.anchorMax = tRT.pivot = new Vector2(0.5f, 1f);
        tRT.anchoredPosition = new Vector2(0f, -18f);
        tRT.sizeDelta = new Vector2(280f, 34f);
        actionTitle.fontStyle = FontStyles.Bold;

        eatButton = MakeButton(apRT, "Eat", new Vector2(0f, 8f), Eat);
        MakeButton(apRT, "Drop", new Vector2(0f, -52f), Drop);
        MakeButton(apRT, "Cancel", new Vector2(0f, -112f), CloseAction);
    }

    // ------------------------------------------------------------------ tiny UI helpers

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static TMP_Text MakeText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>(); // auto-uses the project's default TMP font
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    Button MakeButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(240f, 48f);

        go.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.18f, 1f);

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.16f, 0.16f, 0.18f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.28f, 0.18f, 1f);
        colors.pressedColor = new Color(0.4f, 0.36f, 0.2f, 1f);
        colors.selectedColor = colors.normalColor;
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        var txt = MakeText("Text", rt, label, 22f, TextAlignmentOptions.Center);
        Stretch(txt.rectTransform);
        return btn;
    }
}
