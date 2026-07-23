using UnityEngine;
using TMPro;

// A small persistent HUD in the top-left: the current OBJECTIVE and which NIGHT it is (and whether it's
// day or night). The night number comes from DayNightCycle.NightNumber, so it tracks the difficulty
// ramp. Self-building under the shared Canvas (UIRoot). Put on GameManager.
public class ObjectiveHUD : MonoBehaviour
{
    [Tooltip("The always-on goal reminder.")]
    [TextArea] public string objectiveText = "Objective: find a car key at night, then start the car to escape.";
    [Tooltip("Offset from the TOP-LEFT corner (X = right, Y = down).")]
    public Vector2 margin = new Vector2(24f, 20f);
    public float objectiveSize = 20f;
    public float nightSize = 26f;
    public Color objectiveColor = new Color(1f, 1f, 1f, 0.85f);
    public Color dayColor = new Color(1f, 0.9f, 0.55f, 1f);
    public Color nightColor = new Color(0.6f, 0.75f, 1f, 1f);

    DayNightCycle dayNight;
    TMP_Text nightLabel;
    TMP_Text objLabel;

    void Start()
    {
        dayNight = FindFirstObjectByType<DayNightCycle>();
        Build();
    }

    void Build()
    {
        var root = UIRoot.Get();
        if (root == null) return;

        // NIGHT / DAY line (bigger).
        nightLabel = MakeText("NightLabel", root.transform, "NIGHT 1", nightSize);
        var nrt = nightLabel.rectTransform;
        nrt.anchorMin = nrt.anchorMax = nrt.pivot = new Vector2(0f, 1f);
        nrt.anchoredPosition = new Vector2(margin.x, -margin.y);
        nrt.sizeDelta = new Vector2(520f, 32f);
        nightLabel.fontStyle = FontStyles.Bold;

        // Objective line (smaller, under it).
        objLabel = MakeText("ObjectiveLabel", root.transform, objectiveText, objectiveSize);
        var ort = objLabel.rectTransform;
        ort.anchorMin = ort.anchorMax = ort.pivot = new Vector2(0f, 1f);
        ort.anchoredPosition = new Vector2(margin.x, -margin.y - 34f);
        ort.sizeDelta = new Vector2(560f, 60f);
        objLabel.color = objectiveColor;
    }

    void Update()
    {
        if (nightLabel == null) return;
        int n = dayNight != null ? Mathf.Max(1, dayNight.NightNumber) : 1;
        bool night = dayNight != null && dayNight.IsNight;
        nightLabel.text = (night ? "NIGHT " : "DAY ") + n;
        nightLabel.color = night ? nightColor : dayColor;
    }

    TMP_Text MakeText(string name, Transform parent, string text, float size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();   // uses the project's default TMP font
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }
}
