using UnityEngine;

public enum KeyType
{
    Normal,
    Small
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
    
    [Header("Key Blocking")]
    public bool createBlockCollider = true;
    public bool isBlockColliderTrigger = false;

    public bool IsHeld { get; private set; }

    void Awake()
    {
        keyCollider = GetComponent<Collider2D>();
        keyCollider.isTrigger = true;
        
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.gravityScale = 0f;
        }
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
    
    void DestroyKeyBlockCollider()
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

        // Don't parent the key - position it manually to avoid scale inheritance
        transform.SetParent(null);
        transform.localScale = originalScale;
        
        // Create the blocking collider if enabled
        if (createBlockCollider)
        {
            CreateKeyBlockCollider();
        }
        
        // Keep collider as trigger to avoid blocking player movement
        keyCollider.isTrigger = true;
        
        // Keep Rigidbody2D as Kinematic but enable collision detection
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic; // Keep kinematic
            rb.gravityScale = 0f; // No gravity
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Don't rotate
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
        
        transform.SetParent(null);
        
        if (holderRef != null)
        {
            var player = holderRef.GetComponentInParent<PlayerMovement>();
            if (player != null)
            {
                // Find the ground below the player using raycast
                Vector2 raycastOrigin = new Vector2(player.transform.position.x, player.transform.position.y);
                RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, Vector2.down, Mathf.Infinity, player.groundLayer);
                
                if (hit.collider != null)
                {
                    // Position key on the ground
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
                        float keyHalfHeight = keyCollider.bounds.size.y * 0.5f;
                        float groundY = playerCollider.bounds.min.y;
                        Vector3 dropPosition = new Vector3(
                            player.transform.position.x,
                            groundY + keyHalfHeight,
                            player.transform.position.z
                        );
                        transform.position = dropPosition;
                    }
                }
                
                // Keep collider as trigger for pickup detection
                keyCollider.isTrigger = true;
                
                // Reset Rigidbody2D to kinematic
                var rb = GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Kinematic;
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
    }
}