using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class SmallMonster : MonoBehaviour, ICarryable
{
    public enum State { Patrol, Charge, Return, Flattened, Carried }

    [Header("Patrol")]
    public Transform leftPoint;         // set in Inspector
    public Transform rightPoint;        // set in Inspector
    public float speedX = 2f;           // patrol speed
    public float waitAtEnds = 0.15f;    // pause at patrol edges

    [Header("Detect & Charge")]
    public float detectionRangeY = 4f;  // ray length in facing direction
    public LayerMask playerLayer;       // include only the Player layer
    public float chargeSpeedZ = 5f;     // charge speed (Z > X)
    public float chargeDistance = 6f;   // must be > detectionRangeY
    public float postChargePause = 0.15f;

    [Header("Flatten (by hammer)")]
    public Sprite flattenedSprite;      // visual when flattened
    public bool canBeCarriedOnlyWhenFlattened = true;

    [Header("Carry Setup")]
    public Vector2 holdLocalOffset = new Vector2(0f, 1.2f); // relative to CarryAnchor (or player)

    [Header("Fail / Feedback")]
    public bool failOnTouchPlayer = true; // only when not flattened

    // --- runtime ---
    State state = State.Patrol;
    Rigidbody2D rb;
    Collider2D col;
    SpriteRenderer sr;

    Vector2 startPos;
    bool movingToRight = true;
    int facing = 1;              // +1 = right, -1 = left
    Vector2 velocity;

    Sprite normalSprite;

    // Sorting (rendering) order handling when carried
    int originalOrder;
    int carriedOrder;

    // ICarryable
    public bool IsHeld { get; private set; } = false;
    public bool IsFlattened => state == State.Flattened || state == State.Carried;

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

        // capture initial sorting order for restore on drop
        originalOrder = sr != null ? sr.sortingOrder : 0;

        if (leftPoint == null || rightPoint == null)
            Debug.LogWarning("SmallMonster: assign leftPoint and rightPoint for patrol.");
    }

    void Update()
    {
        if (state == State.Carried) return; // carried = no AI

        switch (state)
        {
            case State.Patrol:
                PatrolLogic();
                if (PlayerDetected()) StartCoroutine(ChargeRoutine());
                break;

            case State.Return:
                ReturnLogic();
                break;

            case State.Flattened:
                velocity = Vector2.zero; // stays still
                break;

            case State.Charge:
                // handled by coroutine; keep current velocity
                break;
        }
    }

    void FixedUpdate()
    {
        if (state == State.Carried) return;
        rb.linearVelocity = velocity;
    }

    // ---------- PATROL ----------
    void PatrolLogic()
    {
        if (leftPoint == null || rightPoint == null)
        {
            velocity = Vector2.zero;
            return;
        }

        Transform target = movingToRight ? rightPoint : leftPoint;
        facing = movingToRight ? 1 : -1;

        float dir = Mathf.Sign(target.position.x - transform.position.x);
        velocity = new Vector2(dir * speedX, 0f);

        // arrived?
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
        float prev = speedX;
        speedX = 0f;
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
        velocity = new Vector2(facing * chargeSpeedZ, 0f);

        float traveled = 0f;
        while (traveled < chargeDistance && state == State.Charge)
        {
            traveled += Mathf.Abs(velocity.x) * Time.deltaTime;
            yield return null;
        }

        velocity = Vector2.zero;
        yield return new WaitForSeconds(postChargePause);

        if (WithinPatrolZone(transform.position.x))
            state = State.Patrol;
        else
            state = State.Return;
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

            // choose a patrol direction based on nearest edge
            if (leftPoint && rightPoint)
                movingToRight = Mathf.Abs(rightPoint.position.x - startPos.x) >= Mathf.Abs(startPos.x - leftPoint.position.x);
            state = State.Patrol;
        }

        if (sr) sr.flipX = (facing < 0);
    }

    bool WithinPatrolZone(float x)
    {
        if (leftPoint == null || rightPoint == null) return true;
        float min = Mathf.Min(leftPoint.position.x, rightPoint.position.x);
        float max = Mathf.Max(leftPoint.position.x, rightPoint.position.x);
        return x >= min && x <= max;
    }

    // ---------- FLATTEN (by hammer) ----------
    public void Flatten()
    {
        if (state == State.Carried) return; // already on player
        state = State.Flattened;
        velocity = Vector2.zero;

        if (flattenedSprite) sr.sprite = flattenedSprite;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Dynamic; // can rest on ground
        col.isTrigger = false;                 // solid on ground
    }

    // ---------- ICarryable ----------
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
        col.isTrigger = true; // avoid colliding with player/head

        transform.SetParent(anchor);
        transform.localPosition = holdLocalOffset;

        // — sorting: render just above the player while carried —
        if (sr != null)
        {
            var holderSR = holder.GetComponentInChildren<SpriteRenderer>();
            if (holderSR)
            {
                sr.sortingLayerID = holderSR.sortingLayerID;
                carriedOrder = holderSR.sortingOrder + 1;
                sr.sortingOrder = carriedOrder;
            }
        }
    }

    public void Drop()
    {
        if (!IsHeld) return;

        IsHeld = false;
        transform.SetParent(null);

        rb.bodyType = RigidbodyType2D.Dynamic;
        col.isTrigger = false;

        // remain flattened & non-hostile after drop
        state = State.Flattened;
        velocity = Vector2.zero;

        // — restore original sorting order on the ground —
        if (sr != null)
            sr.sortingOrder = originalOrder;
    }

    // ---------- Fail on player contact (when not flattened) ----------
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsFlattened && failOnTouchPlayer && collision.collider.GetComponent<PlayerMovement>() != null)
        {
            Debug.LogError("Player failed: hit by small monster!");
            // TODO: call your game over / respawn logic here
        }
    }

    // ---------- Gizmos ----------
    void OnDrawGizmosSelected()
    {
        // patrol edges
        if (leftPoint && rightPoint)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(new Vector3(leftPoint.position.x, transform.position.y, 0f),
                            new Vector3(rightPoint.position.x, transform.position.y, 0f));
            Gizmos.DrawSphere(leftPoint.position, 0.07f);
            Gizmos.DrawSphere(rightPoint.position, 0.07f);
        }

        // detection ray
        Gizmos.color = Color.red;
        int dir = (Application.isPlaying ? facing : 1);
        Vector3 origin = transform.position + new Vector3(0.1f * dir, 0f, 0f);
        Gizmos.DrawLine(origin, origin + new Vector3(detectionRangeY * dir, 0f, 0f));
    }
}
