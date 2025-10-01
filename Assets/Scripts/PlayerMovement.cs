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
    
    [Header("Wall Sliding")]
    public bool enableWallSliding = true;
    public float wallSlideSpeed = 3f;

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
    public Sprite flattenedSprite; // Drag your flattened sprite here
    public Collider2D flattenedCollider; // Drag a separate collider for flattened state

    Rigidbody2D rb;
    Collider2D col;
    SpriteRenderer spriteRenderer;
    Sprite originalSprite;
    float moveInput;
    bool isGrounded;
    KeyCode jumpKey = KeyCode.Space;
    KeyCode interactKey = KeyCode.E;
    bool jumpRequested;
    bool isTouchingWall;
    bool isTouchingLeftWall;
    bool isTouchingRightWall;
    bool isWallSliding;

    // Store original values for flattened state
    Vector3 originalScale;
    
    // Collider management
    private Collider2D activeCollider;
    
    

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        
        originalScale = transform.localScale;
        if (spriteRenderer != null)
        {
            originalSprite = spriteRenderer.sprite;
        }
        
        activeCollider = col;
        if (flattenedCollider != null)
        {
            flattenedCollider.enabled = false;
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
                // Use the active collider's bounds for ground check
                float colliderBottom = activeCollider.bounds.min.y;
                Vector3 groundCheckPos = new Vector3(transform.position.x, colliderBottom, transform.position.z);
                isGrounded = Physics2D.OverlapCircle(groundCheckPos, checkRadius, groundLayer);
            }
            else
            {
                isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
            }
        }
        

        // Check for wall contact
        CheckWallContact();
        
        // horizontal movement
        Vector2 newVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        
        // Apply wall sliding only when pressing movement key in same direction as wall
        if (enableWallSliding && !isGrounded)
        {
            // Check if player is pressing left and touching left wall
            if (isTouchingLeftWall && moveInput < 0)
            {
                newVelocity.y = -wallSlideSpeed;
                newVelocity.x = 0;
            }
            // Check if player is pressing right and touching right wall
            else if (isTouchingRightWall && moveInput > 0)
            {
                newVelocity.y = -wallSlideSpeed;
                newVelocity.x = 0;
            }
            // Allow movement away from walls
            else if (isTouchingLeftWall && moveInput > 0)
            {
                newVelocity.x = moveInput * moveSpeed;
            }
            else if (isTouchingRightWall && moveInput < 0)
            {
                newVelocity.x = moveInput * moveSpeed;
            }
        }
        
        // Platform detection is now handled directly in the platform movement section
        
        // enforce horizontal boundaries
        if (enforceBoundaries)
        {
            float colliderLeft = activeCollider.bounds.min.x;
            float colliderRight = activeCollider.bounds.max.x;
            
            // Check if moving left would go beyond left boundary
            if (newVelocity.x < 0 && colliderLeft + newVelocity.x * Time.fixedDeltaTime <= leftBoundary)
            {
                newVelocity.x = 0;
                // Snap to boundary if already past it
                if (colliderLeft < leftBoundary)
                {
                    float offset = leftBoundary - colliderLeft;
                    transform.position = new Vector3(transform.position.x + offset, transform.position.y, transform.position.z);
                }
            }
            // Check if moving right would go beyond right boundary
            else if (newVelocity.x > 0 && colliderRight + newVelocity.x * Time.fixedDeltaTime >= rightBoundary)
            {
                newVelocity.x = 0;
                // Snap to boundary if already past it
                if (colliderRight > rightBoundary)
                {
                    float offset = rightBoundary - colliderRight;
                    transform.position = new Vector3(transform.position.x + offset, transform.position.y, transform.position.z);
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
        float halfHeight = activeCollider.bounds.size.y * 0.5f;
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
        
        // Physics-based platform movement - no direct position manipulation
        if (isGrounded && groundCheck != null)
        {
            // Detect platform and apply its velocity to player
            Collider2D[] hits = Physics2D.OverlapCircleAll(groundCheck.position, checkRadius, groundLayer);
            
            foreach (var hit in hits)
            {
                if ((groundLayer.value & (1 << hit.gameObject.layer)) != 0)
                {
                    Rigidbody2D platformRb = hit.gameObject.GetComponent<Rigidbody2D>();
                    if (platformRb != null)
                    {
                        // Apply platform velocity to player's velocity
                        Vector2 platformVelocity = platformRb.linearVelocity;
                        newVelocity += platformVelocity;
                        break;
                    }
                }
            }
        }
    }

    public void SetFlattened(bool flattened)
    {
        // Prevent multiple flattening calls
        if (isFlattened == flattened) return;
        
        isFlattened = flattened;

        if (flattened)
        {
            // Scale sprite width to 1.3x and height to 1.3x
            Vector3 flattenedScale = new Vector3(originalScale.x * 1.3f, originalScale.y * 1.3f, originalScale.z);
            transform.localScale = flattenedScale;
            
            // Switch to flattened collider
            if (flattenedCollider == null)
            {
                return;
            }
            
            col.enabled = false; // Disable normal collider
            flattenedCollider.enabled = true; // Enable flattened collider
            activeCollider = flattenedCollider;
            
            // Change sprite to flattened version
            if (spriteRenderer != null && flattenedSprite != null)
            {
                spriteRenderer.sprite = flattenedSprite;
            }
        }
        else
        {
            // Switch back to normal collider
            if (flattenedCollider == null)
            {
                return;
            }
            
            flattenedCollider.enabled = false; // Disable flattened collider
            col.enabled = true; // Enable normal collider
            activeCollider = col;
            
            // Restore original sprite
            if (spriteRenderer != null && originalSprite != null)
            {
                spriteRenderer.sprite = originalSprite;
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
    
    // Method for hammer to check if player is grounded
    public bool IsGrounded()
    {
        return isGrounded;
    }
    
    void CheckWallContact()
    {
        if (!enableWallSliding) return;
        
        // Check for walls at multiple heights along the player's collider
        float colliderTop = activeCollider.bounds.max.y;
        float colliderBottom = activeCollider.bounds.min.y;
        float colliderCenter = transform.position.y;
        
        // Check at top and center always, and at feet only when not grounded (to avoid edge issues)
        float[] checkHeights = { colliderTop, colliderCenter };
        if (!isGrounded)
        {
            checkHeights = new float[] { colliderTop, colliderCenter, colliderBottom };
        }
        
        bool detectedLeftWall = false;
        bool detectedRightWall = false;
        
        foreach (float height in checkHeights)
        {
            Vector2 leftCheck = new Vector2(activeCollider.bounds.min.x - 0.05f, height);
            Vector2 rightCheck = new Vector2(activeCollider.bounds.max.x + 0.05f, height);
            
            RaycastHit2D leftHit = Physics2D.Raycast(leftCheck, Vector2.left, 0.15f);
            RaycastHit2D rightHit = Physics2D.Raycast(rightCheck, Vector2.right, 0.15f);
            
            if (leftHit.collider != null && !leftHit.collider.isTrigger)
            {
                detectedLeftWall = true;
            }
            if (rightHit.collider != null && !rightHit.collider.isTrigger)
            {
                detectedRightWall = true;
            }
        }
        
        // Add stability to wall detection - only change state if detected for multiple frames
        if (detectedLeftWall && !isTouchingLeftWall)
        {
            isTouchingLeftWall = true;
        }
        else if (!detectedLeftWall && isTouchingLeftWall)
        {
            // Only stop detecting wall if not detected for a few frames
            isTouchingLeftWall = false;
        }
        
        if (detectedRightWall && !isTouchingRightWall)
        {
            isTouchingRightWall = true;
        }
        else if (!detectedRightWall && isTouchingRightWall)
        {
            // Only stop detecting wall if not detected for a few frames
            isTouchingRightWall = false;
        }
        
        isTouchingWall = isTouchingLeftWall || isTouchingRightWall;
        
    }
}
