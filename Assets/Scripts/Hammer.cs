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
    public GameObject smallKeyPrefab1;
    public GameObject smallKeyPrefab2;

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
            Debug.Log($"Hammer hit player. Player flattened: {player.isFlattened}");
            
            // Check if player has a carried key and break it (only if player is grounded)
            var carriedKey = player.GetCarriedKey();
            if (carriedKey != null && carriedKey.IsHeld && breakCarriedKeyOnHit && player.IsGrounded())
            {
                Debug.Log("Breaking carried key from player collision (player is grounded)");
                BreakKey(carriedKey, player);
            }
            
            // Hammer is completely safe - no game over, just flatten
            
            // Flatten player if grounded and flattening is enabled
            if (flattenPlayerOnHit && player.IsGrounded())
            {
                Debug.Log("Flattening player");
                player.SetFlattened(true);
                Debug.Log($"After flattening. Player flattened: {player.isFlattened}");
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
            Debug.Log($"Breaking key for player. Player flattened: {player.isFlattened}");
            
            // Destroy the key block collider BEFORE dropping the key
            // This prevents it from persisting when the player flattens
            Key keyComponent = key.GetComponent<Key>();
            if (keyComponent != null && keyComponent.createBlockCollider)
            {
                Debug.Log("Destroying key block collider");
                keyComponent.DestroyKeyBlockCollider();
            }
            
            key.Drop();
            player.SetCarriedKey(null);
            
            Debug.Log($"After key break. Player flattened: {player.isFlattened}");
        }
        
        // Create 2 smaller keys using different prefabs
        CreateSmallKey(keyPosition + Vector3.left * 0.5f, smallKeyPrefab1);
        CreateSmallKey(keyPosition + Vector3.right * 0.5f, smallKeyPrefab2);
        
        // Destroy the original key
        Destroy(key.gameObject);
    }


    void CreateSmallKey(Vector3 position, GameObject prefab)
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
            
            // Create the small key at a temporary position first
            GameObject smallKey = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            smallKey.name = "SmallKey";
            
            // Get the key component and set its type
            Key keyComponent = smallKey.GetComponent<Key>();
            if (keyComponent == null)
            {
                keyComponent = smallKey.AddComponent<Key>();
            }
            keyComponent.keyType = KeyType.Small;
            
            // Use the same logic as normal key Drop() method
            Collider2D keyCollider = smallKey.GetComponent<Collider2D>();
            if (keyCollider != null)
            {
                // Position the key temporarily to get accurate bounds
                smallKey.transform.position = new Vector3(position.x, groundLevel, position.z);
                
                // Force bounds update
                Physics2D.SyncTransforms();
                
                // Calculate the distance from transform center to collider bottom
                float distanceFromCenterToBottom = smallKey.transform.position.y - keyCollider.bounds.min.y;
                Debug.Log($"Small key debug - Ground level: {groundLevel}, Distance to bottom: {distanceFromCenterToBottom}, Final position: {new Vector3(position.x, groundLevel + distanceFromCenterToBottom, position.z)}");
                
                // Position the key so its bottom edge touches the ground
                Vector3 groundPosition = new Vector3(position.x, groundLevel + distanceFromCenterToBottom, position.z);
                smallKey.transform.position = groundPosition;
                
                // Set as trigger AFTER positioning to prevent collision with player
                keyCollider.isTrigger = true;
                
                // Final check - verify the key is actually positioned correctly
                Debug.Log($"Small key ACTUAL final position: {smallKey.transform.position}, Collider bounds: {keyCollider.bounds}");
            }
            else
            {
                Debug.LogError("Small key prefab is missing a Collider2D component!");
                Destroy(smallKey); // Clean up the created object
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
