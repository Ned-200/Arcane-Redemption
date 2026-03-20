using UnityEngine;

public class DungeonCheckpoint : Checkpoint
{
    [SerializeField] private DungeonManager DungeonManager;

    protected override void OnTriggerEnter(Collider collision)
    {
        base.OnTriggerEnter(collision);

        GameObject other = collision.gameObject;
        if (other.CompareTag("Player"))
        {   
            // Set Dungeon Manager Checkpoint to respawn player if they drown/burn
            DungeonManager.checkpoint = spawnPoint;
        }

    }
}
