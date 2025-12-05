using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Trap : MonoBehaviour
{
    [Header("Trap Settings")]
    public bool isDeadly = true;
    public bool disableOnHit = false; // Changed to false so traps stay visible
    
    void Start()
    {
        // Ensure trap has a collider
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            // Make it a solid collider so it physically blocks the player
            col.isTrigger = false;
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Only check if the actual player (not carried objects) hit the trap
        if (collision.gameObject.name == "Player" || collision.gameObject.CompareTag("Player"))
        {
            var player = collision.gameObject.GetComponent<PlayerMovement>();
            if (player != null && isDeadly)
            {
                // Play trap death sound
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayTrapDeathSound();
                }
                
                // Trigger game over
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.GameOver();
                }
                
                // Disable trap if set to do so
                if (disableOnHit)
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
