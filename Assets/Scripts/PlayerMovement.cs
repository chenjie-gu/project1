using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Ground Check")]
    public Transform groundCheck;
    public float checkRadius = 0.25f;
    public LayerMask groundLayer;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 10f;

    [Header("Boundaries")]
    public float leftBoundary = -10f;
    public float rightBoundary = 10f;
    public float bottomBoundary = -5f;
    public float topBoundary = 5f;
    public bool enforceBoundaries = true;

    [Header("Carry")]
    public float pickupRadius = 1.0f;
    private ICarryable carried;

    [Header("Flattened State")]
    public bool isFlattened = false;
    public float flattenedJumpForce = 5f;

    [Header("Flatten Control")]
    public bool flattenLocked = false;   // if true, player stays flattened

    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;    // auto-grab if left null
    public Sprite normalSprite;              // set to your cat sprite
    public Sprite flattenedSprite;           // set to your “squashed cat” sprite
    public bool pauseAnimatorWhileFlattened = true;

    // --- private ---
    Rigidbody2D rb;
    Collider2D col;
    Animator animator;

    float moveInput;
    bool isGrounded;
    KeyCode jumpKey = KeyCode.Space;
    KeyCode interactKey = KeyCode.E;
    bool jumpRequested;
    bool blockedByKey = false;

    Vector3 originalScale;
    Vector2 originalColliderSize;
    Vector2 originalColliderOffset;

    Coroutine flattenRoutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        spriteRenderer = spriteRenderer ?? GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // cache original visuals/shape
        originalScale = transform.localScale;
        if (spriteRenderer != null && normalSprite == null)
            normalSprite = spriteRenderer.sprite;

        if (col is BoxCollider2D box)
        {
            originalColliderSize = box.size;
            originalColliderOffset = box.offset;
        }
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(jumpKey) && isGrounded)
            jumpRequested = true;

        if (Input.GetKeyDown(interactKey))
        {
            // Prefer door interaction
            float currentPickupRadius = isFlattened ? pickupRadius * 0.7f : pickupRadius;
            var hits = Physics2D.OverlapCircleAll(transform.position, currentPickupRadius);

            foreach (var h in hits)
            {
                var door = h.GetComponent<Door>();
                if (door != null && door.TryUseKey(this))
                    return;
            }

            // Pick up / drop
            if (carried != null)
            {
                try { carried.Drop(); }
                catch (MissingReferenceException) { /* destroyed */ }
                carried = null;
            }
            else
            {
                foreach (var h in hits)
                {
                    var c = h.GetComponent<ICarryable>();
                    if (c != null && !c.IsHeld)
                    {
                        c.PickUp(transform);
                        carried = c;
                        break;
                    }
                }
            }
        }
    }

    void FixedUpdate()
    {
        // Ground check
        if (groundCheck != null)
        {
            if (isFlattened)
            {
                float scaledHeight = originalScale.y * 0.5f;
                Vector3 groundCheckPos = new Vector3(transform.position.x, transform.position.y - scaledHeight, transform.position.z);
                isGrounded = Physics2D.OverlapCircle(groundCheckPos, checkRadius, groundLayer);
            }
            else
            {
                isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
            }
        }

        // Horizontal movement
        Vector2 newVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        if (blockedByKey)
        {
            if (moveInput != 0) newVelocity.x = 0;
            if (rb.linearVelocity.y > 0) newVelocity.y = 0;
        }

        // Horizontal boundaries
        if (enforceBoundaries)
        {
            float halfWidth = originalColliderSize.x * 0.5f;
            float nextX = transform.position.x + newVelocity.x * Time.fixedDeltaTime;

            if (newVelocity.x < 0 && nextX <= leftBoundary + halfWidth)
            {
                newVelocity.x = 0;
                if (transform.position.x < leftBoundary + halfWidth)
                    transform.position = new Vector3(leftBoundary + halfWidth, transform.position.y, transform.position.z);
            }
            else if (newVelocity.x > 0 && nextX >= rightBoundary - halfWidth)
            {
                newVelocity.x = 0;
                if (transform.position.x > rightBoundary - halfWidth)
                    transform.position = new Vector3(rightBoundary - halfWidth, transform.position.y, transform.position.z);
            }
        }

        rb.linearVelocity = newVelocity;

        // Jump
        if (jumpRequested)
        {
            float jf = isFlattened ? flattenedJumpForce : jumpForce;
            if (jf > 0f) rb.linearVelocity = new Vector2(rb.linearVelocity.x, jf);
            jumpRequested = false;
        }

        // Vertical boundaries
        float halfHeight = col.bounds.size.y * 0.5f;
        float y = transform.position.y;

        if (y - halfHeight < bottomBoundary)
        {
            transform.position = new Vector3(transform.position.x, bottomBoundary + halfHeight, transform.position.z);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }
        else if (y + halfHeight > topBoundary)
        {
            transform.position = new Vector3(transform.position.x, topBoundary - halfHeight, transform.position.z);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }
    }

    // === Flattening ===
    public void SetFlattened(bool flattened)
    {
    // Don’t allow unflatten while locked
        if (!flattened && flattenLocked) return;

        if (isFlattened == flattened) return;
        isFlattened = flattened;

        if (flattened)
        {
            if (spriteRenderer != null && flattenedSprite != null)
                spriteRenderer.sprite = flattenedSprite;
            if (pauseAnimatorWhileFlattened && animator != null)
                animator.enabled = false;

            Vector3 flattenedScale = new Vector3(originalScale.x * 1.3f, originalScale.y * 0.5f, originalScale.z);
            transform.localScale = flattenedScale;

            float heightDiff = (originalScale.y - flattenedScale.y) * 0.5f;
            transform.position = new Vector3(transform.position.x, transform.position.y - heightDiff, transform.position.z);
        }
        else
        {
            if (spriteRenderer != null && normalSprite != null)
                spriteRenderer.sprite = normalSprite;
            if (pauseAnimatorWhileFlattened && animator != null)
                animator.enabled = true;

            if (col is BoxCollider2D box)
            {
                box.size = originalColliderSize;
                box.offset = originalColliderOffset;
            }
            transform.localScale = originalScale;
        }
    }

    public void FlattenPermanently()
    {
        if (flattenRoutine != null) { StopCoroutine(flattenRoutine); flattenRoutine = null; }
        flattenLocked = true;
        SetFlattened(true);
    }

    public void ForceUnflatten()
    {
        flattenLocked = false;
        SetFlattened(false);
    }


    // === Carry helpers for Level 4 ===
    public bool IsCarryingSmallMonster()
    {
        return carried is SmallMonster; // SmallMonster implements ICarryable
    }

    // Doors
    public bool TryConsumeOneKey()
    {
        if (carried is Key k)
        {
            k.Drop();
            Destroy(k.gameObject);
            carried = null;
            return true;
        }
        return false;
    }

    public Key GetCarriedKey() => carried as Key;
    public void SetCarriedKey(Key key) => carried = key;

    // Gizmos
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (groundCheck != null) Gizmos.DrawWireSphere(groundCheck.position, checkRadius);

        Gizmos.color = Color.yellow;
        float gizmoPickupRadius = isFlattened ? pickupRadius * 0.75f : pickupRadius;
        Gizmos.DrawWireSphere(transform.position, gizmoPickupRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(leftBoundary, bottomBoundary, 0), new Vector3(leftBoundary, topBoundary, 0));
        Gizmos.DrawLine(new Vector3(rightBoundary, bottomBoundary, 0), new Vector3(rightBoundary, topBoundary, 0));
        Gizmos.DrawLine(new Vector3(leftBoundary, bottomBoundary, 0), new Vector3(rightBoundary, bottomBoundary, 0));
        Gizmos.DrawLine(new Vector3(leftBoundary, topBoundary, 0), new Vector3(rightBoundary, topBoundary, 0));
    }

    public void SetBlockedByKey(bool blocked) => blockedByKey = blocked;
    public bool IsGrounded() => isGrounded;
}
