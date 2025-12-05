using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Hammer : MonoBehaviour
{
    [Header("Motion")]
    public Transform topPoint;
    public Transform bottomPoint;
    public float travelTimeX = 1.0f;   // seconds from top→bottom or bottom→top
    public float pauseTimeY  = 0.7f;   // pause at ends

    [Header("Effects")]
    public bool breakCarriedKeyOnHit = true;
    public bool flattenPlayerOnHit = true;
    public bool killPlayerOnHit = true;
    public GameObject smallKeyPrefab;
    public GameObject crackedKeyPrefab; // Cracked key that can't open any doors

    Rigidbody2D rb;
    Collider2D trigger;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        trigger = GetComponent<Collider2D>();

        // ensure it behaves as a moving solid object
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        if (trigger != null) trigger.isTrigger = false;
    }

    void Start()
    {
        if (topPoint == null || bottomPoint == null)
        {
            Debug.LogError("Hammer: Assign topPoint and bottomPoint.");
            enabled = false; return;
        }
        
        transform.position = topPoint.position;
        StartCoroutine(Cycle());
    }

    IEnumerator Cycle()
    {
        while (true)
        {
            // Top → Bottom
            yield return MoveBetween(topPoint.position, bottomPoint.position, travelTimeX);
            yield return new WaitForSeconds(pauseTimeY);
            // Bottom → Top
            yield return MoveBetween(bottomPoint.position, topPoint.position, travelTimeX);
            yield return new WaitForSeconds(pauseTimeY);
        }
    }

    IEnumerator MoveBetween(Vector3 a, Vector3 b, float t)
    {
        float time = 0f;
        while (time < t)
        {
            float u = time / t;
            transform.position = Vector3.Lerp(a, b, u);
            time += Time.deltaTime;
            yield return null;
        }
        transform.position = b;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Handle player collision
        if (collision.gameObject.TryGetComponent<PlayerMovement>(out var player))
        {
            // Check if player has a carried key and break it (only if player is grounded on ground)
            var carriedKey = player.GetCarriedKey();
            if (carriedKey != null && carriedKey.IsHeld && breakCarriedKeyOnHit && player.IsGroundedOnGround())
            {
                BreakKey(carriedKey, player);
            }
            
            // Hammer is completely safe - no game over, just flatten
            
            // Flatten player if grounded on Ground layer and flattening is enabled
            // Only flatten when player is standing on Ground layer (not Platform/hammer)
            if (flattenPlayerOnHit && player.IsGroundedOnGround())
            {
                player.SetFlattened(true);
            }
            return;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Handle key collision - break any key that the hammer hits
        if (other.TryGetComponent<Key>(out var key) && breakCarriedKeyOnHit)
        {
            // Don't break keys that are being held by a player
            // (those are handled in OnCollisionEnter2D to avoid double-breaking)
            if (key.IsHeld)
            {
                return;
            }
            
            PlayerMovement player = null;
            BreakKey(key, player);
        }
    }

    void BreakKey(Key key, PlayerMovement player = null)
    {
        // Only break normal keys, not small keys
        if (key.keyType != KeyType.Normal) return;
        
        Vector3 keyPosition = key.transform.position;
        
        // Handle carried key
        if (key.IsHeld && player != null)
        {
            // Destroy the key block collider BEFORE dropping the key
            // This prevents it from persisting when the player flattens
            Key keyComponent = key.GetComponent<Key>();
            if (keyComponent != null && keyComponent.createBlockCollider)
            {
                keyComponent.DestroyKeyBlockCollider();
            }
            
            key.Drop();
            player.SetCarriedKey(null);
        }
        
        // Use the static method to split the key
        SplitKey(key, keyPosition, smallKeyPrefab, crackedKeyPrefab);
    }
    
    // Public static method that can be called from Key script
    public static void SplitKey(Key key, Vector3 keyPosition, GameObject smallKeyPrefab, GameObject crackedKeyPrefab)
    {
        // Only break normal keys, not small keys
        if (key.keyType != KeyType.Normal) return;
        
        // Play key break sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayKeyBreakSound();
        }
        
        // Create one small key and one cracked key (cracked keys can't open any doors)
        CreateKeyAtPosition(keyPosition + Vector3.left * 0.5f, smallKeyPrefab, KeyType.Small);
        CreateKeyAtPosition(keyPosition + Vector3.right * 0.5f, crackedKeyPrefab, KeyType.Cracked);
        
        // Destroy the original key
        Destroy(key.gameObject);
    }


    void CreateSmallKey(Vector3 position, GameObject prefab, KeyType keyType = KeyType.Small)
    {
        CreateKeyAtPosition(position, prefab, keyType);
    }
    
    // Public static method for creating keys at a position
    public static void CreateKeyAtPosition(Vector3 position, GameObject prefab, KeyType keyType = KeyType.Small)
    {
        if (prefab != null)
        {
            // Find the actual ground level using raycast (same as Key Drop() method)
            // We need to find a player to get the ground layer mask
            PlayerMovement player = FindObjectOfType<PlayerMovement>();
            if (player == null)
            {
                Debug.LogError("No PlayerMovement found in scene!");
                return;
            }
            
            RaycastHit2D hit = Physics2D.Raycast(position, Vector2.down, 10f, player.groundLayer);
            float groundLevel;
            if (hit.collider != null)
            {
                groundLevel = hit.point.y;
            }
            else
            {
                Debug.LogError("No ground found below small key position! Check your ground layer setup.");
                return; // Don't create the key if we can't find ground
            }
            
            // Create the key at a temporary position first
            GameObject key = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            key.name = keyType == KeyType.Small ? "SmallKey" : (keyType == KeyType.Cracked ? "CrackedKey" : "NormalKey");
            
            // Get the key component and set its type
            Key keyComponent = key.GetComponent<Key>();
            if (keyComponent == null)
            {
                keyComponent = key.AddComponent<Key>();
            }
            keyComponent.keyType = keyType;
            
            // Use the same logic as normal key Drop() method
            Collider2D keyCollider = key.GetComponent<Collider2D>();
            if (keyCollider != null)
            {
                // Position the key temporarily to get accurate bounds
                key.transform.position = new Vector3(position.x, groundLevel, position.z);
                
                // Force bounds update
                Physics2D.SyncTransforms();
                
                // Calculate the distance from transform center to collider bottom
                float distanceFromCenterToBottom = key.transform.position.y - keyCollider.bounds.min.y;
                
                // Position the key so its bottom edge touches the ground
                Vector3 groundPosition = new Vector3(position.x, groundLevel + distanceFromCenterToBottom, position.z);
                key.transform.position = groundPosition;
                
                // Set as trigger AFTER positioning to prevent collision with player
                keyCollider.isTrigger = true;
            }
            else
            {
                Debug.LogError("Key prefab is missing a Collider2D component!");
                Destroy(key); // Clean up the created object
                return;
            }
            
            // Draw debug line to show ground level
            Debug.DrawLine(new Vector3(position.x - 0.5f, groundLevel, 0), new Vector3(position.x + 0.5f, groundLevel, 0), Color.red, 5f);
        }
        else
        {
            Debug.LogWarning("SmallKeyPrefab is not assigned in the Hammer Inspector!");
        }
    }
}
