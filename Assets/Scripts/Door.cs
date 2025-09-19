using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Door : MonoBehaviour
{
    public int requiredKeys = 1;
    public GameObject doorClosedVisual;   // assign child
    public GameObject doorOpenVisual;     // assign child

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

    // Look for a Key somewhere under the player (i.e., being carried)
        var heldKey = other.GetComponentInChildren<Key>();
        if (heldKey != null && deposited < requiredKeys)
        {
            heldKey.Drop();
            Destroy(heldKey.gameObject);
            deposited++;
            UpdateVisuals();
        }

    if (deposited >= requiredKeys)
        Open();
    }

    private void UpdateVisuals()
    {
        if (doorClosedVisual) doorClosedVisual.SetActive(deposited < requiredKeys);
        if (doorOpenVisual)   doorOpenVisual.SetActive(deposited >= requiredKeys);
    }

    private void Open()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false; // let player pass
        UpdateVisuals();
        // GameManager.Instance.LevelWin(); // optional
    }
}
