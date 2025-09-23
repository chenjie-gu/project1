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

    [Header("Key Properties")]
    public KeyType keyType = KeyType.Normal;

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

    public void PickUp(Transform newHolder)
    {
        IsHeld = true;
        holder = newHolder;

        transform.SetParent(holder);
        transform.localPosition = Vector3.zero;
        
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
        IsHeld = false;
        holder = null;
        
        transform.SetParent(null);
        
        // Keep collider as trigger for pickup detection
        keyCollider.isTrigger = true;
        
        // Reset Rigidbody2D to kinematic
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
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
            var player = holder.GetComponentInParent<PlayerMovement>();
            if (player != null)
            {
                Collider2D playerCollider = player.GetComponent<Collider2D>();
                Vector3 desiredPosition = player.transform.position;
                
                if (playerCollider != null && keyCollider != null)
                {
                    float playerHalfHeight = player.transform.localScale.y * 0.5f;
                    
                    float keyHalfHeight = keyCollider.bounds.size.y * 0.5f;
                    desiredPosition.y = player.transform.position.y + playerHalfHeight + keyHalfHeight;
                }
                float moveInput = Input.GetAxisRaw("Horizontal");
                
                // Check if moving the key to desired position would cause collision
                // Use a smaller radius for more precise collision detection
                Collider2D[] hits = Physics2D.OverlapCircleAll(desiredPosition, 0.1f);
                bool wouldCollideWithPlatform = false;
                
                foreach (var hit in hits)
                {
                    if (hit != keyCollider && hit.gameObject.name == "Platform")
                    {
                        wouldCollideWithPlatform = true;
                        break;
                    }
                }
                
                if (wouldCollideWithPlatform)
                {
                    // Key would collide with platform at desired position
                    // Check if player is trying to move away from the collision
                    Vector2 currentKeyPos = transform.position;
                    Vector2 desiredKeyPos = desiredPosition;
                    Vector2 keyMovement = desiredKeyPos - currentKeyPos;
                    
                    // Check if player is trying to move away from the platform
                    // If player is moving horizontally and the key would move in the same direction, allow it
                    if (moveInput != 0 && Mathf.Sign(moveInput) == Mathf.Sign(keyMovement.x))
                    {
                        transform.position = desiredPosition;
                        player.SetBlockedByKey(false);
                    }
                    else if (moveInput != 0)
                    {
                        player.SetBlockedByKey(true);
                    }
                    else
                    {
                        // Player not moving horizontally, but might be jumping
                        // Check if this is vertical movement (jumping)
                        if (keyMovement.y > 0.1f)
                        {
                            player.SetBlockedByKey(true);
                        }
                        else
                        {
                            // Check player's vertical velocity to detect jumping
                            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                            if (playerRb != null && playerRb.linearVelocity.y > 0.1f)
                            {
                                player.SetBlockedByKey(true);
                            }
                            else
                            {
                                // Key is stuck but player is not moving - allow player to move away
                                player.SetBlockedByKey(false);
                            }
                        }
                    }
                }
                else
                {
                    // No collision, move the key and allow player movement
                    transform.position = desiredPosition;
                    player.SetBlockedByKey(false);
                }
            }
        }
    }
}