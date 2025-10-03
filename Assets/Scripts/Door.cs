using UnityEngine;

public enum DoorType
{
    Normal,
    Small
}

[RequireComponent(typeof(Collider2D))]
public class Door : MonoBehaviour
{
    [Header("Door Properties")]
    public DoorType doorType = DoorType.Normal;
    public int requiredKeys = 1;
    public GameObject doorClosedVisual;
    public GameObject doorOpenVisual;

    private int deposited = 0;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void Start() { UpdateVisuals(); }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerMovement>();
        if (!player) return;

        // Only consume key if player is carrying one and presses interact
        // This will be handled by the player's interact system instead
    }

    public bool TryUseKey(PlayerMovement player)
    {
        if (deposited >= requiredKeys) return false;

        var heldKey = player.GetCarriedKey();
        if (heldKey != null && heldKey.IsHeld)
        {
            // Check if key type matches door type
            if (!IsKeyCompatible(heldKey.keyType, doorType))
            {
                return false;
            }
            
            heldKey.Drop();
            Destroy(heldKey.gameObject);
            deposited++;
            UpdateVisuals();
            
            if (deposited >= requiredKeys)
                Open();
            
            return true;
        }
        
        return false;
    }

    private void UpdateVisuals()
    {
        bool isOpen = deposited >= requiredKeys;
        if (doorClosedVisual) doorClosedVisual.SetActive(!isOpen);
        if (doorOpenVisual) doorOpenVisual.SetActive(isOpen);
    }

    private void Open()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
        UpdateVisuals();
        
        // Play door opening sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayDoorOpenSound();
        }
    }
    
    public bool IsOpen()
    {
        return deposited >= requiredKeys;
    }
    
    public int GetKeyCount()
    {
        return deposited;
    }
    
    private bool IsKeyCompatible(KeyType keyType, DoorType doorType)
    {
        // Normal keys can only open Normal doors
        // Small keys can only open Small doors
        return (keyType == KeyType.Normal && doorType == DoorType.Normal) ||
               (keyType == KeyType.Small && doorType == DoorType.Small);
    }
}
