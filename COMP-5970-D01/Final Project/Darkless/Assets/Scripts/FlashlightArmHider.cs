using UnityEngine;

// Quality-of-life: hides the player's right arm while the flashlight is out, so it reads as
// "the flashlight IS what you're holding" instead of an arm clipping around it.
//
// The body is a single skinned mesh, so you can't disable "just an arm" renderer. The standard
// trick is to shrink the arm's BONE to (almost) zero scale: every vertex weighted to that bone
// collapses to a point, hiding the limb. We do it in LateUpdate, AFTER the Animator has posed the
// skeleton, so our scale isn't overwritten.
//
// ⚠️ Do NOT parent the flashlight (or anything you want to stay visible) under a bone this hides —
// it would collapse too. Keep the flashlight on its own transform near the hand.
//
// Put this on the PlayerArmature (the object with the Humanoid Animator).
public class FlashlightArmHider : MonoBehaviour
{
    [Tooltip("The flashlight to watch. Leave empty to auto-find the scene's flashlight.")]
    public Flashlight flashlight;

    [Tooltip("The torch to watch too. The arm hides while EITHER the flashlight is on OR the torch is " +
             "lit (both are held in the right hand). Leave empty to auto-find the scene's torch.")]
    public Torch torch;

    [Tooltip("The player's Humanoid Animator. Leave empty to auto-find on this object or a parent.")]
    public Animator animator;

    [Tooltip("Bones to hide when the flashlight is on. Leave empty to auto-use the RIGHT UPPER ARM " +
             "(collapsing it hides the whole right arm — forearm, hand and fingers included).")]
    public Transform[] bonesToHide;

    [Tooltip("Scale the hidden bones shrink to. Exactly 0 can upset some rigs, so a tiny value is safest.")]
    public float hiddenScale = 0.0001f;

    Vector3[] originalScales;

    void Start()
    {
        if (flashlight == null) flashlight = FindFirstObjectByType<Flashlight>();
        if (torch == null) torch = FindFirstObjectByType<Torch>();
        if (animator == null) animator = GetComponentInParent<Animator>();

        // Default target: the right upper arm bone, resolved from the Humanoid rig.
        if ((bonesToHide == null || bonesToHide.Length == 0) && animator != null && animator.isHuman)
        {
            Transform arm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if (arm != null) bonesToHide = new Transform[] { arm };
        }

        // Remember each bone's normal scale so we can put it back.
        if (bonesToHide != null)
        {
            originalScales = new Vector3[bonesToHide.Length];
            for (int i = 0; i < bonesToHide.Length; i++)
                if (bonesToHide[i] != null) originalScales[i] = bonesToHide[i].localScale;
        }

        if (bonesToHide == null || bonesToHide.Length == 0)
            Debug.LogWarning("FlashlightArmHider: no bones to hide. Assign the arm bone(s) manually, " +
                "or make sure this is on the object with the Humanoid Animator.", this);
    }

    void LateUpdate()
    {
        if (bonesToHide == null || bonesToHide.Length == 0 || originalScales == null) return;

        // Hide while EITHER hand light is out: the flashlight is on (and not dead) OR the torch is lit.
        bool hide = (flashlight != null && flashlight.isOn && flashlight.Battery01 > 0f)
                 || (torch != null && torch.IsLit);

        for (int i = 0; i < bonesToHide.Length; i++)
        {
            if (bonesToHide[i] == null) continue;
            bonesToHide[i].localScale = hide ? Vector3.one * hiddenScale : originalScales[i];
        }
    }
}
