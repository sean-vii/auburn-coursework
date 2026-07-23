using UnityEngine;
using UnityEngine.UI;

// A vignette that closes in as the player's light runs out. Reads PlayerDarkness.Light01 (1 = safe,
// 0 = dead): as it falls, a dark radial gradient creeps in from the screen edges, so you FEEL the dark
// taking you before the death screen. Self-building full-screen overlay (procedurally-generated radial
// sprite — no art needed), under the shared Canvas. Put on GameManager.
public class DarknessVignette : MonoBehaviour
{
    public Color color = new Color(0f, 0f, 0f, 1f);
    [Tooltip("Vignette starts creeping in once Light01 drops BELOW this (1 = always faintly present, " +
             "0.7 = only once you're getting into danger).")]
    [Range(0f, 1f)] public float startFadingAt = 0.75f;
    [Tooltip("Darkest the vignette gets right before death (1 = almost fully black edges).")]
    [Range(0f, 1f)] public float maxAlpha = 0.94f;

    PlayerDarkness darkness;
    Image img;

    void Start()
    {
        darkness = FindFirstObjectByType<PlayerDarkness>();
        Build();
    }

    void Build()
    {
        var canvas = UIRoot.Get();
        if (canvas == null) return;

        var go = new GameObject("DarknessVignette", typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        rt.SetAsFirstSibling();   // sits over the game but UNDER the HUD bars and modal menus

        img = go.GetComponent<Image>();
        img.sprite = RadialSprite();
        img.type = Image.Type.Simple;
        img.raycastTarget = false;
        SetAlpha(0f);
    }

    // A soft radial gradient: clear in the middle, opaque toward the edges. Stretched to the screen it
    // reads as a vignette that darkens the periphery.
    Sprite RadialSprite()
    {
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float maxD = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / maxD;   // 0 centre → ~1 edge
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.4f) / 0.6f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    void Update()
    {
        if (darkness == null || img == null) return;
        float light = darkness.Light01;                            // 1 safe → 0 dead
        float t = Mathf.InverseLerp(startFadingAt, 0f, light);     // 0 while safe → 1 at death
        SetAlpha(t * maxAlpha);
    }

    void SetAlpha(float a)
    {
        var col = color; col.a = a;
        if (img != null) img.color = col;
    }
}
