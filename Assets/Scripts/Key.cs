using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Key : MonoBehaviour, ICarryable
{
    public bool breakWhenHammeredIfCarried = true;

    Rigidbody2D rb;
    Collider2D col;

    public bool IsHeld { get; private set; }
    Transform holder;

    RigidbodyType2D originalBodyType;
    float originalGravity;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        originalBodyType = rb.bodyType;     // usually Dynamic
        originalGravity  = rb.gravityScale; // keep designer’s setting
    }

    public void PickUp(Transform newHolder)
    {
        IsHeld = true;
        holder = newHolder;

        transform.SetParent(holder);
        transform.localPosition = Vector3.zero;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        if (col) col.enabled = false;   // disable collisions while held
    }

    public void Drop()
    {
        IsHeld = false;
        holder = null;
        transform.SetParent(null);

        rb.bodyType = RigidbodyType2D.Dynamic;  // or originalBodyType
        rb.gravityScale = originalGravity;
        if (col) col.enabled = true;
    }

    public void Break()
    {
        Destroy(gameObject);
    }
}
