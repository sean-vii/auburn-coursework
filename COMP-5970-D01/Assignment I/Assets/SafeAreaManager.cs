using UnityEngine;

// Scene setup (one-time, in Unity Editor):
// 1. Create empty GameObject "SafeAreaManager" at origin, add this component.
// 2. As children, create "LeftWall" and "RightWall" GameObjects with a
//    SpriteRenderer (any solid sprite, tinted red, scaled tall and thin
//    e.g. scale = (0.2, 20, 1)). Initial position doesn't matter.
// 3. Drag LeftWall / RightWall into the Inspector slots below.
public class SafeAreaManager : MonoBehaviour
{
    public Transform leftWall;
    public Transform rightWall;

    public float startHalfWidth = 8f;
    public float endHalfWidth = 0.6f;
    public float shrinkDuration = 60f;
    public float shrinkStartDelay = 3f;

    Transform player;
    bool gameOverTriggered;
    float currentHalfWidth;

    public float LeftX => -currentHalfWidth;
    public float RightX => currentHalfWidth;

    void Start()
    {
        currentHalfWidth = startHalfWidth;
        var playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null) player = playerMovement.transform;
    }

    void Update()
    {
        float elapsed = Time.timeSinceLevelLoad - shrinkStartDelay;
        float t = Mathf.Clamp01(elapsed / shrinkDuration);
        currentHalfWidth = Mathf.Lerp(startHalfWidth, endHalfWidth, t);

        if (leftWall != null)
        {
            Vector3 p = leftWall.position;
            p.x = LeftX;
            leftWall.position = p;
        }
        if (rightWall != null)
        {
            Vector3 p = rightWall.position;
            p.x = RightX;
            rightWall.position = p;
        }

        if (gameOverTriggered || player == null) return;

        const float epsilon = 0.01f;
        if (player.position.x <= LeftX + epsilon || player.position.x >= RightX - epsilon)
        {
            gameOverTriggered = true;
            FindFirstObjectByType<GameManager>().GameOver();
        }
    }
}
