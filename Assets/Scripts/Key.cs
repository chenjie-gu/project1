using UnityEngine;

public enum KeyType { Normal, Small }

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Key : MonoBehaviour, ICarryable
{
    [Header("Key Properties")]
    public KeyType keyType = KeyType.Normal;
    public Vector2 carryOffset = new Vector2(0f, 1.0f);  // above the holder’s head

    public bool IsHeld { get; private set; }

    Collider2D keyCollider;
    Rigidbody2D rb;
    Transform holder;

    void Awake()
    {
        keyCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();

        // Default to kinematic (not falling) until something drops it
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        keyCollider.isTrigger = true; // easy pickup / no pushing while idle
    }

    // === ICarryable ===
    public void PickUp(Transform newHolder)
    {
        IsHeld = true;
        holder = newHolder;

        // Follow the holder precisely; no physics while held
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        keyCollider.isTrigger = true;

        // If the holder has a "CarryAnchor" use it; otherwise use holder transform
        var anchor = holder.Find("CarryAnchor");
        transform.SetParent(anchor ? anchor : holder);
        transform.localPosition = (Vector3)carryOffset;
    }

    public void Drop()
    {
        IsHeld = false;

        transform.SetParent(null);
        holder = null;

        // Turn on real physics so it behaves like an object and doesn’t “fly”
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        keyCollider.isTrigger = false;
    }

    public void Break()
    {
        Destroy(gameObject);
    }

    void LateUpdate()
    {
        // While held, keep the key locked to the anchor each frame.
        if (IsHeld && holder != null)
        {
            var anchor = transform.parent ? transform.parent : holder;
            transform.position = anchor.position + (Vector3)carryOffset;
        }
    }
}
