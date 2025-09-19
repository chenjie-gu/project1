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
    public float flattenDuration = 0f; // 0 = stay flattened; >0 = auto unflatten
    public bool breakCarriedKeyOnHit = true; // optional L2 fail rule

    Rigidbody2D rb;
    Collider2D trigger;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        trigger = GetComponent<Collider2D>();

        // ensure it behaves as a moving trigger
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        if (trigger != null) trigger.isTrigger = true;
    }

    void Start()
    {
        if (topPoint == null || bottomPoint == null)
        {
            Debug.LogError("Hammer: Assign topPoint and bottomPoint.");
            enabled = false; return;
        }
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

    void OnTriggerEnter2D(Collider2D other)
    {
        // Flatten player
        if (other.TryGetComponent<PlayerMovement>(out var player))
        {
            player.SetFlattened(true);

            // OPTIONAL: treat this as also hitting the carried key (because we disable key collider while held)
            if (breakCarriedKeyOnHit)
            {
                var heldKey = player.GetCarriedKey();
                if (heldKey != null && heldKey.IsHeld)
                {
                    heldKey.Break();
                    // TODO: call your fail flow (UI/reload) if wanted
                    // GameManager.Instance.LevelFail();
                }
            }

            if (flattenDuration > 0f)
                StartCoroutine(UnflattenAfter(player, flattenDuration));

            return;
        }

        // If you also want the hammer to break loose keys (not carried), you can detect Key here.
        // if (other.TryGetComponent<Key>(out var key) && !key.IsHeld) { /* maybe push or break */ }
    }

    IEnumerator UnflattenAfter(PlayerMovement p, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (p != null) p.SetFlattened(false);
    }
}
