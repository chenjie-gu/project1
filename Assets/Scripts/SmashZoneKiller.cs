using UnityEngine;

public class SmashZoneKiller : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<PlayerMovement>(out var p))
        {
            Debug.LogError("Player failed: smashed by LargeMonster!");
            // TODO: call your game-over / respawn
        }
    }

    void OnTriggerStay2D(Collider2D other)   // catch the case where player is already inside
    {
        OnTriggerEnter2D(other);
    }
}
