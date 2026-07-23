using UnityEngine;

// Procedural SOUND EFFECTS for interactions and UI — no audio files needed. It generates a handful of
// short tones at startup and plays them 2D. Static API so anything can call Sfx.Confirm(), Sfx.Click(),
// etc. from one line. Put ONE of these on GameManager.
public class Sfx : MonoBehaviour
{
    public static Sfx Instance;

    [Range(0f, 1f)] public float volume = 0.5f;
    [Tooltip("Master switch for all generated SFX.")]
    public bool sfxEnabled = true;

    AudioSource src;
    AudioClip pickup, confirm, keyFound, deny, click, deposit;

    void Awake()
    {
        Instance = this;
        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;   // 2D UI sound

        // Each sound is a short sequence of tones with a soft envelope so it doesn't click.
        pickup   = Tone(new[] { 880f, 1320f },        0.09f, 0.35f);
        confirm  = Tone(new[] { 523f, 784f },         0.14f, 0.40f);
        keyFound = Tone(new[] { 659f, 988f, 1319f },  0.30f, 0.45f);  // ascending chime
        deny     = Tone(new[] { 180f, 120f },         0.18f, 0.50f, square: true);
        click    = Tone(new[] { 1200f },              0.03f, 0.30f);
        deposit  = Tone(new[] { 330f, 220f },         0.16f, 0.45f);
    }

    // Build a mono clip from a list of tones (each held for an equal slice of the duration), with a
    // 10 ms attack and a linear decay to zero so the start/end don't pop.
    AudioClip Tone(float[] freqs, float duration, float amp, bool square = false)
    {
        const int rate = 44100;
        int total = Mathf.Max(1, (int)(rate * duration));
        float[] data = new float[total];
        int perTone = Mathf.Max(1, total / freqs.Length);

        for (int i = 0; i < total; i++)
        {
            float f = freqs[Mathf.Min(freqs.Length - 1, i / perTone)];
            float phase = 2f * Mathf.PI * f * (i / (float)rate);
            float wave = square ? Mathf.Sign(Mathf.Sin(phase)) : Mathf.Sin(phase);
            float attack = Mathf.Min(1f, i / (rate * 0.01f));
            float decay = 1f - (i / (float)total);
            data[i] = wave * amp * attack * decay;
        }

        AudioClip clip = AudioClip.Create("sfx", total, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void Play(AudioClip c)
    {
        if (!sfxEnabled || c == null || src == null) return;
        src.PlayOneShot(c, volume);
    }

    // --- static one-liners ---
    public static void Pickup()   { if (Instance != null) Instance.Play(Instance.pickup); }
    public static void Confirm()  { if (Instance != null) Instance.Play(Instance.confirm); }
    public static void KeyFound() { if (Instance != null) Instance.Play(Instance.keyFound); }
    public static void Deny()     { if (Instance != null) Instance.Play(Instance.deny); }
    public static void Click()    { if (Instance != null) Instance.Play(Instance.click); }
    public static void Deposit()  { if (Instance != null) Instance.Play(Instance.deposit); }
}
