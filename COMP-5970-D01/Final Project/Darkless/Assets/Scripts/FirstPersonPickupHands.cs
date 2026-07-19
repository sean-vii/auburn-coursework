using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// First-person pickup hands. While the player is doing an interaction (harvest / search), this shows
// a VIEWMODEL arm — a copy of the character, parented under the camera and framed so just the right
// arm is on screen — and plays the SAME pick animation on it, right in your face, so the action
// reads in first person. At the same time it:
//   * collapses the REAL right arm bone (so you don't see two arms), and
//   * hides whatever you're holding (empty hands).
//
// It mirrors the flashlight's "show a held model" idea, except this model is animated. It reuses the
// existing PickFruit / GatherOre clips: the viewmodel just needs the same Humanoid Avatar + an
// Animator Controller that has those trigger states (the player's own controller works).
//
// Put this on the PlayerArmature (the object with the Humanoid Animator) or on GameManager.
[DefaultExecutionOrder(100)] // after FlashlightArmHider / the Animator so our bone-hide wins
public class FirstPersonPickupHands : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("PlayerInteraction (auto-found). Tells us when a pickup starts/ends and which animation.")]
    public PlayerInteraction player;

    [Tooltip("The viewmodel arm: a COPY of the character parented under the camera, framed so the " +
             "right arm shows. Give it a Humanoid Animator (same Avatar + a controller with the " +
             "PickFruit/GatherOre triggers). Start it DISABLED — shown only during a pickup.")]
    public GameObject viewmodel;

    [Tooltip("The viewmodel's Animator. Auto-uses the one on 'viewmodel' if left empty.")]
    public Animator viewmodelAnimator;

    [Header("Empty hands")]
    [Tooltip("Objects to hide during a pickup so your hands read as empty — e.g. the flashlight's " +
             "VISIBLE MODEL (assign the mesh child, not the script object, so its light logic keeps " +
             "running).")]
    public List<GameObject> heldItemsToHide = new List<GameObject>();

    [Header("Hide the real right arm")]
    [Tooltip("Real body bone(s) to collapse during a pickup so the real arm doesn't clip through the " +
             "viewmodel. Auto-uses the Humanoid RIGHT UPPER ARM if left empty.")]
    public Transform[] realArmBones;
    [Tooltip("Scale the hidden bone shrinks to (tiny, not exactly 0, which upsets some rigs).")]
    public float hiddenScale = 0.0001f;

    [Header("Debug (framing helper — safe to leave on)")]
    [Tooltip("Press this key in Play mode to play the pickup animation on the viewmodel WITHOUT " +
             "needing a tree — so you can frame the on-screen hand. The viewmodel stays visible " +
             "between presses. Set to None to disable.")]
    public Key debugPlayKey = Key.P;
    [Tooltip("Which animation the debug key plays (must match a trigger in the viewmodel's Animator, " +
             "e.g. PickFruit or GatherOre).")]
    public string debugTrigger = "PickFruit";

    [Header("Mirror to the LEFT when the right hand is full")]
    [Tooltip("The ViewmodelRoot (the camera-following parent of the viewmodel). When the player's " +
             "RIGHT hand is occupied, this is flipped on X so the pickup plays MIRRORED on the LEFT " +
             "side of the screen. Auto-uses the viewmodel's parent if left empty. Works for any " +
             "current or future first-person hand animation under it.")]
    public Transform mirrorRoot;
    [Tooltip("Force 'right hand occupied' from another script (e.g. a future equip system). If left " +
             "false, it auto-detects: true if the flashlight is on, or if any 'heldItemsToHide' " +
             "object is currently active.")]
    public bool rightHandOccupied = false;

    Flashlight flashlight;
    Animator realAnimator;
    Vector3[] armOriginalScales;
    bool busyState;

    void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerInteraction>();

        if (viewmodel != null)
        {
            if (viewmodelAnimator == null) viewmodelAnimator = viewmodel.GetComponent<Animator>();
            viewmodel.SetActive(false);   // hidden until a pickup starts
        }

        // Mirror pivot = the camera-following ViewmodelRoot (the viewmodel's parent). Flipping its X
        // reflects the whole rig across screen-center. Auto-find the flashlight for the "hand full" check.
        if (mirrorRoot == null && viewmodel != null) mirrorRoot = viewmodel.transform.parent;
        flashlight = FindFirstObjectByType<Flashlight>();

        realAnimator = player != null ? player.GetComponent<Animator>() : null;

        // Default real-arm bone = the Humanoid right upper arm (collapsing it hides the whole arm).
        if ((realArmBones == null || realArmBones.Length == 0) && realAnimator != null && realAnimator.isHuman)
        {
            Transform arm = realAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if (arm != null) realArmBones = new Transform[] { arm };
        }
        if (realArmBones != null)
        {
            armOriginalScales = new Vector3[realArmBones.Length];
            for (int i = 0; i < realArmBones.Length; i++)
                if (realArmBones[i] != null) armOriginalScales[i] = realArmBones[i].localScale;
        }

        // Play the pick animation on the viewmodel the instant an interaction starts.
        if (player != null) player.InteractionStarted += OnInteractionStarted;
    }

    void OnDestroy()
    {
        if (player != null) player.InteractionStarted -= OnInteractionStarted;
    }

    // Framing helper: on the debug key, show the viewmodel and replay the pick animation in place.
    // Because this doesn't set PlayerInteraction.IsBusy, LateUpdate leaves the viewmodel on screen
    // (it only hides on a busy->idle transition), so you can freely tune the Transform while it plays.
    void Update()
    {
        if (debugPlayKey == Key.None || Keyboard.current == null) return;
        if (Keyboard.current[debugPlayKey].wasPressedThisFrame)
            OnInteractionStarted(debugTrigger);
    }

    // The right hand is "occupied" if something is being held in it: another system flagged it, the
    // flashlight is on, or a held model is visible. When occupied, the pickup mirrors to the left.
    bool IsRightHandOccupied()
    {
        if (rightHandOccupied) return true;
        if (flashlight != null && flashlight.isOn) return true;
        foreach (var go in heldItemsToHide)
            if (go != null && go.activeInHierarchy) return true;
        return false;
    }

    // Show the viewmodel and fire the same trigger the real body is playing.
    void OnInteractionStarted(string trigger)
    {
        // Decide the side ONCE, as the pickup starts. If the right hand is full, flip the viewmodel
        // root's X so the whole animation plays mirrored on the LEFT side of the screen (using the
        // free left hand). Flipping the root mirrors ANY hand animation, now or in the future.
        if (mirrorRoot != null)
        {
            Vector3 s = mirrorRoot.localScale;
            float mag = Mathf.Abs(s.x) < 0.0001f ? 1f : Mathf.Abs(s.x);
            mirrorRoot.localScale = new Vector3(IsRightHandOccupied() ? -mag : mag, s.y, s.z);
        }

        if (viewmodel != null) viewmodel.SetActive(true);
        if (viewmodelAnimator != null && !string.IsNullOrEmpty(trigger))
            viewmodelAnimator.SetTrigger(trigger);
    }

    void LateUpdate()
    {
        bool busy = player != null && player.IsBusy;

        // Handle the start/end transition once.
        if (busy != busyState)
        {
            // On end: hide the viewmodel (it's shown by OnInteractionStarted) and give held items back.
            if (!busy && viewmodel != null) viewmodel.SetActive(false);
            foreach (var go in heldItemsToHide)
                if (go != null) go.SetActive(!busy);

            // Restore the real arm ONCE when the pickup ends. While idle we then leave the bone alone
            // so we don't fight the flashlight's arm-hider (which owns it when the flashlight is out).
            if (!busy && realArmBones != null && armOriginalScales != null)
                for (int i = 0; i < realArmBones.Length; i++)
                    if (realArmBones[i] != null) realArmBones[i].localScale = armOriginalScales[i];

            busyState = busy;
        }

        // While busy, keep the real right arm collapsed every frame (after the Animator poses it) so
        // it doesn't clip through the viewmodel arm.
        if (busy && realArmBones != null)
            for (int i = 0; i < realArmBones.Length; i++)
                if (realArmBones[i] != null) realArmBones[i].localScale = Vector3.one * hiddenScale;
    }
}
