using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class LargeMonster : MonoBehaviour
{
    [Header("Detect")]
    public float detectionRange = 6f;
    public LayerMask playerLayer;   // must include the Player layer

    [Header("Smash Motion")]
    [Tooltip("Local offset from rest to the bottom of the smash (negative Y goes down).")]
    public Vector2 smashOffset = new Vector2(0f, -1.5f);
    public float warningDuration = 0.6f;   // color flash before moving
    public float downTime = 0.18f;         // travel to bottom
    public float holdTime = 0.25f;         // zone active at bottom
    public float upTime = 0.22f;           // return to rest
    public float cooldown = 1.0f;          // wait before next check
    public AnimationCurve ease = AnimationCurve.EaseInOut(0,0,1,1);

    [Header("Hit Zone")]
    public Collider2D smashZone;           // child BoxCollider2D (disabled at start)

    [Header("Visuals (optional)")]
    public SpriteRenderer sr;
    public Color warningColor = new Color(1f, 0.7f, 0.2f);
    public Color normalColor = Color.white;

    Vector3 restPos;
    bool attacking;
    bool coolingDown;

    void Awake()
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        restPos = transform.position;

        if (smashZone) smashZone.enabled = false; // start off
    }

    void Update()
    {
        if (attacking || coolingDown) return;

        // Detect player inside range
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);
        if (hit && hit.TryGetComponent<PlayerMovement>(out var player))
        {
            // Don't attack if player is carrying a small monster
            if (player.IsCarryingSmallMonster()) return;

            StartCoroutine(AttackSequence());
        }
    }

    IEnumerator AttackSequence()
    {
        attacking = true;

        // Warning flash
        if (sr) sr.color = warningColor;
        yield return new WaitForSeconds(warningDuration);
        if (sr) sr.color = normalColor;

        Vector3 bottom = restPos + (Vector3)smashOffset;

        // Move down
        float t = 0f;
        while (t < downTime)
        {
            t += Time.deltaTime;
            float u = downTime <= 0f ? 1f : ease.Evaluate(t / downTime);
            transform.position = Vector3.Lerp(restPos, bottom, u);
            yield return null;
        }
        transform.position = bottom;

        // Enable hit zone while at bottom
        if (smashZone) smashZone.enabled = true;
        yield return new WaitForSeconds(holdTime);
        if (smashZone) smashZone.enabled = false;

        // Move up
        t = 0f;
        while (t < upTime)
        {
            t += Time.deltaTime;
            float u = upTime <= 0f ? 1f : ease.Evaluate(t / upTime);
            transform.position = Vector3.Lerp(bottom, restPos, u);
            yield return null;
        }
        transform.position = restPos;

        // Cooldown
        coolingDown = true;
        yield return new WaitForSeconds(cooldown);
        coolingDown = false;
        attacking = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // show bottom of smash
        Vector3 from = Application.isPlaying ? restPos : transform.position;
        Vector3 to = from + (Vector3)smashOffset;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(from, to);
        Gizmos.DrawSphere(to, 0.05f);
    }
}
