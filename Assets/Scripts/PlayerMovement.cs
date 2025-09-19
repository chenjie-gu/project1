using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;

    [Header("Keys")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode interactKey = KeyCode.E;

    [Header("Ground Check")]
    public Transform groundCheck;          // assign a child at feet
    public float checkRadius = 0.25f;
    public LayerMask groundLayer;          // include your Ground layer

    [Header("Carry")]
    public Transform carryAnchor;          // assign a child above head
    public float pickupRadius = 1f;
    private ICarryable carried;

    [Header("Flatten")]
    public bool isFlattened = false;
    public float flattenedJumpForce = 0f;          // 0 = cannot jump while flattened
    public Vector2 normalColliderSize = new Vector2(0.9f, 1.6f);
    public Vector2 flattenedColliderSize = new Vector2(1.6f, 0.5f);
    public Vector3 normalCarryOffset = new Vector3(0f, 0.8f, 0f);
    public Vector3 flattenedCarryOffset = new Vector3(0f, 0.35f, 0f);

    Rigidbody2D rb;
    Collider2D col;
    float moveInput;
    bool isGrounded;
    bool jumpRequested;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        SetFlattened(false); // start normal
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(jumpKey) && isGrounded)
            jumpRequested = true;

        if (Input.GetKeyDown(interactKey))
        {
            if (carried != null)
            {
                carried.Drop();
                carried = null;
            }
            else
            {
                var hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius);
                foreach (var h in hits)
                {
                    var c = h.GetComponent<ICarryable>();
                    if (c != null && !c.IsHeld)
                    {
                        if (carryAnchor != null) c.PickUp(carryAnchor);
                        carried = c;
                        break;
                    }
                }
            }
        }

        // keep carry anchor aligned to form
        if (carryAnchor != null)
            carryAnchor.localPosition = isFlattened ? flattenedCarryOffset : normalCarryOffset;
    }

    void FixedUpdate()
    {
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        // horizontal
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // jump
        if (jumpRequested)
        {
            float jf = isFlattened ? flattenedJumpForce : jumpForce;
            if (jf > 0f)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jf);
            jumpRequested = false;
        }
    }

    public void SetFlattened(bool flattened)
    {
        isFlattened = flattened;

        // change collider size if BoxCollider2D, else just scale visual
        if (col is BoxCollider2D box)
        {
            box.size = flattened ? flattenedColliderSize : normalColliderSize;
            box.offset = Vector2.zero;
        }

        // optional visual squash
        transform.localScale = flattened ? new Vector3(1.2f, 0.6f, 1f) : Vector3.one;
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (groundCheck != null) Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
