using UnityEngine;

/// <summary>
/// Simple “jail/cage” that blocks the player and contains a Key (or any ICarryable).
/// Breaks when hit by a charging SmallMonster with sufficient impulse.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BreakableCage : MonoBehaviour
{
    [Header("Contents")]
    public GameObject keyObject;          // assign the Key GameObject (can be a prefab child)
    public bool disableKeyUntilBroken = true;

    [Header("Visuals")]
    public Sprite intactSprite;
    public Sprite brokenSprite;

    [Header("Physics")]
    public bool becomeNonSolidWhenBroken = true; // turn collider into trigger (or disable)
    public bool dropKeyOnGround = true;          // nudge key down onto ground

    BoxCollider2D col;
    SpriteRenderer sr;
    bool isBroken;

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        sr  = GetComponent<SpriteRenderer>();

    // Fallback: if you didn’t assign Intact Sprite, use whatever the SR has now
        if (!intactSprite && sr) intactSprite = sr.sprite;

        if (disableKeyUntilBroken && keyObject) SetKeyActive(false);
        if (intactSprite && sr) sr.sprite = intactSprite;
    }


    public void Break()
    {
        if (isBroken) return;
        isBroken = true;

        // visuals
        if (brokenSprite && sr) sr.sprite = brokenSprite;

        // physics: stop blocking
        if (becomeNonSolidWhenBroken)
        {
            // simplest: become a trigger so player can walk through debris
            col.isTrigger = true;
        }

        // release key
        if (keyObject)
        {
            SetKeyActive(true);

            if (dropKeyOnGround)
            {
                // place right below the cage
                var rb = keyObject.GetComponent<Rigidbody2D>();
                if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic;

                // small downward nudge so it rests on the floor
                keyObject.transform.position = new Vector3(
                    keyObject.transform.position.x,
                    transform.position.y - col.bounds.extents.y - 0.05f,
                    keyObject.transform.position.z
                );

                var keyCol = keyObject.GetComponent<Collider2D>();
                if (keyCol) keyCol.isTrigger = false;
            }
        }

        // (optional) play sound/particles here
        // AudioSource.PlayClipAtPoint(breakClip, transform.position);
    }

    void SetKeyActive(bool on)
    {
        keyObject.SetActive(on);
    }
}
