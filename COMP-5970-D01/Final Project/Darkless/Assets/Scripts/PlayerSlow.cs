using UnityEngine;
using StarterAssets;

// Slows the player while they're standing inside one or more "bush" trigger volumes (Bush.cs). Each
// bush calls AddSlow()/RemoveSlow() as the player enters/leaves it; while at least one bush is active,
// the movement controller's speeds are scaled by slowMultiplier. Ref-counted so overlapping bushes
// don't stack or get stuck.
//
// Put this on the PlayerArmature (next to the ThirdPersonController).
public class PlayerSlow : MonoBehaviour
{
    [Tooltip("Speed multiplier while pushing through a bush (0.5 = half speed). 1 = no slow.")]
    [Range(0.05f, 1f)] public float slowMultiplier = 0.5f;

    ThirdPersonController controller;
    float baseMove, baseSprint;
    int activeCount;

    void Awake()
    {
        controller = GetComponent<ThirdPersonController>();
        if (controller != null)
        {
            baseMove = controller.MoveSpeed;
            baseSprint = controller.SprintSpeed;
        }
    }

    public void AddSlow() { activeCount++; Apply(); }
    public void RemoveSlow() { activeCount = Mathf.Max(0, activeCount - 1); Apply(); }

    void Apply()
    {
        if (controller == null) return;
        float m = activeCount > 0 ? slowMultiplier : 1f;
        controller.MoveSpeed = baseMove * m;
        controller.SprintSpeed = baseSprint * m;
    }
}
