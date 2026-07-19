using UnityEngine;

// Put this on the first-person pickup VIEWMODEL (the duplicate character parented under the camera).
// It makes the viewmodel render essentially JUST the right arm/hand — no torso, no other limbs.
//
// The body is a single skinned mesh, so we can't just disable "the torso" or "the other limbs".
// Trick, in two steps every LateUpdate:
//   1. Collapse the HIPS bone to a tiny scale. EVERY bone is a descendant of the hips, so the whole
//      body (torso, head, legs, both arms) shrinks to an invisible speck at the pelvis.
//   2. Counter-scale the RIGHT ARM root by 1 / hiddenScale, which brings ONLY the right arm back to
//      full size. Because we scaled just ONE bone (the hips), restoring the arm is a single division
//      instead of undoing several stacked scales — so it stays numerically safe (no float blow-up)
//      and the arm keeps its full animated pose. The only side effect is that the arm now anchors at
//      the pelvis point instead of the shoulder, so it sits a bit lower; you correct that by framing
//      the viewmodel's Transform (position it under the camera so the hand lands where you want).
//
// Runs in LateUpdate so the Animator (which doesn't animate scale on Humanoid clips) can't undo it.
[DefaultExecutionOrder(1100)] // after the viewmodel's Animator poses the skeleton
public class ViewmodelArmOnly : MonoBehaviour
{
    [Tooltip("The viewmodel's Humanoid Animator. Auto-found on this object if empty.")]
    public Animator animator;

    [Tooltip("Scale the whole body shrinks to (tiny, not exactly 0, which upsets some rigs). " +
             "The right arm is brought back by 1/this, so don't set it to 0.")]
    public float hiddenScale = 0.0001f;

    [Tooltip("Also hide the right SHOULDER/deltoid (show only upper-arm -> hand). " +
             "Off = the whole right arm including the shoulder.")]
    public bool hideRightShoulder = false;

    Transform hips;       // collapsed -> hides the entire body
    Transform armRoot;    // counter-scaled -> the only part that comes back

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null || !animator.isHuman)
        {
            Debug.LogWarning("ViewmodelArmOnly: needs a Humanoid Animator on the viewmodel.", this);
            enabled = false;
            return;
        }

        hips = animator.GetBoneTransform(HumanBodyBones.Hips);

        // Bring back the whole right arm (from the shoulder) by default, or just the upper-arm->hand
        // if we're also hiding the shoulder. Fall back to the upper arm if the rig has no shoulder bone.
        armRoot = hideRightShoulder ? null : animator.GetBoneTransform(HumanBodyBones.RightShoulder);
        if (armRoot == null) armRoot = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);

        if (hips == null || armRoot == null)
        {
            Debug.LogWarning("ViewmodelArmOnly: could not find the Hips or right-arm bone on this rig.", this);
            enabled = false;
            return;
        }

        // With most of the skeleton collapsed to a point, the mesh's computed bounds can shrink and
        // Unity may frustum-cull the arm even while it's on screen. Force always-render to be safe.
        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            smr.updateWhenOffscreen = true;
    }

    void LateUpdate()
    {
        if (hips == null || armRoot == null) return;

        hips.localScale = Vector3.one * hiddenScale;            // hide the whole body
        armRoot.localScale = Vector3.one * (1f / hiddenScale);  // bring the right arm back to full size
    }
}
