using UnityEngine;

// Keeps the mini-map camera directly above the player, looking straight down,
// and rotates it with the player so the map turns as the player turns.
// Attach this to the MiniMap Camera.
public class MiniMapFollow : MonoBehaviour
{
    [Tooltip("The player to follow (drag the PlayerArmature here).")]
    public Transform target;

    [Tooltip("How far above the player the camera floats.")]
    public float height = 40f;

    // LateUpdate runs after the player has moved this frame, so the camera never lags behind.
    void LateUpdate()
    {
        if (target == null)
            return;

        // Stay over the player on X/Z, raised by 'height' on Y.
        Vector3 newPosition = target.position;
        newPosition.y = target.position.y + height;
        transform.position = newPosition;

        // Look straight down (X = 90) and match the player's Y turn so the map rotates too.
        transform.rotation = Quaternion.Euler(90f, target.eulerAngles.y, 0f);
    }
}
