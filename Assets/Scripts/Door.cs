using UnityEngine;

public enum DoorType
{
    Normal,
    Small,
    Level2  // Requires key 1 and key 2
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
    private bool key1Deposited = false;
    private bool key2Deposited = false;
    private bool isOpened = false; // Track if door has been opened to prevent multiple sound plays

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
        var heldKey = player.GetCarriedKey();
        if (heldKey == null || !heldKey.IsHeld) return false;

        // Special handling for Level2 doors - require key 1 and key 2 (order doesn't matter)
        if (doorType == DoorType.Level2)
        {
            if (heldKey.keyID == 1)
            {
                if (key1Deposited)
                {
                    return false;
                }
                key1Deposited = true;
                heldKey.Drop();
                Destroy(heldKey.gameObject);
                UpdateVisuals();
                
                // Play sound when first key is deposited
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayDoorOpenSound();
                }
                
                // Check if both keys are deposited
                if (key1Deposited && key2Deposited)
                {
                    Open();
                }
                return true;
            }
            else if (heldKey.keyID == 2)
            {
                if (key2Deposited)
                {
                    return false;
                }
                key2Deposited = true;
                heldKey.Drop();
                Destroy(heldKey.gameObject);
                UpdateVisuals();
                
                // Play sound when first key is deposited
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayDoorOpenSound();
                }
                
                if (key1Deposited && key2Deposited)
                {
                    Open();
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        
        // Normal door behavior for other door types
        if (deposited >= requiredKeys) return false;

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

    private void UpdateVisuals()
    {
        bool isOpen = IsOpen();
        if (doorClosedVisual) doorClosedVisual.SetActive(!isOpen);
        if (doorOpenVisual) doorOpenVisual.SetActive(isOpen);
    }

    private void Open()
    {
        // Only open once
        if (isOpened) return;
        
        isOpened = true;
        
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
        UpdateVisuals();
        
        // Play door opening sound only when door actually opens
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayDoorOpenSound();
        }
    }
    
    public bool IsOpen()
    {
        // Level2 doors require both key 1 and key 2
        if (doorType == DoorType.Level2)
        {
            return key1Deposited && key2Deposited;
        }
        
        // Other door types use the normal system
        return deposited >= requiredKeys;
    }
    
    public int GetKeyCount()
    {
        return deposited;
    }
    
    private bool IsKeyCompatible(KeyType keyType, DoorType doorType)
    {
        // Cracked keys can't open any doors
        if (keyType == KeyType.Cracked)
        {
            return false;
        }
        
        // Level2 doors use key ID system, not key type
        if (doorType == DoorType.Level2)
        {
            return true; // Key compatibility is checked by keyID in TryUseKey
        }
        
        // Normal keys can only open Normal doors
        // Small keys can only open Small doors
        return (keyType == KeyType.Normal && doorType == DoorType.Normal) ||
               (keyType == KeyType.Small && doorType == DoorType.Small);
    }
}
