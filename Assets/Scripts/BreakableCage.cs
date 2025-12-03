using UnityEngine;

/// Blocks the player and contains a Key (or any ICarryable).
/// Becomes invisible/non-solid when broken; releases the key with real physics.
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BreakableCage : MonoBehaviour
{
    [Header("Contents")]
    public GameObject keyObject;                 // assign the Key object (can be a child)
    public bool disableKeyUntilBroken = true;    // hide/disable key until cage breaks

    [Header("Visuals")]
    public bool hideCompletelyWhenBroken = true; // turn the renderer off (invisible)
    public bool destroyAfterBroken = false;      // optional cleanup after a short delay
    public float destroyDelay = 0.25f;

    [Header("Physics")]
    public bool removeColliderWhenBroken = true; // disables collider so it's non-solid
    public float keyDropYOffset = 0.06f;         // small nudge so key rests on ground

    BoxCollider2D col;
    SpriteRenderer sr;
    bool isBroken;

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        sr  = GetComponent<SpriteRenderer>();

        if (disableKeyUntilBroken && keyObject)
            SetKeyActive(false);
    }

    /// <summary>Call this from SmallMonster (already happens in your charge collision).</summary>
    public void Break()
    {
        if (isBroken) return;
        isBroken = true;

        // Make the cage invisible and non-solid
        if (hideCompletelyWhenBroken && sr) sr.enabled = false;
        if (removeColliderWhenBroken && col) col.enabled = false;

        // Release the key with proper physics
        if (keyObject)
        {
            // If the key is a child, unparent first so it’s free
            keyObject.transform.SetParent(null);
            SetKeyActive(true);

            // Put it just below the cage so it doesn’t overlap and get ejected
            Vector3 p = keyObject.transform.position;
            float halfY = col ? col.bounds.extents.y : 0.25f;
            keyObject.transform.position = new Vector3(p.x, transform.position.y - halfY - keyDropYOffset, p.z);

            // Ensure proper physics state
            var rb = keyObject.GetComponent<Rigidbody2D>();
            if (!rb) rb = keyObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var keyCol = keyObject.GetComponent<Collider2D>();
            if (keyCol) keyCol.isTrigger = false;
        }

        if (destroyAfterBroken) Destroy(gameObject, destroyDelay);
    }

    void SetKeyActive(bool on)
    {
        keyObject.SetActive(on);
    }
}
