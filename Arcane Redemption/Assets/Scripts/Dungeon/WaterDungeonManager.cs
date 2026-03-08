using UnityEngine;
using System.Collections;

public class WaterDungeonManager : MonoBehaviour
{
    [SerializeField] GameObject[] battleLockedDoors;
    [SerializeField] GameObject[] enemies;
    [SerializeField] public Transform checkpoint;
    [SerializeField] Collider exitFireWall;
    [SerializeField] GameObject dungeonExit;
    [SerializeField] GameObject teleportDoor;
    [SerializeField] CharacterController characterController;
    private PlayerController playerController;
    [SerializeField] GameObject drowningPrefab;
    private GameObject drowningEffect;
    private bool movedOrMoving;
    private bool drowning;
    private bool exitOpened;
    [SerializeField] float moveDuration;

    private bool[] doorsOpened = new bool[5];

    void Start()
    {
        if (characterController == null)
        {
            Debug.LogError("Fire dungeon manager can't find characterController!");
        }

        if (drowningPrefab == null)
        {
            Debug.LogError("Fire dungeon manager can't find drowningPrefab!");
        }
        
        if (exitFireWall == null)
        {
            Debug.LogError("Fire dungeon manager can't find exitFireWall!");
        }

        if (dungeonExit == null)
        {
            Debug.LogError("Fire dungeon manager can't find dungeonExit!");
        }
        
        if (teleportDoor == null)
        {
            Debug.LogError("Fire dungeon manager can't find teleportDoor!");
        }
    }

    IEnumerator TweenPosition(GameObject movingObject, Vector3 targetPos, float duration)
    {
        Vector3 startPosition = movingObject.transform.localPosition;
        float timeElapsed = 0.0f;

        while (timeElapsed < duration)
        {
            // Calculate the interpolation percentage (0 to 1)
            float t = timeElapsed / duration;

            // Interpolate the position
            movingObject.transform.localPosition = Vector3.Lerp(startPosition, targetPos, t);
            
            // Increment time and wait for the next frame
            timeElapsed += Time.deltaTime;
            yield return null; // Wait until the next frame
        }

        // Ensure the object reaches the exact target position
        movingObject.transform.localPosition = targetPos;
    }

    private void Update() {
        if (enemies[0] == null & !doorsOpened[0])
        {
            doorsOpened[0] = true;
            StartCoroutine(TweenPosition(battleLockedDoors[0], new Vector3(battleLockedDoors[0].transform.localPosition.x, 0, battleLockedDoors[0].transform.localPosition.z), 3));
            Debug.Log("Opening doors");
        }

        if (enemies[1] == null & enemies[2] == null & !doorsOpened[1])
        {
            doorsOpened[1] = true;
            StartCoroutine(TweenPosition(battleLockedDoors[1], new Vector3(battleLockedDoors[1].transform.localPosition.x, 0, battleLockedDoors[1].transform.localPosition.z), 3));
            Debug.Log("Opening door");
        }

        if (enemies[3] == null & !doorsOpened[2])
        {
            doorsOpened[2] = true;
            StartCoroutine(TweenPosition(battleLockedDoors[2], new Vector3(battleLockedDoors[2].transform.localPosition.x, -23, battleLockedDoors[2].transform.localPosition.z), 2));
            StartCoroutine(TweenPosition(battleLockedDoors[3], new Vector3(battleLockedDoors[3].transform.localPosition.x, -26, battleLockedDoors[3].transform.localPosition.z), 3));
            StartCoroutine(TweenPosition(battleLockedDoors[4], new Vector3(battleLockedDoors[4].transform.localPosition.x, -33, battleLockedDoors[4].transform.localPosition.z), 4));
            Debug.Log("Opening door");
        }

        // Dungeon Exit
        if (!exitFireWall.enabled & !exitOpened)
        {
            exitOpened = true;
            teleportDoor.SetActive(true);
            // Start the tweening coroutine
            StartCoroutine(TweenPosition(dungeonExit, new Vector3(dungeonExit.transform.localPosition.x, dungeonExit.transform.localPosition.y+12, dungeonExit.transform.localPosition.z), 10));
            dungeonExit.transform.localPosition = new Vector3(dungeonExit.transform.localPosition.x, dungeonExit.transform.localPosition.y+12, dungeonExit.transform.localPosition.z);
        }
        
    }

    void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player") && !drowning)
        {   
            drowning = true;
            Debug.Log("Player fell in pit!");
            playerController = other.GetComponent<PlayerController>();
            
            Invoke(nameof(SpawnPlayerAtopPit), 2);
            drowningEffect = Instantiate(drowningPrefab, characterController.transform.position, drowningPrefab.transform.rotation);
            drowningEffect.transform.SetParent(other.transform);

            playerController.playerAnim.SetBool("Drowned", true);
            // playerController.canMove = false;
            playerController.gravity = 7.5f;
        }
    }

    void SpawnPlayerAtopPit()
    {
        playerController.gravity = -9.81f;
        characterController.enabled = false;
        characterController.transform.position = checkpoint.position;
        characterController.enabled = true;
        // playerController.canMove = true;
        
        playerController.playerAnim.SetBool("Drowned", false);
        drowning = false;
    }
}
