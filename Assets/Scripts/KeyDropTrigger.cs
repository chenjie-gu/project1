using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class KeyDropTrigger : MonoBehaviour
{
    [Header("Key Drop Settings")]
    public Key keyToDrop; // Assign the key that should fall from the tree
    
    void Start()
    {
        // Ensure trigger is set up
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if player entered the trigger
        if (other.gameObject.name == "Player" || other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerMovement>();
            if (player != null && keyToDrop != null)
            {
                // Make the key start falling
                keyToDrop.StartFalling();
            }
        }
    }
}

