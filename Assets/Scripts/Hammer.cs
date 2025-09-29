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
            // Hammer is completely safe - no game over, just break keys and flatten
            
            // Break carried key if player has one
            if (breakCarriedKeyOnHit)
            {
                var heldKey = player.GetCarriedKey();
                if (heldKey != null && heldKey.IsHeld)
                {
                    BreakKey(heldKey, player);
                }
            }
            
            // Flatten player if grounded and flattening is enabled
            if (flattenPlayerOnHit && player.IsGrounded())
            {
                player.SetFlattened(true);
            }
            return;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Handle key collision
        if (other.TryGetComponent<Key>(out var key) && breakCarriedKeyOnHit)
        {
            PlayerMovement player = null;
            if (key.IsHeld)
            {
                player = other.GetComponentInParent<PlayerMovement>();
            }
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
            key.Drop();
            player.SetCarriedKey(null);
        }
        
        // Create 2 smaller keys
        CreateSmallKey(keyPosition + Vector3.left * 0.5f);
        CreateSmallKey(keyPosition + Vector3.right * 0.5f);
        
        // Destroy the original key
        Destroy(key.gameObject);
    }


    void CreateSmallKey(Vector3 position)
    {
        if (smallKeyPrefab != null)
        {
            // Find the actual ground level using raycast
            RaycastHit2D hit = Physics2D.Raycast(position, Vector2.down, 10f);
            float groundLevel = hit.collider != null ? hit.point.y : -5f;
            
            // Create the small key first to get its actual height
            GameObject smallKey = Instantiate(smallKeyPrefab, position, Quaternion.identity);
            smallKey.name = "SmallKey";
            
            // Get the key component and set its type
            Key keyComponent = smallKey.GetComponent<Key>();
            if (keyComponent == null)
            {
                keyComponent = smallKey.AddComponent<Key>();
            }
            keyComponent.keyType = KeyType.Small;
            
            // Calculate the actual half height of the small key
            Collider2D keyCollider = smallKey.GetComponent<Collider2D>();
            float keyHalfHeight = keyCollider != null ? keyCollider.bounds.size.y * 0.5f : 0.25f;
            
            // Position the key on the ground: ground top + half key height
            Vector3 groundPosition = new Vector3(position.x, groundLevel + keyHalfHeight, position.z);
            smallKey.transform.position = groundPosition;
        }
        else
        {
            Debug.LogWarning("SmallKeyPrefab is not assigned in the Hammer Inspector!");
        }
    }
}
