using UnityEngine;
using System.Collections;

public class FireDungeonManager : MonoBehaviour
{
    [SerializeField] GameObject[] battleLockedDoors;
    [SerializeField] GameObject[] enemies;
    [SerializeField] GameObject bridge;
    [SerializeField] CharacterController characterController;    
    private PlayerController playerController;
    [SerializeField] GameObject burningPrefab;
    private GameObject burningEffect;
    private bool movedOrMoving;
    
    private Vector3 targetPosition;
    [SerializeField] float moveDuration;

    void Start()
    {
        if (bridge == null)
        {
            Debug.LogError("Fire dungeon manager can't find bridge!");
        }

        if (characterController == null)
        {
            Debug.LogError("Fire dungeon manager can't find characterController!");
        }

        if (burningPrefab == null)
        {
            Debug.LogError("Fire dungeon manager can't find burningPrefab!");
        }

        targetPosition = new Vector3(58,-16.9f,16);
    }

    IEnumerator TweenPosition(Vector3 targetPos, float duration)
    {
        Vector3 startPosition = bridge.transform.position;
        float timeElapsed = 0.0f;

        while (timeElapsed < duration)
        {
            // Calculate the interpolation percentage (0 to 1)
            float t = timeElapsed / duration;

            // Interpolate the position
            bridge.transform.position = Vector3.Lerp(startPosition, targetPos, t);
            
            // Increment time and wait for the next frame
            timeElapsed += Time.deltaTime;
            yield return null; // Wait until the next frame
        }

        // Ensure the object reaches the exact target position
        bridge.transform.position = targetPos;
    }

    private void Update() {
        if (enemies[0] == null & enemies[1] == null)
        {
            Destroy(battleLockedDoors[0]);
            Destroy(battleLockedDoors[1]);
        }

        if (enemies[2] == null & enemies[3] == null)
        {
            Destroy(battleLockedDoors[2]);
        }

        if (enemies[4] == null & enemies[5] == null & enemies[6] == null & enemies[7] == null & enemies[8] == null & enemies[9] == null)
        {
            Destroy(battleLockedDoors[3]);
        }

        if (enemies[10] == null & enemies[11] == null & !movedOrMoving)
        {
            movedOrMoving = true;
            // Start the tweening coroutine
            StartCoroutine(TweenPosition(targetPosition, moveDuration));
            bridge.transform.position = new Vector3(58,-17,16);
        }
        
    }

    void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {   
            Debug.Log("Player fell in pit!");
            playerController = other.GetComponent<PlayerController>();
            
            Invoke("SpawnPlayerAtopPit", 2);
            burningEffect = Instantiate(burningPrefab, characterController.transform.position, burningPrefab.transform.rotation);
            playerController.canMove = false;

            Invoke("DestroyBurningEffect", 3);
        }
    }

    void DestroyBurningEffect()
    {
        Destroy(burningEffect);
    }

    void SpawnPlayerAtopPit()
    {
        characterController.enabled = false;
        characterController.transform.position = new Vector3(60,0,18);
        characterController.enabled = true;
        playerController.canMove = true;
    }
}
