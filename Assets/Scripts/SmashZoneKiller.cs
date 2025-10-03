using UnityEngine;

public class SmashZoneKiller : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<PlayerMovement>(out var p))
        {
            Debug.LogError("Player failed: smashed by LargeMonster!");
            // Call GameManager's game over system
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
        }
    }

    void OnTriggerStay2D(Collider2D other)   // catch the case where player is already inside
    {
        OnTriggerEnter2D(other);
    }
}
