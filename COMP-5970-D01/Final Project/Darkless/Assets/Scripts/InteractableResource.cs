using UnityEngine;

// Sits on each harvestable resource's parent object (apple tree, ore rock). Holds what the
// resource is, how much it gives, how many harvests it has left, and which animation the
// player plays for it. PlayerInteraction calls Interact() when the player harvests it.
public class InteractableResource : MonoBehaviour
{
    [Header("What this resource gives")]
    [Tooltip("Sent to the counter. Use \"apple\" or \"ore\" (matches ResourceCounter).")]
    public string resourceName = "apple";
    [Tooltip("How much is added each harvest.")]
    public int amountPerCollect = 1;
    [Tooltip("How many times this can be harvested before it's empty.")]
    public int usesRemaining = 1;

    [Header("Interaction")]
    [Tooltip("Animator trigger the player fires for this resource: \"PickFruit\" or \"GatherOre\".")]
    public string animationTrigger = "PickFruit";
    [Tooltip("Disappear once its uses run out.")]
    public bool destroyWhenEmpty = true;

    // The scene's single counter we report collected amounts to.
    private ResourceCounter counter;

    void Start()
    {
        // Grab the shared counter once at startup (FindFirstObjectByType is the Unity 6 API).
        counter = Object.FindFirstObjectByType<ResourceCounter>();
    }

    // Called when the player successfully harvests this resource.
    public void Interact()
    {
        // Already empty -> nothing to give.
        if (usesRemaining <= 0)
            return;

        if (counter != null)
            counter.AddResource(resourceName, amountPerCollect);

        usesRemaining--;

        // Used up: hide it. Deactivating also drops its collider out of the player's detection.
        if (usesRemaining <= 0 && destroyWhenEmpty)
            gameObject.SetActive(false);
    }
}
