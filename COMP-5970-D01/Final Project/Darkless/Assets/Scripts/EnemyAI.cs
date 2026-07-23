using UnityEngine;
using MimicSpace;

// Darkless — Phase 4: the Mimic's behaviour, as a STATE MACHINE.
// Design + diagram: vault note "Darkless - Monster AI".
//
// States:
//   Retreat    — flee FAST away from the player, break line of sight, then VANISH (renderers off).
//                Also the daytime dormant state: it stays hidden through the day.
//   Stalking   — invisible; teleports between DARK spots near the player, holding distance,
//                waiting out a patience timer before it commits.
//   Attacking  — visible; closes in. Its speed is throttled by how brightly lit the ground is
//                (bright = crawl, dark = fast). Touch = death.
//   Reposition — walked into campfire/lantern light mid-attack; slips back out to the dark to resume.
//
// Master rule (wins from ANY active state): flashlight ON it OR daytime -> Retreat.
//
// "In / near light" is detected with the existing LightZone system:
//   * the flashlight owns its own LightZone, so "flashlight on the mimic" = that zone covers us,
//   * any other zone (campfire / lantern) covering us = "wandered into a lit area".
//
// Put this on the Mimic (alongside its Mimic component). Replaces the placeholder chase.
[RequireComponent(typeof(Mimic))]
public class EnemyAI : MonoBehaviour
{
    public enum State { Retreat, Stalking, Attacking, Reposition }

    [Header("Target")]
    [Tooltip("Who to hunt. Auto-finds the object tagged 'Player' at Start if left empty.")]
    public Transform target;
    public string playerTag = "Player";

    [Header("Speeds (units/second)")]
    [Tooltip("Attacking base speed in the dark. Keep a touch under the player's sprint so they can " +
             "'just' outrun it with a torch (GDD §5).")]
    public float attackSpeed = 4.2f;
    [Tooltip("Retreat speed multiplier: when it flees it moves this many times its attack speed " +
             "(5 = it panics and bolts for cover).")]
    public float retreatSpeedMultiplier = 5f;
    [Tooltip("Reposition speed — how fast it slips to a darker spot mid-attack (not a full retreat).")]
    public float fleeSpeed = 8f;

    [Header("Movement feel")]
    [Range(0.5f, 5f)] public float height = 0.8f;   // body height above the ground
    public float velocityLerpCoef = 6f;             // how quickly velocity eases to target
    public float turnSpeed = 200f;                  // degrees/sec the body turns to face travel

    [Header("Stalking")]
    [Tooltip("Preferred lurking distance from the player while stalking.")]
    public float stalkDistanceMin = 7f;
    public float stalkDistanceMax = 13f;
    [Tooltip("Seconds between teleports to a fresh dark spot while stalking.")]
    public float teleportInterval = 2.5f;

    [Header("Attack / give-up")]
    [Tooltip("Contact range: within this (horizontal metres) the player dies.")]
    public float killDistance = 1.5f;
    [Tooltip("If the player stays safe in the CAMPFIRE (the hard-barrier light) this long during an " +
             "attack, it gives up to Stalking.")]
    public float giveUpTime = 3f;
    [Tooltip("If the player opens up at least this much distance during an attack, the Mimic breaks off " +
             "(→ Stalking or Reposition). This is the 'run away' escape — pull far enough and it stops.")]
    public float escapeDistance = 16f;

    [Header("Light slow (attacking)")]
    [Tooltip("Light doesn't stop the Mimic (except the campfire barrier) — it SLOWS it, linearly with " +
             "the light's strength (LightZone.slowStrength). Slow is temporary (only while lit). At MAX " +
             "aggression the slow is only this fraction as effective, so a frenzied Mimic barely cares " +
             "about your torch (0.25 = a quarter as slow at full aggression).")]
    [Range(0f, 1f)] public float aggressionSlowFloor = 0.25f;

