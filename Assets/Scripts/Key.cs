using UnityEngine;

public enum KeyType
{
    Normal,
    Small,
    Cracked
}

[RequireComponent(typeof(Collider2D))]
public class Key : MonoBehaviour, ICarryable
{
    Collider2D keyCollider;
    Transform holder;
    Vector3 originalScale; // Store original scale
    bool scaleStored = false; // Flag to ensure we only store once
    
    // Key blocking system
    CapsuleCollider2D keyBlockCollider;

    [Header("Key Properties")]
    public KeyType keyType = KeyType.Normal;
    public int keyID = 0; // 0 = no specific ID, 1 = key 1, 2 = key 2, etc.
    
    [Header("Key Blocking")]
    public bool createBlockCollider = true;
    public bool isBlockColliderTrigger = false;
    
    [Header("Fall Break")]
    public float fallBreakHeight = 5.0f; // Minimum fall height to break the key

    public bool IsHeld { get; private set; }
    
    // Fall tracking
    private float fallStartHeight;
    private Rigidbody2D rb;
    
    // Public method to make the key start falling
    public void StartFalling()
    {
        if (IsHeld || keyType != KeyType.Normal) return;
        
        fallStartHeight = transform.position.y;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
        }
        if (keyCollider != null)
        {
            keyCollider.isTrigger = false;
        }
    }

    void Awake()
    {
        keyCollider = GetComponent<Collider2D>();
        keyCollider.isTrigger = true;
        
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.isKinematic = true;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    void CreateKeyBlockCollider()
    {
        keyBlockCollider = holder.gameObject.AddComponent<CapsuleCollider2D>();
        
        CapsuleCollider2D keyCapsuleCollider = keyCollider as CapsuleCollider2D;
        
        // Account for both key's scale and player's scale
        Vector3 playerScale = holder.localScale;
        Vector3 keyScale = transform.localScale;
        keyBlockCollider.size = new Vector2(
            keyCapsuleCollider.size.x * keyScale.x / playerScale.x,
            keyCapsuleCollider.size.y * keyScale.y / playerScale.y
        );
        
        keyBlockCollider.offset = keyCapsuleCollider.offset;

        float keyHalfHeight = keyCollider.bounds.size.y * 0.5f;
        SpriteRenderer playerSprite = holder.GetComponent<SpriteRenderer>();
        float playerTopY;
        playerTopY = playerSprite.bounds.max.y;
        
        float keyCenterY = playerTopY + keyHalfHeight;
        
        transform.position = new Vector3(holder.position.x, keyCenterY, holder.position.z);
        
        float offsetY = keyCenterY - holder.position.y;
        // Convert world offset to local offset (accounting for player scale)
        keyBlockCollider.offset = new Vector2(keyBlockCollider.offset.x, keyBlockCollider.offset.y + (offsetY / playerScale.y));
        
        // Set the collider as trigger or solid based on setting
        keyBlockCollider.isTrigger = isBlockColliderTrigger;
    }
    
    public void DestroyKeyBlockCollider()
    {
        if (keyBlockCollider != null)
        {
            Destroy(keyBlockCollider);
            keyBlockCollider = null;
        }
    }

    public void PickUp(Transform newHolder)
    {
        // Store original scale before first pickup
        if (!scaleStored)
        {
            originalScale = transform.localScale;
            scaleStored = true;
        }
        
        IsHeld = true;
        holder = newHolder;

        transform.localScale = originalScale;
        
        // Create the blocking collider if enabled
        if (createBlockCollider)
        {
            CreateKeyBlockCollider();
        }
        
        keyCollider.isTrigger = true;
        
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    public void Drop()
    {
        // Store holder reference before clearing it
        Transform holderRef = holder;
        
        IsHeld = false;
        holder = null;
        
        // Destroy the blocking collider if it was created
        if (createBlockCollider)
        {
            DestroyKeyBlockCollider();
        }
        
        if (holderRef != null)
        {
            var player = holderRef.GetComponentInParent<PlayerMovement>();
            if (player != null)
            {
                Vector2 raycastOrigin = new Vector2(player.transform.position.x, player.transform.position.y);
                RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, Vector2.down, Mathf.Infinity, player.groundLayer);
                
                if (hit.collider != null)
                {
                    float keyHalfHeight = keyCollider.bounds.size.y * 0.5f;
                    Vector3 dropPosition = new Vector3(
                        player.transform.position.x,
                        hit.point.y + keyHalfHeight,
                        player.transform.position.z
                    );
                    transform.position = dropPosition;
                }
                else
                {
                    // Fallback to original method if no ground found
                    Collider2D playerCollider = player.GetComponent<Collider2D>();
                    if (player.isFlattened && player.flattenedCollider != null)
                    {
                        playerCollider = player.flattenedCollider;
                    }
                    
                    if (playerCollider != null)
                    {
                        // Calculate distance from transform center to collider bottom
                        float distanceFromCenterToBottom = -keyCollider.bounds.min.y;
                        float groundY = playerCollider.bounds.min.y;
                        Vector3 dropPosition = new Vector3(
                            player.transform.position.x,
                            groundY + distanceFromCenterToBottom,
                            player.transform.position.z
                        );
                        transform.position = dropPosition;
                    }
                }
                
                // Keep collider as trigger for pickup detection
                keyCollider.isTrigger = true;
                
                // Reset Rigidbody2D to kinematic
                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Kinematic;
                }
            }
        }
        
        // If key is dropped and not on ground, enable physics to let it fall
        if (!IsHeld && keyType == KeyType.Normal)
        {
            PlayerMovement player = FindObjectOfType<PlayerMovement>();
            if (player != null)
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.5f, player.groundLayer);
                if (hit.collider == null)
                {
                    // No ground below, enable physics and track fall
                    fallStartHeight = transform.position.y;
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.gravityScale = 1f;
                    keyCollider.isTrigger = false;
                }
            }
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Only check if key is falling (has physics enabled)
        if (rb == null || rb.isKinematic || keyType != KeyType.Normal) return;
        
        // Check if we hit the ground layer
        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        if (player != null)
        {
            int groundLayer = player.groundLayer.value;
            int collisionLayer = 1 << collision.gameObject.layer;
            
            if ((groundLayer & collisionLayer) != 0)
            {
                // Calculate fall distance
                float fallDistance = fallStartHeight - transform.position.y;
                
                if (fallDistance >= fallBreakHeight)
                {
                    // Break the key
                    Hammer hammer = FindObjectOfType<Hammer>();
                    if (hammer != null && hammer.smallKeyPrefab != null && hammer.crackedKeyPrefab != null)
                    {
                        Hammer.SplitKey(this, transform.position, hammer.smallKeyPrefab, hammer.crackedKeyPrefab);
                    }
                    else
                    {
                        Destroy(gameObject);
                    }
                }
                else
                {
                    // Stop falling - reset to kinematic
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.gravityScale = 0f;
                    rb.linearVelocity = Vector2.zero;
                    keyCollider.isTrigger = true;
                }
            }
        }
    }

    public void Break()
    {
        Destroy(gameObject);
    }
    
    void Update()
    {
        if (IsHeld && holder != null)
        {
            // Stop falling if picked up
            if (rb != null && !rb.isKinematic)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                rb.linearVelocity = Vector2.zero;
                keyCollider.isTrigger = true;
            }
            
            // Use SpriteRenderer bounds for accurate visual positioning
            SpriteRenderer playerSprite = holder.GetComponent<SpriteRenderer>();
            float playerTopY;
            playerTopY = playerSprite.bounds.max.y;
            
            // Check if player is flattened and adjust positioning
            PlayerMovement player = holder.GetComponent<PlayerMovement>();
            if (player != null && player.isFlattened)
            {
                // When flattened, the player's scale changes, so we need to account for that
                // The SpriteRenderer bounds should already account for the scale change
                // But we might need to adjust based on the flattened collider
                Collider2D flattenedCollider = player.flattenedCollider;
                if (flattenedCollider != null && flattenedCollider.enabled)
                {
                    // Use the flattened collider bounds for more accurate positioning
                    playerTopY = flattenedCollider.bounds.max.y;
                }
            }
            float keyHalfHeight = keyCollider.bounds.size.y * 0.5f;
            
            float keyCenterY = playerTopY + keyHalfHeight;
            
            transform.position = new Vector3(holder.position.x, keyCenterY, holder.position.z);
        }
        else if (!IsHeld && rb != null && !rb.isKinematic)
        {
            // Update fall start height if we're going higher while falling
            if (transform.position.y > fallStartHeight)
            {
                fallStartHeight = transform.position.y;
            }
        }
    }
}