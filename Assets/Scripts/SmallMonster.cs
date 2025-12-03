using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class SmallMonster : MonoBehaviour, ICarryable
{
    // 新增了 StunnedAirborne (滞空) 和 Dead (卡在尖刺上) 状态
    public enum State { Patrol, Charge, Return, Flattened, Carried, StunnedAirborne, Dead }

    // ---------- Ground / Physics ----------
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    public float gravityScaleWhenAlive = 3f;
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
    public float preChargePause = 0.35f;
    public float chargeSpeedZ = 5f;
    public float chargeDistance = 6f;
    public float postChargePause = 0.15f;
    public float chargeBreakImpulse = 4.5f;

    // ---------- Player Interaction ----------
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
    public State state = State.Patrol; // Public for debugging
    Rigidbody2D rb;
    Collider2D col;
    SpriteRenderer sr;

    Vector2 startPos;
    bool movingToRight = true;
    int facing = 1;
    Vector2 desiredVel;

    Sprite normalSprite;
    int originalOrder, carriedOrder;

    // ICarryable
    public bool IsHeld { get; private set; } = false;
    public bool IsFlattened => state == State.Flattened || state == State.Carried;
    public bool IsCharging { get; private set; } = false;

    bool isGrounded_SM;

    // --- 新增变量 ---
    bool _hasBouncedOnce = false;      // 记录是否已经弹过一次
    float _ignoreGroundCheckUntil = 0f;// 防止起跳瞬间被判定为落地

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

        // 确保朝向更新：如果有速度，就朝向速度方向（解决出生时朝向错误的问题）
        if (Mathf.Abs(desiredVel.x) > 0.1f)
        {
            facing = (int)Mathf.Sign(desiredVel.x);
            if (sr) sr.flipX = (facing < 0);
        }

        switch (state)
        {
            case State.Patrol:
                PatrolLogic();
                // 只有在巡逻时才检测玩家
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

            case State.StunnedAirborne:
                desiredVel.x = 0f; // 空中禁止水平移动

                // 落地检测：缓冲期过后 + 接地 + 垂直速度向下或静止
                if (Time.time > _ignoreGroundCheckUntil && isGrounded_SM && rb.linearVelocity.y <= 0.1f)
                {
                    // 落地后恢复巡逻 (或者你可以改成 State.Dead 让它落地就死)
                    state = State.Patrol;
                }
                break;

            case State.Dead:
                desiredVel.x = 0f; // 死在尖刺上，完全不动
                break;
        }
    }

    void FixedUpdate()
    {
        // Ground check
        if (groundCheck)
            isGrounded_SM = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (state == State.Carried || state == State.Dead) return;

        // Drive ONLY X so gravity & spike impulses affect Y
        // Dead 状态下我们可能会设为 Kinematic，所以不需要这行
        if (rb.bodyType == RigidbodyType2D.Dynamic)
        {
            rb.linearVelocity = new Vector2(desiredVel.x, rb.linearVelocity.y);
        }
    }

    // ---------- Spike Interaction (New) ----------

    // 由 BounceSpikes2D.cs 调用
    public void OnSpikeLaunch()
    {
        // 如果被拿着，先强制掉落
        if (IsHeld) Drop();

        // 1. 如果这是第二次碰到尖刺（已经弹过一次了）
        if (_hasBouncedOnce)
        {
            // 立即停止所有运动
            rb.linearVelocity = Vector2.zero;

            // 设为 Kinematic 让它不再受重力或弹力影响，死死粘在尖刺上
            rb.bodyType = RigidbodyType2D.Kinematic;

            // 确保碰撞体还在（作为平台），但可能需要关闭 Trigger 如果你有特殊需求
            // 这里保持原样，它就是一个固体方块

            state = State.Dead;
            StopAllCoroutines();
            return;
        }

        // 2. 如果是第一次碰到尖刺 -> 起飞
        _hasBouncedOnce = true;
        state = State.StunnedAirborne;
        IsCharging = false;
        StopAllCoroutines(); // 停止冲锋等

        // 给予 0.2秒 的落地检测宽限期
        _ignoreGroundCheckUntil = Time.time + 0.2f;
    }

    // ---------- Patrol ----------
    void PatrolLogic()
    {
        if (!leftPoint || !rightPoint) { desiredVel.x = 0f; return; }

        Transform target = movingToRight ? rightPoint : leftPoint;

        float dir = Mathf.Sign(target.position.x - transform.position.x);
        desiredVel.x = dir * speedX;

        // arrived?
        if (Mathf.Abs(transform.position.x - target.position.x) < 0.05f)
        {
            desiredVel.x = 0f;
            movingToRight = !movingToRight;
            StartCoroutine(EdgePause());
        }
    }

    IEnumerator EdgePause()
    {
        float prev = speedX; speedX = 0f;
        yield return new WaitForSeconds(waitAtEnds);
        speedX = prev;
    }

    bool PlayerDetected()
    {
        // 修正后的射线检测逻辑
        Vector2 origin = (Vector2)transform.position + new Vector2(0.1f * facing, 0f);
        Vector2 dir = new Vector2(facing, 0f);
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, detectionRangeY, playerLayer);
        Debug.DrawRay(origin, dir * detectionRangeY, Color.red, 0.05f);

        // 只有打中且带有 PlayerMovement 组件才算
        return hit.collider != null && hit.collider.GetComponent<PlayerMovement>() != null;
    }

    IEnumerator ChargeRoutine()
    {
        state = State.Charge;

        // wind-up
        desiredVel.x = 0f;
        IsCharging = false;
        yield return new WaitForSeconds(preChargePause);

        // charge
        IsCharging = true;
        desiredVel.x = facing * chargeSpeedZ;

        float traveled = 0f;
        while (traveled < chargeDistance && state == State.Charge)
        {
            traveled += Mathf.Abs(desiredVel.x) * Time.deltaTime;
            yield return null;
        }

        // end
        IsCharging = false;
        desiredVel.x = 0f;
        yield return new WaitForSeconds(postChargePause);

        state = WithinPatrolZone(transform.position.x) ? State.Patrol : State.Return;
    }

    void ReturnLogic()
    {
        float dir = Mathf.Sign(startPos.x - transform.position.x);
        desiredVel.x = dir * speedX;

        if (Mathf.Abs(transform.position.x - startPos.x) < 0.05f)
        {
            transform.position = new Vector3(startPos.x, transform.position.y, transform.position.z);
            desiredVel.x = 0f;

            if (leftPoint && rightPoint)
                movingToRight = Mathf.Abs(rightPoint.position.x - startPos.x) >= Mathf.Abs(startPos.x - leftPoint.position.x);

            state = State.Patrol;
        }
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
        if (state == State.Carried || state == State.Dead) return; // 死掉后或者被抓着不能再被砸
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
        if (state == State.Dead) return; // 死了（粘在尖刺上）就不能被拔出来了

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

        // Drop 后默认为 Flattened 状态
        state = State.Flattened;
        desiredVel.x = 0f;

        if (sr) sr.sortingOrder = originalOrder;
    }

    // ---------- Collision / Safety / Fail ----------
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
        // 1. 滞空或死亡状态下，对所有碰撞无害化
        if (state == State.StunnedAirborne || state == State.Dead) return;

        // Cage break
        var cage = c.collider.GetComponent<BreakableCage>()
                ?? c.collider.GetComponentInParent<BreakableCage>()
                ?? c.collider.GetComponentInChildren<BreakableCage>();

        if (IsCharging && cage != null && !c.collider.isTrigger)
        {
            float rel = c.relativeVelocity.magnitude;
            float speed = Mathf.Abs(rb.linearVelocity.x);
            if (Mathf.Max(rel, speed) >= chargeBreakImpulse)
                cage.Break();
        }

        // Player interaction
        var player = c.collider.GetComponent<PlayerMovement>();
        if (player != null)
        {
            if (IsFlattened || state == State.Carried) return;

            if (IsPlayerOnTop(player))
            {
                if (stompBounce > 0f)
                {
                    var prb = player.GetComponent<Rigidbody2D>();
                    if (prb) prb.linearVelocity = new Vector2(prb.linearVelocity.x, Mathf.Max(prb.linearVelocity.y, stompBounce));
                }
                return;
            }

            if (failOnTouchPlayer)
            {
                Debug.LogError("Player failed: hit by small monster.");
                // TODO: Call Game Over
            }
        }
    }

    void OnCollisionStay2D(Collision2D c)
    {
        // 持续接触无害化
        if (state == State.StunnedAirborne || state == State.Dead) return;

        var player = c.collider.GetComponent<PlayerMovement>();
        if (player == null) return;
        if (IsFlattened || state == State.Carried) return;
        if (IsPlayerOnTop(player)) return;

        if (failOnTouchPlayer)
        {
            Debug.LogError("Player failed: sustained contact.");
        }
    }

    // ---------- Gizmos ----------
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

        if (groundCheck)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}