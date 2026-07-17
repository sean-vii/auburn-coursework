using System.Text;
using UnityEngine;

// TEMPORARY DIAGNOSTIC. Attach to ANY GameObject in the scene (e.g. an empty, or the
// GameManager). It prints the truth about the camera at runtime so we can see what is
// really driving it. Prints once at Start, then again ~1.5s into Play (after the
// ThirdPersonController has had a chance to move things). Delete this script when done.
public class CameraDebugProbe : MonoBehaviour
{
    private bool _secondReportDone;
    private float _t;

    void Start()
    {
        Report("=== CAMERA PROBE @ Start (frame 0) ===");
    }

    void LateUpdate()
    {
        if (_secondReportDone) return;
        _t += Time.deltaTime;
        if (_t >= 1.5f)
        {
            _secondReportDone = true;
            Report("=== CAMERA PROBE @ ~1.5s (after gameplay ran) ===");
        }
    }

    void Report(string header)
    {
        var sb = new StringBuilder();
        sb.AppendLine(header);

        // 1) Which camera does Unity consider the 'main' one?
        Camera main = Camera.main;
        if (main == null)
        {
            sb.AppendLine("Camera.main = NULL (no enabled camera tagged 'MainCamera'!)");
        }
        else
        {
            sb.AppendLine($"Camera.main = '{main.name}'  path='{PathOf(main.transform)}'  id={main.GetInstanceID()}");
            sb.AppendLine($"   enabled={main.enabled}  depth={main.depth}  targetTexture={(main.targetTexture ? main.targetTexture.name : "none")}");
            sb.AppendLine($"   LOCAL pos = {V(main.transform.localPosition)}   (this is the offset from its parent)");
            sb.AppendLine($"   WORLD pos = {V(main.transform.position)}");
            sb.AppendLine($"   parent    = {(main.transform.parent ? main.transform.parent.name : "<none / root>")}");
            sb.AppendLine($"   parent chain: {ChainOf(main.transform)}");

            bool hasBrain = false;
            foreach (var c in main.GetComponents<Component>())
            {
                if (c == null) continue;
                string n = c.GetType().Name;
                if (n.Contains("CinemachineBrain")) hasBrain = true;
            }
            sb.AppendLine($"   has CinemachineBrain? {hasBrain}");
        }

        // 2) Every camera in the scene, so we catch a second/rogue camera.
        var cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"-- All cameras in scene ({cams.Length}) --");
        foreach (var c in cams)
        {
            sb.AppendLine($"   '{c.name}' path='{PathOf(c.transform)}' enabled={c.enabled} activeInHierarchy={c.gameObject.activeInHierarchy} tag={c.tag} depth={c.depth} targetTex={(c.targetTexture ? c.targetTexture.name : "none")}");
        }

        // 3) Where is PlayerCameraRoot, and where is the player?
        var root = GameObject.Find("PlayerCameraRoot");
        if (root)
        {
            sb.AppendLine($"-- PlayerCameraRoot WORLD pos = {V(root.transform.position)}");
            sb.AppendLine($"   PlayerCameraRoot LOCAL pos (offset from PlayerArmature) = {V(root.transform.localPosition)}");
            sb.AppendLine($"   PlayerCameraRoot forward = {V(root.transform.forward)}");
        }
        else
        {
            sb.AppendLine("-- PlayerCameraRoot: NOT FOUND by name.");
        }

        // 4) Where is the PLAYER itself, and where does the camera look?
        var player = GameObject.Find("PlayerArmature");
        if (player && main)
        {
            Vector3 toCam = main.transform.position - player.transform.position;
            sb.AppendLine($"-- PlayerArmature WORLD pos = {V(player.transform.position)}");
            sb.AppendLine($"   camera - player  = {V(toCam)}   (how far the camera is from the player root; ~0 horizontally = first person)");
            sb.AppendLine($"   horizontal distance camera<->player = {new Vector2(toCam.x, toCam.z).magnitude:F2} m");
            sb.AppendLine($"   camera forward = {V(main.transform.forward)}");
            // Is the player IN FRONT of the camera (i.e. we'd see our own body = looks 3rd person)?
            Vector3 camToPlayer = (player.transform.position - main.transform.position);
            float inFront = Vector3.Dot(main.transform.forward, camToPlayer.normalized);
            sb.AppendLine($"   player-in-front-of-camera dot = {inFront:F2}  (>0.3 means the body is in view ahead of you)");
        }
        else if (main)
        {
            sb.AppendLine("-- PlayerArmature: NOT FOUND by name (tell me the exact name of your player root in the Hierarchy).");
        }

        Debug.Log(sb.ToString(), this);
    }

    static string V(Vector3 v) => $"({v.x:F2}, {v.y:F2}, {v.z:F2})";

    static string PathOf(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }

    static string ChainOf(Transform t)
    {
        var sb = new StringBuilder();
        while (t != null) { sb.Append(t.name); t = t.parent; if (t != null) sb.Append("  <-  "); }
        return sb.ToString();
    }
}
