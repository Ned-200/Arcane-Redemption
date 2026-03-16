using UnityEngine;

public class DungeonCheckpoint : Checkpoint
{
    [SerializeField] private DungeonManager DungeonManager;

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
            DungeonManager.checkpoint = spawnPoint;
            
            boxCollider.enabled = false;
        }

    }
}
