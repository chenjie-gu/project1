using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Ground Check")]
    public Transform groundCheck;
    public float checkRadius = 0.25f;
    public LayerMask groundLayer;

    [Header("Movement")]
    public float moveSpeed;
    public float jumpForce;

    [Header("Boundaries")]
    public float leftBoundary;
    public float rightBoundary;
    public float bottomBoundary;
    public float topBoundary;
    public bool enforceBoundaries = true;

    [Header("Carry")]
    public float pickupRadius;
    private ICarryable carried;



    [Header("Flattened State")]
    public bool isFlattened = false;
    public float flattenedJumpForce;

    Rigidbody2D rb;
    Collider2D col;
    float moveInput;
    bool isGrounded;
    KeyCode jumpKey = KeyCode.Space;
    KeyCode interactKey = KeyCode.E;
    bool jumpRequested;
    bool blockedByKey = false;
    // Store original values for flattened state
    Vector3 originalScale;
    Vector2 originalColliderSize;
    Vector2 originalColliderOffset;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        
        // Store original values
        originalScale = transform.localScale;
        if (col is BoxCollider2D box)
        {
            originalColliderSize = box.size;
            originalColliderOffset = box.offset;
        }
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(jumpKey))
        {
            if (isGrounded)
            {
                jumpRequested = true;
            }
        }

        if (Input.GetKeyDown(interactKey))
        {
            // First check for doors to use keys
            float currentPickupRadius = isFlattened ? pickupRadius * 0.7f : pickupRadius;
            var hits = Physics2D.OverlapCircleAll(transform.position, currentPickupRadius);
            
            foreach (var h in hits)
            {
                var door = h.GetComponent<Door>();
                if (door != null)
                {
                    if (door.TryUseKey(this))
                    {
                        return;
                    }
                }
            }
            
            // If no door interaction, handle normal pickup/drop logic
            if (carried != null)
            {
                // Check if the carried object still exists before calling Drop()
                try
                {
                    carried.Drop();
                }
                catch (MissingReferenceException)
                {
                    // Object was destroyed, just clear the reference
                }
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

        // horizontal movement
        Vector2 newVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        
        // Check if blocked by key collision
        if (blockedByKey)
        {
            if (moveInput != 0)
            {
                newVelocity.x = 0;
            }
            
            if (rb.linearVelocity.y > 0)
            {
                newVelocity.y = 0;
            }
        }
        
        // enforce horizontal boundaries
        if (enforceBoundaries)
        {
            float halfWidth = originalColliderSize.x * 0.5f;
            float currentX = transform.position.x;
            
            // Check if moving left would go beyond left boundary
            if (newVelocity.x < 0 && currentX + newVelocity.x * Time.fixedDeltaTime <= leftBoundary + halfWidth)
            {
                newVelocity.x = 0;
                // Snap to boundary if already past it
                if (currentX < leftBoundary + halfWidth)
                {
                    transform.position = new Vector3(leftBoundary + halfWidth, transform.position.y, transform.position.z);
                }
            }
            // Check if moving right would go beyond right boundary
            else if (newVelocity.x > 0 && currentX + newVelocity.x * Time.fixedDeltaTime >= rightBoundary - halfWidth)
            {
                newVelocity.x = 0;
                // Snap to boundary if already past it
                if (currentX > rightBoundary - halfWidth)
                {
                    transform.position = new Vector3(rightBoundary - halfWidth, transform.position.y, transform.position.z);
                }
            }
        }
        
        rb.linearVelocity = newVelocity;

        if (jumpRequested)
        {
            float jumpForceToUse = isFlattened ? flattenedJumpForce : jumpForce;
            if (jumpForceToUse > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForceToUse);
            }
            jumpRequested = false;
        }
        
        // enforce vertical boundaries
        float halfHeight = col.bounds.size.y * 0.5f;
        float currentY = transform.position.y;
        
        // Check bottom boundary
        if (currentY - halfHeight < bottomBoundary)
        {
            transform.position = new Vector3(transform.position.x, bottomBoundary + halfHeight, transform.position.z);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }
        // Check top boundary
        else if (currentY + halfHeight > topBoundary)
        {
            transform.position = new Vector3(transform.position.x, topBoundary - halfHeight, transform.position.z);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }
    }

    public void SetFlattened(bool flattened)
    {
        // Prevent multiple flattening calls
        if (isFlattened == flattened) return;
        
        isFlattened = flattened;

        if (flattened)
        {
            Vector3 flattenedScale = new Vector3(originalScale.x * 1.3f, originalScale.y * 0.5f, originalScale.z);
            transform.localScale = flattenedScale;
            
            float scaleFactor = flattenedScale.y / originalScale.y;
            float heightDifference = (originalScale.y - flattenedScale.y) * 0.5f;
            transform.position = new Vector3(transform.position.x, transform.position.y - heightDifference, transform.position.z);
        }
        else
        {
            if (col is BoxCollider2D box)
            {
                box.size = originalColliderSize;
                box.offset = originalColliderOffset;
            }
            
            transform.localScale = originalScale;
        }
    }

    // Door asks to consume one key
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

    // Hammer checks if player is carrying a key
    public Key GetCarriedKey()
    {
        return carried as Key;
    }

    public void SetCarriedKey(Key key)
    {
        carried = key;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (groundCheck != null) Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        Gizmos.color = Color.yellow;
        float gizmoPickupRadius = isFlattened ? pickupRadius * 0.75f : pickupRadius;
        Gizmos.DrawWireSphere(transform.position, gizmoPickupRadius);
        
        // Draw boundary lines
        Gizmos.color = Color.red;
        // Left boundary
        Gizmos.DrawLine(new Vector3(leftBoundary, bottomBoundary, 0), new Vector3(leftBoundary, topBoundary, 0));
        // Right boundary
        Gizmos.DrawLine(new Vector3(rightBoundary, bottomBoundary, 0), new Vector3(rightBoundary, topBoundary, 0));
        // Bottom boundary
        Gizmos.DrawLine(new Vector3(leftBoundary, bottomBoundary, 0), new Vector3(rightBoundary, bottomBoundary, 0));
        // Top boundary
        Gizmos.DrawLine(new Vector3(leftBoundary, topBoundary, 0), new Vector3(rightBoundary, topBoundary, 0));
    }
    
    // Method for key to tell player it's blocked
    public void SetBlockedByKey(bool blocked)
    {
        blockedByKey = blocked;
    }
    
    // Method for hammer to check if player is grounded
    public bool IsGrounded()
    {
        return isGrounded;
    }
}
