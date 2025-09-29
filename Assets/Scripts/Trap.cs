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
            col.isTrigger = true; // Make it a trigger so it doesn't block movement
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Only check if the actual player (not carried objects) hit the trap
        if (other.gameObject.name == "Player" || other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerMovement>();
            if (player != null && isDeadly)
            {
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
