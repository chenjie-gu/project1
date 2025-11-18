using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class SmallMonster : MonoBehaviour, ICarryable
{
    public enum State { Patrol, Charge, Return, Flattened, Carried }

    // ---------- Ground / Physics ----------
    [Header("Ground Check")]
    public Transform groundCheck;              // child under feet
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;              // include platforms/tiles
    public float gravityScaleWhenAlive = 3f;   // >0 to avoid hovering
    public bool requireGroundedToCharge = false;

    // ---------- Patrol ----------
    [Header("Patrol")]
    public Transform leftPoint;
    public Transform rightPoint;
    public float speedX = 2f;
    public float waitAtEnds = 0.15f;

    // ---------- Detection / Charge ----------
    [Header("Detect & Charge")]
    public float detectionRangeY = 4f;
    public LayerMask playerLayer;
    public float preChargePause = 0.35f;     // pause before charge
    public float chargeSpeedZ = 5f;          // horizontal charge speed
    public float chargeDistance = 6f;        // > detection range
    public float postChargePause = 0.15f;
    public float chargeBreakImpulse = 4.5f;  // threshold to break cages

    // ---------- Player interaction (safety on top) ----------
    [Header("Player Interaction")]
    [Tooltip("Allow the player to stand/jump on top without dying.")]
    public bool allowStandOnTop = true;
    [Tooltip("Vertical tolerance for 'on top' test (world units).")]
    public float topTolerance = 0.08f;
    [Tooltip("Optional upward boost when player lands on top (0 = none).")]
    public float stompBounce = 0f;

    // ---------- Flatten / Carry ----------
    [Header("Flatten (by hammer)")]
    public Sprite flattenedSprite;
    public bool canBeCarriedOnlyWhenFlattened = true;

    [Header("Carry Setup")]
    public Vector2 holdLocalOffset = new Vector2(0f, 1.2f);

    // ---------- Fail ----------
    [Header("Fail / Feedback")]
    public bool failOnTouchPlayer = true;

    // ---------- Runtime ----------
    State state = State.Patrol;
    Rigidbody2D rb;
    Collider2D col;
    SpriteRenderer sr;

    Vector2 startPos;
    bool movingToRight = true;
    int facing = 1;
    Vector2 desiredVel;              // write only X into rb.velocity

    Sprite normalSprite;

    // Sorting while carried
    int originalOrder, carriedOrder;

    // ICarryable
    public bool IsHeld { get; private set; } = false;
    public bool IsFlattened => state == State.Flattened || state == State.Carried;

    // Flags
    public bool IsCharging { get; private set; } = false;
    bool isGrounded_SM;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        normalSprite = sr.sprite;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = gravityScaleWhenAlive;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        startPos = transform.position;
        originalOrder = sr ? sr.sortingOrder : 0;

        if (!leftPoint || !rightPoint)
            Debug.LogWarning("SmallMonster: assign leftPoint and rightPoint.");
        if (!groundCheck)
            Debug.LogWarning("SmallMonster: assign groundCheck under the feet.");
    }

    void Update()
    {
        if (state == State.Carried) return;

        switch (state)
        {
            case State.Patrol:
                PatrolLogic();
                if ((!requireGroundedToCharge || isGrounded_SM) && PlayerDetected())
                    StartCoroutine(ChargeRoutine());
                break;

            case State.Return:
                ReturnLogic();
                break;

            case State.Flattened:
                desiredVel.x = 0f;
                break;

            case State.Charge:
                // handled by coroutine
                break;
        }
    }

    void FixedUpdate()
    {
        // Ground check
        if (groundCheck)
            isGrounded_SM = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (state == State.Carried) return;

        // Drive ONLY X so gravity & spike impulses affect Y
        rb.linearVelocity = new Vector2(desiredVel.x, rb.linearVelocity.y);
    }

    // ---------- Patrol ----------
    void PatrolLogic()
    {
        if (!leftPoint || !rightPoint) { desiredVel.x = 0f; return; }

        Transform target = movingToRight ? rightPoint : leftPoint;
        facing = movingToRight ? 1 : -1;

        float dir = Mathf.Sign(target.position.x - transform.position.x);
        desiredVel.x = dir * speedX;

        // arrived?
        if (Mathf.Abs(transform.position.x - target.position.x) < 0.05f)
        {
            desiredVel.x = 0f;
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

        // wind-up telegraph
        desiredVel.x = 0f;
        IsCharging = false;
        yield return new WaitForSeconds(preChargePause);

        // begin charge (horizontal)
        IsCharging = true;
        desiredVel.x = facing * chargeSpeedZ;

        float traveled = 0f;
        while (traveled < chargeDistance && state == State.Charge)
        {
            traveled += Mathf.Abs(desiredVel.x) * Time.deltaTime;
            yield return null;
        }

        // end charge
        IsCharging = false;
        desiredVel.x = 0f;
        yield return new WaitForSeconds(postChargePause);

        state = WithinPatrolZone(transform.position.x) ? State.Patrol : State.Return;
    }

    void ReturnLogic()
    {
        float dir = Mathf.Sign(startPos.x - transform.position.x);
        facing = dir >= 0 ? 1 : -1;
        desiredVel.x = dir * speedX;

        if (Mathf.Abs(transform.position.x - startPos.x) < 0.05f)
        {
            transform.position = new Vector3(startPos.x, transform.position.y, transform.position.z);
            desiredVel.x = 0f;

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

    // ---------- Flatten by Hammer ----------
    public void Flatten()
    {
        if (state == State.Carried) return;
        state = State.Flattened;
        desiredVel.x = 0f;

        if (flattenedSprite) sr.sprite = flattenedSprite;

        rb.gravityScale = gravityScaleWhenAlive;
        col.isTrigger = false;
        IsCharging = false;
    }

    // ---------- ICarryable ----------
    public void PickUp(Transform holder)
    {
        if (canBeCarriedOnlyWhenFlattened && !IsFlattened) return;

        Transform anchor = holder.Find("CarryAnchor");
        if (anchor == null) anchor = holder;

        IsHeld = true;
        state = State.Carried;
        desiredVel.x = 0f;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        col.isTrigger = true;

        transform.SetParent(anchor);
        transform.localPosition = holdLocalOffset;

        // render above the player
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
        rb.gravityScale = gravityScaleWhenAlive;
        col.isTrigger = false;

        state = State.Flattened;  // remains non-hostile
        desiredVel.x = 0f;

        if (sr) sr.sortingOrder = originalOrder;
    }

    // ---------- Player safety on top / cage break / fail ----------
    // Is the player standing on top of the monster?
    bool IsPlayerOnTop(PlayerMovement player)
    {
        if (!allowStandOnTop || player == null || col == null) return false;
        var pCol = player.GetComponent<Collider2D>();
        if (pCol == null) return false;

        Bounds pb = pCol.bounds;
        Bounds mb = col.bounds;
        return pb.min.y >= (mb.max.y - topTolerance);
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        // Cage break during charge
        var cage = c.collider.GetComponent<BreakableCage>()
               ?? c.collider.GetComponentInParent<BreakableCage>()
               ?? c.collider.GetComponentInChildren<BreakableCage>();

        if (IsCharging && cage != null && !c.collider.isTrigger)
        {
            float rel = c.relativeVelocity.magnitude;
            float speed = Mathf.Abs(rb.linearVelocity.x);
            float measured = Mathf.Max(rel, speed);
            if (measured >= chargeBreakImpulse)
                cage.Break();
        }

        // Player interaction
        var player = c.collider.GetComponent<PlayerMovement>();
        if (player != null)
        {
            // never hostile when flattened or carried
            if (IsFlattened || state == State.Carried) return;

            // allow platform use / stomp from top
            if (IsPlayerOnTop(player))
            {
                if (stompBounce > 0f)
                {
                    var prb = player.GetComponent<Rigidbody2D>();
                    if (prb) prb.linearVelocity = new Vector2(prb.linearVelocity.x, Mathf.Max(prb.linearVelocity.y, stompBounce));
                }
                return; // no fail
            }

            // side/front contact is hostile
            if (failOnTouchPlayer)
            {
                Debug.LogError("Player failed: hit by small monster (side/front).");
                // TODO: call your game-over/respawn logic
            }
        }
    }

    void OnCollisionStay2D(Collision2D c)
    {
        var player = c.collider.GetComponent<PlayerMovement>();
        if (player == null) return;

        if (IsFlattened || state == State.Carried) return;

        if (IsPlayerOnTop(player))
            return; // safe while standing

        if (failOnTouchPlayer)
        {
            Debug.LogError("Player failed: sustained contact with small monster (side/front).");
            // TODO: game-over hook
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

        // ground check gizmo
        if (groundCheck)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
