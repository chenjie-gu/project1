using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class SmallMonster : MonoBehaviour, ICarryable
{
    // 新增了 StunnedAirborne (滞空) 和 Dead (卡在尖刺上/撞死) 状态
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
                desiredVel.x = 0f; // 死在尖刺上或撞死，完全不动
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

            // 确保碰撞体还在（作为平台）
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
        // 修改点 1: 定义什么是“可被抓取”的状态
        // 现在允许：压扁状态 (Flattened) 或者 死亡状态 (Dead)
        bool isPickable = IsFlattened || state == State.Dead;

        // 如果设置了“只能在压扁时抓取”，但当前既不是压扁也不是死亡，就不让抓
        // (也就是说，活蹦乱跳巡逻的时候还是不能抓，但撞死或者被砸扁后可以抓)
        if (canBeCarriedOnlyWhenFlattened && !isPickable) return;

        // 修改点 2: 删除了之前 "if (state == State.Dead) return;" 的限制
        // 现在的逻辑是：即使它死在笼子旁或者尖刺上，你也能把它拔出来

        Transform anchor = holder.Find("CarryAnchor");
        if (anchor == null) anchor = holder;

        IsHeld = true;
        state = State.Carried;
        desiredVel.x = 0f;

        // 变成运动学刚体，跟随玩家
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        col.isTrigger = true; // 变成触发器，防止在手里撞来撞去

        transform.SetParent(anchor);
        transform.localPosition = holdLocalOffset;

        // 调整图层顺序，让它显示在玩家前面
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

    // 这个就是导致你报错的方法，我已经把它们合并好了
    void OnCollisionEnter2D(Collision2D c)
    {
        // 1. 如果已经滞空或死亡，忽略碰撞逻辑
        if (state == State.StunnedAirborne || state == State.Dead) return;

        // --- 笼子交互逻辑 ---
        var cage = c.collider.GetComponent<BreakableCage>()
                ?? c.collider.GetComponentInParent<BreakableCage>()
                ?? c.collider.GetComponentInChildren<BreakableCage>();

        // 检查是否撞到了笼子（不需要非得是冲锋状态，只要速度够快或者正在冲锋都可以）
        if (cage != null && !c.collider.isTrigger)
        {
            float rel = c.relativeVelocity.magnitude;
            float speed = Mathf.Abs(rb.linearVelocity.x);

            // 我把阈值稍微降低了，防止因为摩擦力减速导致撞不碎
            // 只要处于冲锋状态，或者速度超过 3，或者相对撞击力超过 2，都算撞碎
            if (IsCharging || speed > 3f || rel >= 2.0f)
            {
                // A. 打破笼子 (传入自己的 collider 以忽略碰撞)
                cage.Break(col);

                // B. 让怪物彻底停下
                rb.linearVelocity = Vector2.zero;
                desiredVel.x = 0f;
                IsCharging = false;
                StopAllCoroutines(); // 停止冲锋协程

                // C. 【关键修改】切换为 Kinematic，像钉子一样钉在原地
                // 这样任何物理力（包括 Key 的弹力）都无法推动它
                rb.bodyType = RigidbodyType2D.Kinematic;

                // 设置为 Dead
                state = State.Dead;

                return; // 结束逻辑
            }
        }

        // --- 玩家交互逻辑 (保持原样) ---
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
                // TODO: Game Over logic
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