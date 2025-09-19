using UnityEngine;

public interface ICarryable
{
    bool IsHeld { get; }
    void PickUp(Transform holder);
    void Drop();
}