    [Header("Aggression & the night ramp")]
    [Tooltip("Drive aggression from the night number automatically (recommended). The Mimic gets more " +
             "aggressive every night: shorter stalks before it decides, and a higher chance that " +
             "decision is an ATTACK. Turn off to set 'aggression' by hand.")]
    public bool aggressionFromNight = true;
    [Tooltip("Night the Mimic reaches FULL aggression (frenzied). Night 1 = calmest.")]
    public int nightsToMaxAggression = 6;
    [Range(0f, 1f)]
    [Tooltip("How aggressive the monster is (0 calm … 1 frenzied). Auto-set from the night when " +
             "'Aggression From Night' is on. Higher = shorter stalks before deciding, weaker light-slow, " +
             "faster re-engage.")]
    public float aggression = 0f;
    [Tooltip("Base seconds it lies low after being scared off before it resumes stalking (at neutral " +
             "aggression). Aggression scales this down — higher aggression = comes back sooner.")]
    public float reengageDelay = 3f;

    [Header("Attack-or-retreat decision (ramps per night)")]
    [Tooltip("After stalking a while the Mimic DECIDES: attack, or give up and retreat. This is its " +
             "chance to ATTACK on NIGHT 1 (so early on it mostly retreats — ~1 attack the first night).")]
    [Range(0f, 1f)] public float attackChanceNight1 = 0.15f;
    [Tooltip("Attack chance ADDED each night after the first (+0.15 = 15% more per night).")]
    [Range(0f, 1f)] public float attackChancePerNight = 0.15f;
    [Tooltip("Maximum attack chance, however late the night (caps the ramp).")]
    [Range(0f, 1f)] public float attackChanceMax = 0.90f;
    [Tooltip("Seconds it stalks before each decision at LOW aggression (night 1) — long, so attacks are " +
             "RARE early. Roughly (night length / this) × attack-chance ≈ attacks that night.")]
    public float decisionIntervalMax = 55f;
    [Tooltip("Seconds it stalks before each decision at FULL aggression (late game) — short, so it " +
             "attacks often. A ±25% jitter is applied so it isn't clockwork.")]
    public float decisionIntervalMin = 18f;

    [Header("Vision")]
    [Tooltip("Beyond this distance the player is considered NOT to see the monster (fog hides it), " +
             "so it may vanish even while roughly in front of them.")]
    public float maxSeeDistance = 40f;

    [Header("Audio")]
    [Tooltip("Looping footstep/skitter sound. Plays ONLY while Attacking — the cue that it's " +
             "actively closing in on you. 3D, so it gets louder as it nears.")]
    public AudioClip walkClip;
    [Tooltip("Rustle sounds for Stalking. One is picked at random and played each time it " +
             "repositions (at least once per stalk). Because the monster is invisible while " +
             "stalking, this 3D rustle from its position is your only cue to where it is.")]
    public AudioClip[] stalkRustleClips;
    [Range(0f, 1f)] public float walkVolume = 0.9f;
    [Range(0f, 1f)] public float rustleVolume = 1f;
    [Tooltip("Distance at which the monster's own sounds fade out (linear 3D rolloff).")]
    public float soundMaxDistance = 35f;
    [Tooltip("Chance it rustles on each stalk TELEPORT at LOW aggression (night 1) — kept low so it " +
             "isn't heard too often early. A guaranteed rustle still plays when it STARTS stalking, so " +
             "you always get at least one cue before an attack.")]
    [Range(0f, 1f)] public float rustleChanceMin = 0.15f;
    [Tooltip("Chance it rustles on each teleport at FULL aggression (late game) — a frenzied Mimic is " +
             "noisier. Scales linearly with aggression between min and max.")]
    [Range(0f, 1f)] public float rustleChanceMax = 0.6f;

    [Header("Debug marker (testing only)")]
    [Tooltip("Show an always-visible beacon above the monster + an on-screen readout of its state " +
             "and distance — even while it's invisible/stalking. Turn OFF for real play.")]
    public bool showDebugMarker = true;
    [Tooltip("Colour of the debug beacon and the on-screen text.")]
    public Color markerColor = new Color(1f, 0f, 1f, 1f); // magenta
    [Tooltip("Current state — read-only; watch it change in Play mode.")]
    public State state = State.Retreat;

