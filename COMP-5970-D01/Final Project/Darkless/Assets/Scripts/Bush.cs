using UnityEngine;

// A bush (a green-tinted rock mesh) the player can walk THROUGH but is slowed inside. It's a TRIGGER
// volume: when the player enters, it tells their PlayerSlow to slow down; when they leave, it clears it.
// No solid collision — you push through the foliage, you just move slower while in it.
//
// Put this on a bush prefab that has a trigger Collider.
[RequireComponent(typeof(Collider))]
public class Bush : MonoBehaviour
{
    // Ensures the collider is a trigger when the component is first added in the Editor.
    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        var slow = other.GetComponentInParent<PlayerSlow>();
        if (slow != null) slow.AddSlow();
    }

    void OnTriggerExit(Collider other)
    {
        var slow = other.GetComponentInParent<PlayerSlow>();
        if (slow != null) slow.RemoveSlow();
    }
}
