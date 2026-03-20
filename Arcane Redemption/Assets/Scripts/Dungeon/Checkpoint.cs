using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    protected private BoxCollider boxCollider;
    protected private Transform spawnPoint;
    public bool checkpointSet;

    protected void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            Debug.LogError("Checkpoint doesn't have Box Collider!");
        }

        spawnPoint = gameObject.transform.Find("SpawnPoint");
        if (spawnPoint == null)
        {
            Debug.LogError("Checkpoint doesn't have spawnPoint!");
        }
    }

    protected virtual void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player") && !checkpointSet)
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

            boxCollider.enabled = false;
            checkpointSet = true;
        }
    }
}
