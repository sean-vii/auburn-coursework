using UnityEngine;
using TMPro;

// Draws a compass bar: eight direction labels (N, NE, E, ... NW) that slide left/right
// across the panel based on which way the player is facing. Labels that drift too far
// from center are hidden, so only the nearby directions show.
// Attach this to the Compass Panel.
public class CompassMarkerBar : MonoBehaviour
{
    [Header("References")]
    [Tooltip("What the compass reads its heading from. Wire the MAIN CAMERA so the compass " +
             "matches where you're LOOKING, not just the direction the body is facing.")]
    public Transform headingSource;
    [Tooltip("The UI object the direction labels are placed inside.")]
    public RectTransform markerContainer;
    [Tooltip("The template TMP label that gets copied once per direction.")]
    public TMP_Text markerPrefab;

    [Header("Layout")]
    [Tooltip("Width of the compass panel in pixels; labels past half this are hidden.")]
    public float panelWidth = 500f;
    [Tooltip("How many pixels each label moves per degree the player turns.")]
    public float pixelsPerDegree = 4f;

    // The eight compass directions, 45 degrees apart.
    private string[] labels = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
    private TMP_Text[] markers;

    void Start()
    {
        markers = new TMP_Text[labels.Length];

        // Make one label per direction by copying the prefab into the container.
        for (int i = 0; i < labels.Length; i++)
        {
            TMP_Text marker = Instantiate(markerPrefab, markerContainer);
            marker.text = labels[i];
            marker.alignment = TextAlignmentOptions.Center;
            markers[i] = marker;
        }

        // Hide the original template — it only existed to be copied.
        markerPrefab.gameObject.SetActive(false);
    }

    void Update()
    {
        // Guard: nothing to do if we're not wired up or the markers aren't built yet.
        if (headingSource == null || markers == null)
            return;

        // Which way we're looking around the horizontal (Y) axis.
        float heading = headingSource.eulerAngles.y;

        for (int i = 0; i < markers.Length; i++)
        {
            // Each label sits at its own compass angle (N=0, NE=45, E=90, ...).
            float markerAngle = i * 45f;

            // Shortest signed angle between where we face and this label's direction.
            float diff = Mathf.DeltaAngle(heading, markerAngle);

            // Convert that angle into a horizontal offset across the bar.
            float x = diff * pixelsPerDegree;
            markers[i].rectTransform.anchoredPosition = new Vector2(x, 0f);

            // Only show labels near the center of the panel.
            bool visible = Mathf.Abs(x) < panelWidth / 2f;
            markers[i].gameObject.SetActive(visible);
        }
    }
}
