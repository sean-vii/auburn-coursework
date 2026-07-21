using UnityEngine;
using UnityEngine.UI;

// The ONE shared scene Canvas that every runtime-built UI parents under, so all UI (HUD bars,
// inventory/map, game-over, win) lives under a single Canvas object in the hierarchy — one place to
// find and reason about it, one CanvasScaler driving everything.
//
// Returns the existing scene "Canvas". If somehow none exists (a bare test scene) it builds a minimal
// overlay Canvas so the UI still shows. Cached so repeated calls are cheap; the cache self-heals after
// a scene reload (a destroyed Canvas compares == null, so we re-find the new one).
public static class UIRoot
{
    static Canvas cached;

    public static Canvas Get()
    {
        if (cached != null) return cached;

        // Prefer the existing ROOT canvas already in the scene (the hand-placed "Canvas").
        var all = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
        foreach (var c in all)
            if (c != null && c.isRootCanvas) { cached = c; return cached; }

        // Fallback: build a bare overlay canvas so UI still appears in a stripped test scene.
        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        cached = canvas;
        return cached;
    }
}
