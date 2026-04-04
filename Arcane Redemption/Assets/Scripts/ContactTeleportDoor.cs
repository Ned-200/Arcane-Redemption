using UnityEngine;

public class ContactTeleportDoor : TeleportDoor
{
    protected override void Start()
    {
        // Get player data
        playerDataObject = GameObject.FindWithTag("PlayerData");
        if (playerDataObject != null)
        {
            playerData = playerDataObject.GetComponent<PlayerData>();
        } else
        {
            Debug.LogError("No Player Data in Scene! Check Tag!");
        }
    }

    protected override void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {   
            playerInRange = true;
            
            // Show Loading Screen
            LoadingUI.SetActive(true);

            Invoke(nameof(Teleport), 1.5f);
        }
    }
}
