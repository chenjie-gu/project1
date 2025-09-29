using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Hammer : MonoBehaviour
{
    [Header("Motion")]
    public Transform topPoint;
    public Transform bottomPoint;
    public float travelTimeX = 1.0f;
    public float pauseTimeY  = 0.7f;

    [Header("Effects")]
    public bool breakCarriedKeyOnHit = true;
    public GameObject smallKeyPrefab;

    [Header("Flattening")]

    Rigidbody2D rb;
    Collider2D col;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        if (col != null) col.isTrigger = false;

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Start()
    {
        if (topPoint == null || bottomPoint == null)
        {
            Debug.LogError("Hammer: Assign topPoint and bottomPoint.");
            enabled = false; return;
        }

        rb.position = topPoint.position;
        StartCoroutine(Cycle());
    }

    IEnumerator Cycle()
    {
        while (true)
        {
            yield return MoveBetween(topPoint.position, bottomPoint.position, travelTimeX);
            yield return new WaitForSeconds(pauseTimeY);
            yield return MoveBetween(bottomPoint.position, topPoint.position, travelTimeX);
            yield return new WaitForSeconds(pauseTimeY);
        }
    }

    IEnumerator MoveBetween(Vector2 a, Vector2 b, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            float u = t / duration;
            Vector2 pos = Vector2.Lerp(a, b, u);
            rb.MovePosition(pos);
            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        rb.MovePosition(b);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
    // Player
        if (collision.gameObject.TryGetComponent<PlayerMovement>(out var player))
        {
        if (breakCarriedKeyOnHit)
        {
                var heldKey = player.GetCarriedKey();
                if (heldKey != null && heldKey.IsHeld) BreakKey(heldKey, player);
        }

        if (player.IsGrounded())
            {
            player.FlattenPermanently();   // <-- stays flattened
            }
        return;
    }

    // Small monster
    if (collision.gameObject.TryGetComponent<SmallMonster>(out var small))
    {
        small.Flatten();
        return;
    }
}




void OnTriggerEnter2D(Collider2D other)
{
    if (other.TryGetComponent<Key>(out var key) && breakCarriedKeyOnHit)
    {
        PlayerMovement player = key.IsHeld ? other.GetComponentInParent<PlayerMovement>() : null;
        BreakKey(key, player);
    }

    if (other.TryGetComponent<SmallMonster>(out var small))
    {
        small.Flatten();
    }
}


    // --- Keys from earlier levels (kept intact) ---
    void BreakKey(Key key, PlayerMovement player = null)
    {
        if (key.keyType != KeyType.Normal) return;

        Vector3 p = key.transform.position;

        if (key.IsHeld && player != null)
        {
            key.Drop();
            player.SetCarriedKey(null);
        }

        CreateSmallKey(p + Vector3.left * 0.5f);
        CreateSmallKey(p + Vector3.right * 0.5f);
        Destroy(key.gameObject);
    }

    void CreateSmallKey(Vector3 position)
    {
        if (smallKeyPrefab == null)
        {
            Debug.LogWarning("SmallKeyPrefab is not assigned in the Hammer Inspector!");
            return;
        }

        RaycastHit2D hit = Physics2D.Raycast(position, Vector2.down, 10f);
        float groundLevel = hit.collider != null ? hit.point.y : position.y - 1.0f;
        float keyHeight = 0.5f;
        Vector3 groundPosition = new Vector3(position.x, groundLevel + keyHeight, position.z);

        GameObject smallKey = Instantiate(smallKeyPrefab, groundPosition, Quaternion.identity);
        smallKey.name = "SmallKey";
        Key keyComponent = smallKey.GetComponent<Key>() ?? smallKey.AddComponent<Key>();
        keyComponent.keyType = KeyType.Small;
    }
}
