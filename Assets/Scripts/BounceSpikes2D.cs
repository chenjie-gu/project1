using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BounceSpikes2D : MonoBehaviour
{
    [Header("Bounce")]
    [Tooltip("Upward velocity to apply on hit (units/sec).")]
    public float bounceVelocity = 14f;

    [Tooltip("If true, only boosts when this would be higher than current upward speed.")]
    public bool onlyIfHigher = true;

    [Tooltip("Only bounce when the other object is above (typical ground spikes).")]
    public bool onlyFromTop = true;

    [Tooltip("Zero horizontal speed on bounce (optional).")]
    public bool zeroHorizontalOnBounce = false;

    [Header("Filtering")]
    [Tooltip("Who can bounce (usually just Player). Leave empty to allow any Rigidbody2D).")]
    public LayerMask bounceLayers;

    [Header("FX (optional)")]
    public AudioClip bounceSfx;
    public ParticleSystem bounceVfx;

    Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        // Make sure the collider is NOT a trigger if you want physical landing.
        // If you prefer trigger, add OnTriggerEnter2D doing the same as TryBounce().
    }

    void OnCollisionEnter2D(Collision2D c)  { TryBounce(c); }
    void OnCollisionStay2D (Collision2D c)  { TryBounce(c); } // catches edge cases

    void TryBounce(Collision2D c)
    {
        // Layer filter
        if (bounceLayers.value != 0 && ((1 << c.gameObject.layer) & bounceLayers.value) == 0)
            return;

        // Find the other body's RB (player)
        var rb = c.rigidbody; // this is the Rigidbody2D of the other collider
        if (rb == null) return;

        // Top-only filter: require at least one upward-facing contact (normal pointing up)
        if (onlyFromTop)
        {
            bool valid = false;
            foreach (var contact in c.contacts)
            {
                // Normal is from spikes into the other collider; for a player landing on top,
                // the normal on the spikes points UP (y > 0.5).
                if (contact.normal.y > 0.5f) { valid = true; break; }
            }
            if (!valid) return;
        }

        // Apply bounce
        Vector2 v = rb.linearVelocity;
        float targetY = bounceVelocity;
        if (onlyIfHigher) v.y = Mathf.Max(v.y, targetY);
        else              v.y = targetY;

        if (zeroHorizontalOnBounce) v.x = 0f;

        rb.linearVelocity = v;

        // Optional: reset player's grounded timers/flags if your controller needs it
        // var pm = rb.GetComponent<PlayerMovement>();
        // if (pm) pm.ForceUngroundedFor(0.05f);

        // FX
        if (bounceSfx) AudioSource.PlayClipAtPoint(bounceSfx, transform.position);
        if (bounceVfx) bounceVfx.Play();
    }

    // If you prefer using a Trigger collider on the spikes instead of a solid collider,
    // add this and ensure the spikes' collider has IsTrigger = true:
    void OnTriggerEnter2D(Collider2D other)
    {
        if (col != null && !col.isTrigger) return;
        var rb = other.attachedRigidbody;
        if (rb == null) return;

        if (bounceLayers.value != 0 && ((1 << other.gameObject.layer) & bounceLayers.value) == 0)
            return;

        // Assume top hit when using trigger; remove if you want side-bounce too
        Vector2 v = rb.linearVelocity;
        v.y = (onlyIfHigher ? Mathf.Max(v.y, bounceVelocity) : bounceVelocity);
        if (zeroHorizontalOnBounce) v.x = 0f;
        rb.linearVelocity = v;

        if (bounceSfx) AudioSource.PlayClipAtPoint(bounceSfx, transform.position);
        if (bounceVfx) bounceVfx.Play();
    }
}
