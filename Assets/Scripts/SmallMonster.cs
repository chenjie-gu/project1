using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class SmallMonster : MonoBehaviour, ICarryable
{
    public enum State { Patrol, Charge, Return, Flattened, Carried }

    [Header("Patrol")]
    public Transform leftPoint;
    public Transform rightPoint;
    public float speedX = 2f;
    public float waitAtEnds = 0.15f;

    [Header("Detect & Charge")]
    public float detectionRangeY = 4f;
    public LayerMask playerLayer;
    public float preChargePause = 0.35f;     // NEW: pause before charging
    public float chargeSpeedZ = 5f;
    public float chargeDistance = 6f;
    public float postChargePause = 0.15f;
    public float chargeBreakImpulse = 4.5f;  // threshold to break cages

    [Header("Flatten (by hammer)")]
    public Sprite flattenedSprite;
    public bool canBeCarriedOnlyWhenFlattened = true;

    [Header("Carry Setup")]
    public Vector2 holdLocalOffset = new Vector2(0f, 1.2f);

    [Header("Fail / Feedback")]
    public bool failOnTouchPlayer = true;

    // --- runtime ---
    State state = State.Patrol;
    Rigidbody2D rb;
    Collider2D col;
    SpriteRenderer sr;

    Vector2 startPos;
    bool movingToRight = true;
    int facing = 1;
    Vector2 velocity;

    Sprite normalSprite;

    // sorting
    int originalOrder, carriedOrder;

    // ICarryable
    public bool IsHeld { get; private set; } = false;
    public bool IsFlattened => state == State.Flattened || state == State.Carried;

    // flag so cages know a hit came from a charge
    public bool IsCharging { get; private set; } = false;   // NEW

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        normalSprite = sr.sprite;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        startPos = transform.position;
        originalOrder = sr ? sr.sortingOrder : 0;

        if (leftPoint == null || rightPoint == null)
            Debug.LogWarning("SmallMonster: assign leftPoint and rightPoint for patrol.");
    }

    void Update()
    {
        if (state == State.Carried) return;

        switch (state)
        {
            case State.Patrol:
                PatrolLogic();
                if (PlayerDetected()) StartCoroutine(ChargeRoutine()); // will include pause
                break;
            case State.Return:
                ReturnLogic();
                break;
            case State.Flattened:
                velocity = Vector2.zero;
                break;
            case State.Charge:
                // handled in coroutine
                break;
        }
    }

    void FixedUpdate()
    {
        if (state == State.Carried) return;
        rb.linearVelocity = velocity;
    }

    void PatrolLogic()
    {
        if (!leftPoint || !rightPoint) { velocity = Vector2.zero; return; }

        Transform target = movingToRight ? rightPoint : leftPoint;
        facing = movingToRight ? 1 : -1;

        float dir = Mathf.Sign(target.position.x - transform.position.x);
        velocity = new Vector2(dir * speedX, 0f);

        if (Mathf.Abs(transform.position.x - target.position.x) < 0.05f)
        {
            velocity = Vector2.zero;
            movingToRight = !movingToRight;
            StartCoroutine(EdgePause());
        }

        if (sr) sr.flipX = (facing < 0);
    }

    IEnumerator EdgePause()
    {
        float prev = speedX; speedX = 0f;
        yield return new WaitForSeconds(waitAtEnds);
        speedX = prev;
    }

    bool PlayerDetected()
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(0.1f * facing, 0f);
        Vector2 dir = new Vector2(facing, 0f);
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, detectionRangeY, playerLayer);
        Debug.DrawRay(origin, dir * detectionRangeY, Color.red, 0.05f);
        return hit.collider != null && hit.collider.GetComponent<PlayerMovement>() != null;
    }

    IEnumerator ChargeRoutine()
    {
        state = State.Charge;

        // NEW: pre-charge pause (wind-up/telegraph)
        velocity = Vector2.zero;
        IsCharging = false;
        yield return new WaitForSeconds(preChargePause);

        // begin charge
        IsCharging = true;
        velocity = new Vector2(facing * chargeSpeedZ, 0f);

        float traveled = 0f;
        while (traveled < chargeDistance && state == State.Charge)
        {
            traveled += Mathf.Abs(velocity.x) * Time.deltaTime;
            yield return null;
        }

        // end charge
        IsCharging = false;
        velocity = Vector2.zero;
        yield return new WaitForSeconds(postChargePause);

        state = WithinPatrolZone(transform.position.x) ? State.Patrol : State.Return;
    }

    void ReturnLogic()
    {
        float dir = Mathf.Sign(startPos.x - transform.position.x);
        facing = dir >= 0 ? 1 : -1;
        velocity = new Vector2(dir * speedX, 0f);

        if (Mathf.Abs(transform.position.x - startPos.x) < 0.05f)
        {
            transform.position = new Vector3(startPos.x, transform.position.y, transform.position.z);
            velocity = Vector2.zero;
            if (leftPoint && rightPoint)
                movingToRight = Mathf.Abs(rightPoint.position.x - startPos.x) >= Mathf.Abs(startPos.x - leftPoint.position.x);
            state = State.Patrol;
        }

        if (sr) sr.flipX = (facing < 0);
    }

    bool WithinPatrolZone(float x)
    {
        if (!leftPoint || !rightPoint) return true;
        float min = Mathf.Min(leftPoint.position.x, rightPoint.position.x);
        float max = Mathf.Max(leftPoint.position.x, rightPoint.position.x);
        return x >= min && x <= max;
    }

    public void Flatten()
    {
        if (state == State.Carried) return;
        state = State.Flattened;
        velocity = Vector2.zero;

        if (flattenedSprite) sr.sprite = flattenedSprite;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Dynamic;
        col.isTrigger = false;
        IsCharging = false;
    }

    // ICarryable
    public void PickUp(Transform holder)
    {
        if (canBeCarriedOnlyWhenFlattened && !IsFlattened) return;

        Transform anchor = holder.Find("CarryAnchor");
        if (anchor == null) anchor = holder;

        IsHeld = true;
        state = State.Carried;
        velocity = Vector2.zero;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.isTrigger = true;

        transform.SetParent(anchor);
        transform.localPosition = holdLocalOffset;

        var holderSR = holder.GetComponentInChildren<SpriteRenderer>();
        if (sr && holderSR)
        {
            sr.sortingLayerID = holderSR.sortingLayerID;
            carriedOrder = holderSR.sortingOrder + 1;
            sr.sortingOrder = carriedOrder;
        }
    }

    public void Drop()
    {
        if (!IsHeld) return;

        IsHeld = false;
        transform.SetParent(null);

        rb.bodyType = RigidbodyType2D.Dynamic;
        col.isTrigger = false;

        state = State.Flattened;
        velocity = Vector2.zero;

        if (sr) sr.sortingOrder = originalOrder;
    }

    // Break cage on charge impact, fail player on touch when hostile
    void OnCollisionEnter2D(Collision2D c)
    {
        // Breakable cage logic
        if (IsCharging && c.collider.TryGetComponent<BreakableCage>(out var cage))
        {
            float impulse = c.relativeVelocity.magnitude;
            if (impulse >= chargeBreakImpulse)
            {
                cage.Break();
            }
        }

        // Player fail (only when not flattened)
        if (!IsFlattened && failOnTouchPlayer && c.collider.GetComponent<PlayerMovement>() != null)
        {
            Debug.LogError("Player failed: hit by small monster!");
            // TODO: call your fail/respawn
        }
    }

    void OnDrawGizmosSelected()
    {
        if (leftPoint && rightPoint)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(new Vector3(leftPoint.position.x, transform.position.y, 0f),
                            new Vector3(rightPoint.position.x, transform.position.y, 0f));
            Gizmos.DrawSphere(leftPoint.position, 0.07f);
            Gizmos.DrawSphere(rightPoint.position, 0.07f);
        }

        Gizmos.color = Color.red;
        int dir = (Application.isPlaying ? facing : 1);
        Vector3 origin = transform.position + new Vector3(0.1f * dir, 0f, 0f);
        Gizmos.DrawLine(origin, origin + new Vector3(detectionRangeY * dir, 0f, 0f));
    }
}
