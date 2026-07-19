using UnityEngine;

// Makes an empty root object copy the first-person camera's exact world position + rotation every
// frame. Park the first-person hand VIEWMODEL under this object instead of under the camera itself.
//
// Why not just parent the viewmodel to the camera? The camera lives INSIDE PlayerArmature
// (PlayerArmature > PlayerCameraRoot > MainCamera), and PlayerArmature has the body's Humanoid
// Animator. A second Humanoid skeleton anywhere under that Animator (the viewmodel is a copy of the
// character) has the same bone names, so the Animator's avatar binds to the WRONG skeleton and the
// real body freezes in a T-pose. Keeping the viewmodel under THIS object — which is a scene-root
// object, NOT a child of PlayerArmature — sidesteps that entirely while still looking camera-attached.
//
// Runs at a very high execution order so it happens AFTER the player controller has finished moving
// the camera this frame (otherwise the hand would lag a frame behind the view).
//
// Setup: create an empty GameObject at the scene ROOT, add this script, then parent the viewmodel
// under it (give the viewmodel the same local offset it used to have under the camera).
//
// [ExecuteAlways] so it also snaps to the camera in the EDITOR (not just Play) — that makes framing
// the hand WYSIWYG: park the viewmodel under this object and it appears in front of the camera in
// the Scene/Game view so you can position it without entering Play.
[ExecuteAlways]
[DefaultExecutionOrder(10000)]
public class ViewmodelFollowCamera : MonoBehaviour
{
    [Tooltip("The first-person camera to mirror. Leave empty to auto-use the MainCamera.")]
    public Transform cameraTransform;

    void LateUpdate()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
        if (cameraTransform == null) return;

        transform.SetPositionAndRotation(cameraTransform.position, cameraTransform.rotation);
    }
}
