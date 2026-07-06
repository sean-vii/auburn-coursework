using UnityEngine;
using TMPro;

// Keeps the running totals of what the player has collected (apples and ore) and shows
// them on the on-screen counter labels. There is one of these in the scene.
// Attach this to a GameManager empty and wire the two counter TMP texts.
public class ResourceCounter : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("The 'Apples: N' label.")]
    public TMP_Text appleText;
    [Tooltip("The 'Ore: N' label.")]
    public TMP_Text oreText;

    // The live totals during play.
    private int appleCount;
    private int oreCount;

    void Start()
    {
        // Show the starting values (0 / 0) right away.
        UpdateUI();
    }

    // Called by any resource when it's harvested. resourceName is "apple" or "ore";
    // amount is how much that harvest gives. Both resource types share this one method.
    public void AddResource(string resourceName, int amount)
    {
        // Compare case-insensitively so a stray capital in the Inspector can't misroute a count.
        if (resourceName.ToLower() == "apple")
            appleCount += amount;
        else
            oreCount += amount;

        UpdateUI();
    }

    // Pushes the current totals onto the screen.
    void UpdateUI()
    {
        if (appleText != null)
            appleText.text = "Apples: " + appleCount;
        if (oreText != null)
            oreText.text = "Ore: " + oreCount;
    }
}