    // --- internals ---
    Mimic mimic;
    MeshRenderer bodyRenderer;
    DayNightCycle dayNight;
    Flashlight flashlight;
    LightZone flashlightZone;
    FlashlightBeamCone flashlightBeamCone;
    Light flashlightBeam;
    Camera cam;
    AudioSource walkSource;    // looping footsteps while Attacking
    AudioSource rustleSource;  // one-shot rustles while Stalking

    Vector3 velocity;
    Vector3 retreatDir; // the (fixed, random) heading it flees along this retreat
    bool visible = true;
    float teleportTimer;
    float decisionTimer;   // counts down while Stalking; at 0 it rolls attack-or-retreat
    float reengageTimer;
    float playerSafeTimer;
    bool retreatVanished; // true once Retreat has broken line of sight — it then stays hidden
    GameObject marker; // optional debug beacon (created at runtime, never saved to the scene)

    void Start()
    {
        mimic = GetComponent<Mimic>();
        bodyRenderer = GetComponentInChildren<MeshRenderer>();

        // Kill the asset's built-in Movement if it's still around (legacy input, player-driven).
        Movement builtIn = GetComponent<Movement>();
        if (builtIn != null) builtIn.enabled = false;

        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) target = p.transform;
        }

        dayNight = FindFirstObjectByType<DayNightCycle>();
        flashlight = FindFirstObjectByType<Flashlight>();
        if (flashlight != null)
        {
            flashlightZone = flashlight.GetComponent<LightZone>();
            flashlightBeam = flashlight.beam;
            if (flashlight.beamCone != null)
                flashlightBeamCone = flashlight.beamCone.GetComponent<FlashlightBeamCone>();
        }

        // Build our own 3D audio sources so no manual wiring is needed — just assign the clips.
        // (Created before EnterRetreat, which stops the walk source.)
        walkSource = CreateAudioSource(loop: true, volume: walkVolume);
        walkSource.clip = walkClip;
        rustleSource = CreateAudioSource(loop: false, volume: rustleVolume);

        if (showDebugMarker) CreateMarker();
        EnterRetreat();
    }

    // A fully-3D AudioSource on the monster, so direction and distance read naturally even while
    // the creature itself is invisible (Stalking) or fading in from the fog.
    AudioSource CreateAudioSource(bool loop, float volume)
    {
        AudioSource a = gameObject.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.loop = loop;
        a.volume = volume;
        a.spatialBlend = 1f;                       // fully 3D
        a.rolloffMode = AudioRolloffMode.Linear;
        a.minDistance = 3f;
        a.maxDistance = soundMaxDistance;
        return a;
    }

    // Pick a random rustle and play it from the monster's current (often invisible) position.
    void PlayStalkRustle()
    {
        if (rustleSource == null || stalkRustleClips == null || stalkRustleClips.Length == 0) return;
        AudioClip clip = stalkRustleClips[Random.Range(0, stalkRustleClips.Length)];
        if (clip != null) rustleSource.PlayOneShot(clip, rustleVolume);
    }

    // Keep the beacon parked above the monster (works even while its body renderers are off).
    void LateUpdate()
    {
        if (showDebugMarker && marker == null) CreateMarker(); // allow toggling on mid-play
        if (marker == null) return;

        marker.SetActive(showDebugMarker);
        if (showDebugMarker)
            marker.transform.position = transform.position + Vector3.up * 7f;
    }

    // A tall, bright, unlit pillar that ignores the monster's invisibility. No collider, so it
    // never interferes with the ground raycasts or the kill check.
    void CreateMarker()
    {
        marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "MimicDebugMarker";
        Collider col = marker.GetComponent<Collider>();
        if (col != null) Destroy(col);
        marker.transform.localScale = new Vector3(0.4f, 14f, 0.4f);

        MeshRenderer mr = marker.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        Material m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", markerColor);
        if (m.HasProperty("_Color")) m.SetColor("_Color", markerColor);
        mr.sharedMaterial = m;
    }

    // On-screen readout so you can tell "hiding" from "glitching" at a glance.
    void OnGUI()
    {
        if (!showDebugMarker || target == null) return;
        GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
        style.normal.textColor = markerColor;
        string info = $"MIMIC:  {state}   N{CurrentNight}  atk {NightAttackChance:P0}   " +
                      $"{FlatDistanceToPlayer():0.0} m   {(visible ? "visible" : "HIDDEN")}";
        GUI.Label(new Rect(Screen.width * 0.5f - 240f, 8f, 620f, 26f), info, style);
    }

    void Update()
    {
        if (target == null) return;
        if (cam == null) cam = Camera.main; // cache lazily; the player camera may spawn after us

        // Night ramp: aggression rises each night (night 1 = calm, nightsToMaxAggression = frenzied).
        if (aggressionFromNight)
            aggression = Mathf.Clamp01((CurrentNight - 1f) / Mathf.Max(1, nightsToMaxAggression - 1));

        // ---- The master interrupt: light on us / daytime always sends us fleeing. ----
        // Enter Retreat and fall straight through to Tick it THIS frame — no stall, no downtime.
        if (state != State.Retreat && (LitByFlashlight() || IsDay()))
            EnterRetreat();

        switch (state)
        {
            case State.Retreat:    TickRetreat();    break;
            case State.Stalking:   TickStalking();   break;
            case State.Attacking:  TickAttacking();  break;
            case State.Reposition: TickReposition(); break;
        }
    }

    // ------------------------------------------------------------------ states

    void EnterRetreat()
    {
        state = State.Retreat;
        retreatVanished = false;
        // How long it lies low before resuming the hunt — shorter when it's more aggressive.
        reengageTimer = reengageDelay * AggressionTimeScale;

        // Pick a random flee heading in the hemisphere pointing AWAY from the player (±85°), so it
        // darts off in some direction rather than straight back — but never runs across the player.
        Vector3 away = target != null ? FlatDir(transform.position - target.position) : FlatDir(transform.forward);
        if (away == Vector3.zero) away = FlatDir(transform.forward);
        retreatDir = Quaternion.Euler(0f, Random.Range(-85f, 85f), 0f) * away;

        // Snap velocity to the flee direction so it bolts INSTANTLY — no momentum lag when the
        // flashlight catches it mid-attack (this is what kills the janky reaction).
        velocity = retreatDir * RetreatSpeed();
        SetVisible(true);
        if (walkSource != null) walkSource.Stop();
    }

    void TickRetreat()
    {
        // --- Phase 1: RUN away (visible) until we break the player's line of sight. ---
        // While the player can see us we NEVER teleport — we sprint. We only vanish once unseen.
        if (!retreatVanished)
        {
            if (PlayerCanSeeMe())
            {
                SetVisible(true);
                Move(retreatDir, RetreatSpeed());
                return;
            }
            // Out of sight -> vanish and LATCH hidden; it won't re-appear on its own.
            retreatVanished = true;
            SetVisible(false);
        }

        // --- Phase 2: vanished/dormant. Lie low, then resume stalking at an opportune time. ---
        // Opportune = it's night and nothing is shining on us. Aggression sets how long it lies low
        // (reengageTimer). During the day it simply stays here, dormant, until night falls.
        reengageTimer -= Time.deltaTime;
        if (reengageTimer <= 0f && IsNight() && !LitByFlashlight())
            EnterStalking();
    }

    void EnterStalking()
    {
        state = State.Stalking;
        SetVisible(false);
        teleportTimer = 0f;
        // How long it stalks before it decides attack-or-retreat: long at low aggression, short when
        // frenzied, with a ±25% jitter so it isn't clockwork.
        decisionTimer = DecisionInterval * Random.Range(0.75f, 1.25f);
        if (walkSource != null) walkSource.Stop();
        // Snap to a dark spot right away so it's lurking at a sensible distance.
        TeleportToDark(preferOutOfView: true, mustBeOutOfView: false);
        // Guarantee the player hears it at least once each time it starts stalking.
        PlayStalkRustle();
    }

    void TickStalking()
    {
        // Invisible the whole time; hold position, just count timers and reposition occasionally.
        mimic.velocity = Vector3.zero;

        teleportTimer -= Time.deltaTime;
        if (teleportTimer <= 0f)
        {
            teleportTimer = teleportInterval * AggressionTimeScale;
            TeleportToDark(preferOutOfView: true, mustBeOutOfView: false);
            // A fresh rustle from the new spot (the directional cue) — but only SOMETIMES, so it isn't
            // heard too often. The chance rises with aggression (a frenzied Mimic is noisier). The
            // guaranteed rustle in EnterStalking still ensures at least one cue before any attack.
            if (Random.value < RustleChance) PlayStalkRustle();
        }

        // After stalking a while, DECIDE: attack, or give up and retreat. Early nights it almost always
        // retreats (low NightAttackChance) and decides rarely (long DecisionInterval); later nights it
        // attacks more AND decides more often — so attacks-per-night ramp up over the game.
        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f)
        {
            if (Random.value < NightAttackChance) EnterAttacking();
            else EnterRetreat();   // Stalking -> Retreat: slink off and lie low before trying again
        }
    }

    void EnterAttacking()
    {
        state = State.Attacking;
        SetVisible(true);
        playerSafeTimer = 0f;
        // Footsteps only while actively attacking — the sound of it closing in.
        if (walkSource != null && walkClip != null && !walkSource.isPlaying) walkSource.Play();
    }

    void TickAttacking()
    {
        float dist = FlatDistanceToPlayer();

        // Contact kills.
        if (dist <= killDistance) { Kill(); return; }

        // The CAMPFIRE is the ONE light it won't push into (a hard barrier) — slip back to the dark.
        // Torches / flashlight / lanterns do NOT block it; they only slow it (below).
        if (LightZone.AnyBarrierCovers(transform.position))
        {
            EnterReposition();
            return;
        }

        // The player reached the campfire safe zone. If they wait it out there, give up and re-stalk.
        if (PlayerSafe())
        {
            playerSafeTimer += Time.deltaTime;
            if (playerSafeTimer >= giveUpTime) { EnterStalking(); return; }
        }
        else playerSafeTimer = 0f;

        // "Run away" escape: if the player has opened up enough distance, the attack ENDS. Whether it
        // drops back to Stalking or Repositions depends on how much light is on the Mimic right now —
        // more light -> more likely it recoils to the dark (Reposition) rather than calmly re-stalking.
        if (dist >= escapeDistance)
        {
            float lightOnMe = LightZone.MaxSlowAt(transform.position);
            if (Random.value < lightOnMe) EnterReposition();
            else EnterStalking();
            return;
        }

        // Close in. Light SLOWS it linearly with the light's strength (LightZone.slowStrength), and that
        // slow is itself weakened by aggression — so a torch buys you time to run, and a shone flashlight
        // (strong light) nearly stalls it, but a frenzied Mimic pushes through. The slow is temporary:
        // it's recomputed from the current light every frame, so stepping out of the light restores speed.
        float rawSlow = LightZone.MaxSlowAt(transform.position);
        float effSlow = Mathf.Clamp01(rawSlow * Mathf.Lerp(1f, aggressionSlowFloor, Mathf.Clamp01(aggression)));
        float speed = attackSpeed * (1f - effSlow);
        Move(FlatDir(target.position - transform.position), speed);
    }

    void EnterReposition()
    {
        state = State.Reposition;
        SetVisible(true);
        // Not "attacking" per the design — silence the footsteps while it slips back to the dark.
        if (walkSource != null) walkSource.Stop();
    }

    void TickReposition()
    {
        // Move away from whatever light is on us, biased toward the player so it stays engaged.
        LightZone zone = NearestCoveringZone(transform.position);
        if (zone == null) { EnterAttacking(); return; } // reached the dark -> resume the hunt

        Vector3 awayFromLight = FlatDir(transform.position - zone.transform.position);
        Vector3 towardPlayer = FlatDir(target.position - transform.position);
        Vector3 dir = FlatDir(awayFromLight * 1.5f + towardPlayer * 0.5f);
        Move(dir, fleeSpeed);
    }

    // ------------------------------------------------------------------ movement

    // Ease velocity toward dir*speed, step, stick to the ground, feed the Mimic, and face travel.
    void Move(Vector3 dir, float speed)
    {
        Vector3 desired = dir * speed;
        velocity = Vector3.Lerp(velocity, desired, velocityLerpCoef * Time.deltaTime);

        transform.position += velocity * Time.deltaTime;
        transform.position = GroundSnap(transform.position);

        mimic.velocity = velocity; // so the IK legs reach in the travel direction

        if (velocity.sqrMagnitude > 0.01f)
        {
            Quaternion look = Quaternion.LookRotation(FlatDir(velocity));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
        }
    }

    // Raycast down and place the body `height` above the ground (mirrors the asset's Movement).
    Vector3 GroundSnap(Vector3 pos)
    {
        if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 50f))
            return new Vector3(pos.x, Mathf.Lerp(pos.y, hit.point.y + height, velocityLerpCoef * Time.deltaTime), pos.z);
        return pos;
    }

    // Try to jump to a dark spot around the player at lurking distance. Returns true on success.
    bool TeleportToDark(bool preferOutOfView, bool mustBeOutOfView)
    {
        const int samples = 14;
        Vector3 best = transform.position;
        bool found = false;

        for (int i = 0; i < samples; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(stalkDistanceMin, stalkDistanceMax);
            Vector3 candidate = target.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * dist;

            // Must land on ground.
            if (!Physics.Raycast(candidate + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 200f))
                continue;
            candidate = hit.point + Vector3.up * height;

            // Must be dark (and not hugging the edge of a light).
            if (InAnyLight(candidate)) continue;

            bool outOfView = !PointInView(candidate);
            if (mustBeOutOfView && !outOfView) continue;

            best = candidate;
            found = true;
            // Prefer spots the player isn't looking at; take the first out-of-view hit immediately.
            if (!preferOutOfView || outOfView) break;
        }

        if (found)
        {
            transform.position = best;
            velocity = Vector3.zero;
            mimic.velocity = Vector3.zero;
        }
        return found;
    }

    // ------------------------------------------------------------------ visibility

    // Show/hide the whole creature (body + all spawned legs) and freeze/unfreeze the leg spawner.
    // Hidden also means it's gone from the mini-map, since its renderers stop drawing.
    void SetVisible(bool v)
    {
        if (visible == v) return;
        visible = v;

        if (bodyRenderer != null) bodyRenderer.enabled = v;
        if (mimic != null) mimic.enabled = v; // freeze / allow the leg spawner

        // Going invisible: recycle every leg so NONE are left drawn at the old spot (this is what
        // made legs linger after a teleport). Going visible: the unfrozen spawner grows fresh legs.
        if (!v) HideAllLegs();
    }

    // Pull every ACTIVE leg back into the Mimic's pool (deactivated + line geometry cleared) and
    // zero the leg counters, so the creature shows no legs while hidden and regrows fresh ones on
    // reveal. Pooled/inactive legs are skipped so they're not double-added.
    void HideAllLegs()
    {
        if (mimic == null) return;
        Leg[] legs = GetComponentsInChildren<Leg>(true);
        foreach (Leg leg in legs)
        {
            if (leg == null || !leg.gameObject.activeSelf) continue;
            LineRenderer lr = leg.GetComponent<LineRenderer>();
            if (lr != null) lr.positionCount = 0; // erase the drawn line immediately
            mimic.RecycleLeg(leg.gameObject);      // adds to pool + SetActive(false)
        }
        mimic.legCount = 0;
        mimic.deployedLegs = 0;
    }

    // ------------------------------------------------------------------ conditions

    bool IsNight() => dayNight != null && dayNight.IsNight;
    bool IsDay()   => dayNight != null && !dayNight.IsNight;

    // Aggression compresses every "wait" timer. 0 = calm/patient, 1 = frenzied. So higher
    // aggression = shorter patience before attacking AND a shorter lie-low before resuming the
    // stalk = it transitions into Stalking and Attacking more often.
    float AggressionTimeScale => Mathf.Lerp(1.8f, 0.3f, Mathf.Clamp01(aggression));

    // Which night it is (1-based). Reads the day/night clock's counter; 1 if there's no clock.
    int CurrentNight => dayNight != null ? Mathf.Max(1, dayNight.NightNumber) : 1;

    // Chance a stalking decision is an ATTACK this night: night-1 base + per-night step, capped.
    // Night 1 ≈ 0.15 (mostly retreats), rising 0.15/night to the cap (default 0.90).
    float NightAttackChance =>
        Mathf.Clamp(attackChanceNight1 + attackChancePerNight * (CurrentNight - 1), 0f, attackChanceMax);

    // Seconds it stalks before each decision: long when calm (night 1), short when frenzied. The
    // shorter this is, the MORE decisions per night, so attacks-per-night rise with aggression too.
    float DecisionInterval => Mathf.Lerp(decisionIntervalMax, decisionIntervalMin, Mathf.Clamp01(aggression));

    // Chance of a rustle on each stalk-teleport, rising linearly with aggression (noisier late game).
    float RustleChance => Mathf.Lerp(rustleChanceMin, rustleChanceMax, Mathf.Clamp01(aggression));

    // Full flee speed = attack speed × the retreat multiplier.
    float RetreatSpeed() => attackSpeed * retreatSpeedMultiplier;

    // Change how aggressive the monster is at runtime (e.g. the night difficulty ramp in Phase 6).
    public void SetAggression(float value) => aggression = Mathf.Clamp01(value);

    // Is the flashlight's BEAM actually on the monster? Uses the beam cone as the hitbox — the
    // flashlight is the player's strongest weapon, so aiming it at the mimic must reliably scare it.
    bool LitByFlashlight()
    {
        if (flashlight == null || !flashlight.isOn) return false;

        // Prefer the visible beam cone as the hitbox; fall back to the Spot Light, then the zone.
        if (flashlightBeamCone != null)
            return InCone(flashlightBeamCone.transform, flashlightBeamCone.length, flashlightBeamCone.coneAngle);
        if (flashlightBeam != null)
            return InCone(flashlightBeam.transform, flashlightBeam.range, flashlightBeam.spotAngle);
        return flashlightZone != null && flashlightZone.Covers(transform.position);
    }

    // Inside a cone whose apex is `origin`, aimed along origin.forward, `length` metres long and
    // `fullAngle` degrees wide (the same shape the beam mesh is built from)?
    bool InCone(Transform origin, float length, float fullAngle)
    {
        Vector3 to = transform.position - origin.position;
        if (to.sqrMagnitude > length * length) return false;
        return Vector3.Angle(origin.forward, to) <= fullAngle * 0.5f;
    }

    // Inside ANY safe light zone (campfire / lantern / flashlight).
    bool InAnyLight(Vector3 point) => LightZone.AnyCovers(point);

    // The player has reached a true SAFE ZONE — the campfire (hard-barrier) light. Holding a torch
    // only SLOWS the Mimic; it doesn't make you 'safe' enough for it to give up the attack, so near a
    // torch you still have to actually lose it (run away / shine the flashlight), not just stand there.
    bool PlayerSafe() => LightZone.AnyBarrierCovers(target.position);

    LightZone NearestCoveringZone(Vector3 point)
    {
        LightZone best = null;
        float bestSq = float.MaxValue;
        for (int i = 0; i < LightZone.Active.Count; i++)
        {
            LightZone z = LightZone.Active[i];
            if (z == null || !z.Covers(point)) continue;
            float sq = (z.transform.position - point).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; best = z; }
        }
        return best;
    }

    // Can the player's camera actually see the monster (in the view frustum, in range, unoccluded)?
    bool PlayerCanSeeMe()
    {
        if (cam == null) return false;
        Vector3 pos = transform.position;
        if ((pos - cam.transform.position).sqrMagnitude > maxSeeDistance * maxSeeDistance) return false;
        // In the camera's view frustum and in range. (Occlusion by trees is left to the retreat
        // timeout below, to avoid a first-person ray clipping the player's own body.)
        return PointInView(pos);
    }

    bool PointInView(Vector3 world)
    {
        if (cam == null) return false;
        Vector3 vp = cam.WorldToViewportPoint(world);
        return vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
    }

    // ------------------------------------------------------------------ helpers

    static Vector3 FlatDir(Vector3 v) { v.y = 0f; return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.zero; }
    float FlatDistanceToPlayer() { Vector3 d = target.position - transform.position; d.y = 0f; return d.magnitude; }

    void Kill()
    {
        PlayerDarkness pd = target.GetComponentInParent<PlayerDarkness>();
        if (pd != null) pd.Die("It found you.");
        else UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
