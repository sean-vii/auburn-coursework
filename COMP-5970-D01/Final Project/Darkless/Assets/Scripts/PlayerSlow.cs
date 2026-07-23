using UnityEngine;
using StarterAssets;

// The single authority over the player's movement speed. Two jobs:
//   * BUSHES (Bush.cs) call AddSlow()/RemoveSlow() as the player enters/leaves; while at least one is
//     active the controller's speeds are scaled by slowMultiplier. Ref-counted so overlapping bushes
//     don't stack or get stuck.
//   * BEAR TRAPS (BearTrap.cs) call Snare(seconds): the player is ROOTED (move + jump disabled) for a
//     few seconds, then released. Root overrides the bush slow while it lasts.
//
// Put this on the PlayerArmature (next to the ThirdPersonController).
public class PlayerSlow : MonoBehaviour
{
    [Tooltip("Speed multiplier while pushing through a bush (0.5 = half speed). 1 = no slow.")]
    [Range(0.05f, 1f)] public float slowMultiplier = 0.5f;

    ThirdPersonController controller;
    float baseMove, baseSprint, baseJump;
    int activeCount;

    bool rooted;
    float rootTimer;

    // True while a bear trap has the player rooted in place.
    public bool IsRooted => rooted;

    void Awake()
    {
        controller = GetComponent<ThirdPersonController>();
        if (controller != null)
        {
            baseMove = controller.MoveSpeed;
            baseSprint = controller.SprintSpeed;
            baseJump = controller.JumpHeight;
        }
    }

    void Update()
    {
        if (!rooted) return;
        rootTimer -= Time.deltaTime;
        if (rootTimer <= 0f) { rooted = false; Apply(); }
    }

    public void AddSlow() { activeCount++; Apply(); }
    public void RemoveSlow() { activeCount = Mathf.Max(0, activeCount - 1); Apply(); }

    // Root the player in place for 'seconds' (stepped on a bear trap). Extends an active snare rather
    // than shortening it.
    public void Snare(float seconds)
    {
        rooted = true;
        rootTimer = Mathf.Max(rootTimer, seconds);
        Apply();
    }

    void Apply()
    {
        if (controller == null) return;
        // Rooted wins over everything: no move, no jump. Otherwise the bush slow (if any) applies.
        float m = rooted ? 0f : (activeCount > 0 ? slowMultiplier : 1f);
        controller.MoveSpeed = baseMove * m;
        controller.SprintSpeed = baseSprint * m;
        controller.JumpHeight = rooted ? 0f : baseJump;
    }
}
