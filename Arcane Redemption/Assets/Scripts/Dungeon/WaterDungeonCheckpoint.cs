using UnityEngine;

public class WaterDungeonCheckpoint : Checkpoint
{
    [SerializeField] private WaterDungeonManager waterDungeonManager;

    protected override void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {   
            Debug.Log("Player reached checkpoint!");
            
            PlayerCharacter playerCharacter = other.GetComponent<PlayerCharacter>();
            if (playerCharacter != null)
            {
                playerCharacter.respawnPoint = spawnPoint;
            } else
            {
                Debug.LogError("Checkpoint cound not find player character!");
            }

            // Set Water Dungeon to respawn player if they drown
            waterDungeonManager.checkpoint = spawnPoint;
            
            boxCollider.enabled = false;
        }

    }
}
