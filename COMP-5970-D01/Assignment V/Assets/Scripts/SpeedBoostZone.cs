using UnityEngine;

public class SpeedBoostZone : MonoBehaviour
{
    float boostSpeed = 15f;
    float boostDuration = 2f;

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag("Player")) 
        {
            PlayerMovement playerMovement = collider.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.ActivateSpeedBoost(boostSpeed, boostDuration);
            }
        }
    }
}
