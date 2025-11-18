using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BounceSpikes2D : MonoBehaviour
{
    [Header("Bounce")]
    [Tooltip("Upward velocity applied on bounce (units/sec).")]
    public float bounceVelocity = 14f;

    [Tooltip("Require at least this downward speed to trigger (enter + air re-bounce). Set <= 0 to ignore.")]
    public float minDownwardSpeed = 0.3f;

    [Tooltip("Zero horizontal speed on bounce.")]
    public bool zeroHorizontalOnBounce = false;

    [Header("Who can bounce")]
    [Tooltip("Layers allowed to trigger (e.g., Player, SmallMonster).")]
    public LayerMask bounceLayers;

    [Header("Usage Limits")]
    [Tooltip("Shared charges across ALL actors. -1 = unlimited.")]
    public int sharedCharges = -1;

    [Tooltip("Per-actor charges. -1 = unlimited.")]
    public int perActorCharges = -1;

    [Tooltip("Disable this trigger when shared charges reach zero.")]
    public bool disableWhenDepleted = true;

    [Header("Air re-bounce in trigger")]
    [Tooltip("Allow another bounce while still inside the trigger if the actor is falling again.")]
    public bool allowAirRebounceInTrigger = true;

    [Header("Resting bounce (optional)")]
    [Tooltip("Also bounce actors that are resting on top after a short delay.")]
    public bool bounceOnStay = true;

    [Tooltip("Time resting on the spikes before a stay-bounce fires.")]
    public float stayBounceDelay = 0.12f;

    [Header("Timing")]
    [Tooltip("Cooldown between bounces for the same actor (prevents spam).")]
    public float perActorCooldown = 0.15f;

    [Tooltip("After bounce, allow the player to press jump immediately within this window.")]
    public float immediateJumpWindow = 0.18f;

    [Header("Apex Hang (NEW)")]
    [Tooltip("Pause actors briefly at the top of their bounce before falling.")]
    public bool enableApexHang = true;

    [Tooltip("How long to pause at the apex (seconds).")]
    public float apexHangDuration = 0.15f;

    [Tooltip("How small vertical speed must be to consider we are at the apex.")]
    public float apexDetectEpsilon = 0.05f;

    [Tooltip("Safety limit while waiting to reach apex (seconds).")]
    public float apexWaitTimeout = 0.8f;

    [Header("Feedback (optional)")]
    public Color depletedTint = new Color(1f, 0.6f, 0.6f, 0.9f);
    public AudioClip bounceSfx;

    // --- internals ---
    private Collider2D trig;
    private SpriteRenderer sr;

    // Shared pool across scene (static so multiple spikes share it)
    private static int s_sharedRemaining = int.MinValue; // sentinel = uninitialized

    // Per-actor tracking
    private static readonly Dictionary<int, int>   s_actorRemaining  = new();
    private static readonly Dictionary<int, float> s_enterTime       = new();
    private static readonly Dictionary<int, float> s_lastBounceTime  = new();

    // Apex hang tracking
    private static readonly Dictionary<int, Coroutine> s_apexCoroutines = new();
    private static readonly Dictionary<int, float>     s_savedGravity    = new();

    void Awake()
    {
        trig = GetComponent<Collider2D>();
        sr   = GetComponent<SpriteRenderer>();

        // Ensure non-sticky trigger behavior
        if (!trig.isTrigger) trig.isTrigger = true;

        // Initialize shared pool once per domain reload
        if (s_sharedRemaining == int.MinValue)
            s_sharedRemaining = (sharedCharges < 0 ? int.MaxValue : sharedCharges);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null) return;
        if (!IsLayerAllowed(other.gameObject.layer)) return;

        int id = rb.GetInstanceID();
        s_enterTime[id] = Time.time;

        // Enter bounce requires downward speed (if enabled)
        if (minDownwardSpeed > 0f && rb.linearVelocity.y > -minDownwardSpeed) return;

        TryBounce(other, rb);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null) return;
        if (!IsLayerAllowed(other.gameObject.layer)) return;

        int id = rb.GetInstanceID();

        // 1) Air re-bounce while still inside the trigger and falling again
        if (allowAirRebounceInTrigger && minDownwardSpeed > 0f && rb.linearVelocity.y <= -minDownwardSpeed)
        {
            if (!s_lastBounceTime.TryGetValue(id, out var tLast) || Time.time - tLast >= perActorCooldown)
            {
                TryBounce(other, rb);
                return;
            }
        }

        // 2) Optional "resting" bounce (paused on top)
        if (!bounceOnStay) return;

        bool resting = rb.linearVelocity.y <= 0.01f; // not rising
        if (!resting) return;

        float enteredAt = s_enterTime.TryGetValue(id, out var tEnt) ? tEnt : Time.time;
        if (Time.time - enteredAt < stayBounceDelay) return;

        if (!s_lastBounceTime.TryGetValue(id, out var tLastRest) || Time.time - tLastRest >= perActorCooldown)
        {
            TryBounce(other, rb);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var rb = other.attachedRigidbody;
        if (rb != null)
        {
            int id = rb.GetInstanceID();
            s_enterTime.Remove(id);
            // no need to stop apex coroutine here; it runs on the actor after bounce
        }
    }

    // ---- Core helpers -------------------------------------------------------

    void TryBounce(Collider2D other, Rigidbody2D rb)
    {
        // Shared / per-actor quotas
        if (!ConsumeSharedIfAvailable()) { OnDepleted(); return; }
        if (!ConsumePerActorIfAvailable(rb.GetInstanceID())) return;

        // Apply bounce velocity
        Vector2 v = rb.linearVelocity;
        v.y = Mathf.Max(v.y, bounceVelocity);
        if (zeroHorizontalOnBounce) v.x = 0f;
        rb.linearVelocity = v;

        int id = rb.GetInstanceID();
        s_lastBounceTime[id] = Time.time;

        // Grant immediate jump window to Player
        var pm = rb.GetComponent<PlayerMovement>();
        if (pm != null && immediateJumpWindow > 0f)
            pm.GrantImmediateJumpWindow(immediateJumpWindow);

        if (bounceSfx) AudioSource.PlayClipAtPoint(bounceSfx, transform.position);

        // Start/Restart apex hang for this actor
        if (enableApexHang)
        {
            if (s_apexCoroutines.TryGetValue(id, out var running) && running != null)
                StopCoroutine(running);

            s_apexCoroutines[id] = StartCoroutine(ApexHangRoutine(rb, id));
        }
    }

    IEnumerator ApexHangRoutine(Rigidbody2D rb, int id)
    {
        if (rb == null) yield break;

        // Wait until near the apex (vy ~ 0 going from + to -), but bail after a timeout
        float start = Time.time;
        while (rb != null && Time.time - start < apexWaitTimeout)
        {
            // If actor started falling or vertical speed is tiny, we consider apex reached
            if (Mathf.Abs(rb.linearVelocity.y) <= apexDetectEpsilon || rb.linearVelocity.y <= 0f)
                break;
            yield return null;
        }

        if (rb == null) yield break;

        // Save and zero gravity, pin vertical velocity
        float originalGravity = rb.gravityScale;
        s_savedGravity[id] = originalGravity;

        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        // Let them hang
        float hangStart = Time.time;
        while (rb != null && Time.time - hangStart < apexHangDuration)
        {
            // keep vy zero so they don't drift up/down
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            yield return null;
        }

        if (rb != null)
        {
            // Restore gravity
            float restore = s_savedGravity.TryGetValue(id, out var g) ? g : 1f;
            rb.gravityScale = restore;
        }

        s_apexCoroutines[id] = null;
    }

    bool ConsumeSharedIfAvailable()
    {
        if (s_sharedRemaining <= 0) return false;
        if (sharedCharges >= 0) s_sharedRemaining--;
        return true;
    }

    bool ConsumePerActorIfAvailable(int actorId)
    {
        if (perActorCharges < 0) return true; // unlimited per actor

        if (!s_actorRemaining.TryGetValue(actorId, out int left))
            left = perActorCharges;

        if (left <= 0) return false;

        left--;
        s_actorRemaining[actorId] = left;
        return true;
    }

    bool IsLayerAllowed(int layer)
    {
        return bounceLayers.value == 0 || (bounceLayers.value & (1 << layer)) != 0;
    }

    void OnDepleted()
    {
        if (disableWhenDepleted && trig.enabled)
            trig.enabled = false;

        if (sr) sr.color = depletedTint;
    }

    // ---- Public API ---------------------------------------------------------

    /// <summary>
    /// Reset all spike quotas for a new level. Call from your Level/Scene manager.
    /// </summary>
    public static void ResetAllQuotas(int newSharedCharges)
    {
        s_sharedRemaining = (newSharedCharges < 0 ? int.MaxValue : newSharedCharges);
        s_actorRemaining.Clear();
        s_enterTime.Clear();
        s_lastBounceTime.Clear();

        // stop tracking apex coroutines/gravity (scene reload typically resets anyway)
        s_apexCoroutines.Clear();
        s_savedGravity.Clear();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.color = Color.white;
        string label = (sharedCharges < 0 ? "∞" :
                        (s_sharedRemaining == int.MinValue ? sharedCharges : s_sharedRemaining).ToString());
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"Spikes shared: {label}");
    }
#endif
}