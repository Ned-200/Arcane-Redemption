using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private BoxCollider boxCollider;
    private Transform spawnPoint;
    [SerializeField] private WaterDungeonManager waterDungeonManager;

    void Start()
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

    void Update()
    {
        
    }

    void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {   
            Debug.Log("Player reached checkpoint!");
            waterDungeonManager.checkpoint = spawnPoint;
            boxCollider.enabled = false;
        }
    }
}
