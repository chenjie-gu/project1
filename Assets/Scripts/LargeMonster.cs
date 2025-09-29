using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class LargeMonster : MonoBehaviour
{
    [Header("Detect")]
    public float detectionRange = 6f;
    public LayerMask playerLayer;

    [Header("Attack")]
    public float warningDuration = 0.6f;
    public float smashActiveDuration = 0.6f;
    public float cooldown = 1.2f;

    [Tooltip("Area that becomes dangerous/blocks during smash. Keep disabled by default.")]
    public Collider2D smashZone;   // e.g., BoxCollider2D; leave disabled until attack

    [Header("Visuals (optional)")]
    public SpriteRenderer sr;
    public Color warningColor = new Color(1f, 0.7f, 0.2f);
    public Color normalColor = Color.white;

    bool attacking;
    bool coolingDown;

    void Reset()
    {
        // If this object's collider is used as a detector, keep it trigger.
        var c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;
    }

    void Update()
    {
        if (attacking || coolingDown) return;

        // Detect player around the monster
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);
        if (hit != null && hit.TryGetComponent<PlayerMovement>(out var player))
        {
            // If player is carrying small monster, do nothing
            if (player.IsCarryingSmallMonster())
                return;

            StartCoroutine(AttackSequence(player));
        }
    }

    IEnumerator AttackSequence(PlayerMovement player)
    {
        attacking = true;

        // Warning phase (flash color, play anim, etc.)
        if (sr != null) sr.color = warningColor;
        yield return new WaitForSeconds(warningDuration);
        if (sr != null) sr.color = normalColor;

        // Smash phase: enable smashZone temporarily
        if (smashZone != null)
        {
            bool wasEnabled = smashZone.enabled;
            smashZone.enabled = true; // must be enabled for OverlapCollider

            // Optional: if smashZone is set to trigger, "kill" player inside it
            if (smashZone.isTrigger)
            {
                // Build a no-filter ContactFilter2D
                ContactFilter2D filter = new ContactFilter2D();
                filter.NoFilter();

                // Buffer to receive overlaps
                Collider2D[] results = new Collider2D[8];
                int count = Physics2D.OverlapCollider(smashZone, filter, results);

                for (int i = 0; i < count; i++)
                {
                    var c = results[i];
                    if (c != null && c.TryGetComponent<PlayerMovement>(out var p))
                    {
                        Debug.LogError("Player failed: smashed by large monster!");
                        // TODO: call your game-over/respawn logic here
                        break;
                    }
                }
            }

            yield return new WaitForSeconds(smashActiveDuration);
            smashZone.enabled = wasEnabled;
        }

        // Cooldown before next attack
        coolingDown = true;
        yield return new WaitForSeconds(cooldown);
        coolingDown = false;
        attacking = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
