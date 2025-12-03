using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BreakableCage : MonoBehaviour
{
    [Header("Contents")]
    public GameObject keyObject;
    public bool disableKeyUntilBroken = true;

    [Header("Visuals")]
    public bool hideCompletelyWhenBroken = true;
    public bool destroyAfterBroken = false;
    public float destroyDelay = 0.25f;

    [Header("Physics")]
    public bool removeColliderWhenBroken = true;
    public float keyDropYOffset = 0.06f;

    BoxCollider2D col;
    SpriteRenderer sr;
    bool isBroken;

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        sr = GetComponent<SpriteRenderer>();

        if (disableKeyUntilBroken && keyObject)
            SetKeyActive(false);
    }

    // --- 修改点 1：接收一个 Collider2D 参数 (breaker) ---
    public void Break(Collider2D breaker = null)
    {
        if (isBroken) return;
        isBroken = true;

        if (hideCompletelyWhenBroken && sr) sr.enabled = false;
        if (removeColliderWhenBroken && col) col.enabled = false;

        if (keyObject)
        {
            keyObject.transform.SetParent(null);
            SetKeyActive(true);

            Vector3 p = keyObject.transform.position;
            float halfY = col ? col.bounds.extents.y : 0.25f;
            keyObject.transform.position = new Vector3(p.x, transform.position.y - halfY - keyDropYOffset, p.z);

            var rb = keyObject.GetComponent<Rigidbody2D>();
            if (!rb) rb = keyObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var keyCol = keyObject.GetComponent<Collider2D>();
            if (keyCol) keyCol.isTrigger = false;

            // 这一行非常重要：告诉物理引擎 Key 和 怪物 (breaker) 互不干扰
            if (breaker != null && keyCol != null)
            {
                Physics2D.IgnoreCollision(keyCol, breaker, true);
            }
        }

        if (destroyAfterBroken) Destroy(gameObject, destroyDelay);
    }

    void SetKeyActive(bool on)
    {
        keyObject.SetActive(on);
    }
}