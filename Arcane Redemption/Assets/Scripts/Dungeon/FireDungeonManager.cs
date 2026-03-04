using UnityEngine;
using System.Collections;

public class FireDungeonManager : MonoBehaviour
{
    [SerializeField] GameObject[] battleLockedDoors;
    [SerializeField] GameObject[] enemies;
    [SerializeField] GameObject bridge;
    [SerializeField] GameObject vinesWall2;
    [SerializeField] GameObject dungeonEntrance;
    [SerializeField] GameObject teleportDoor;
    [SerializeField] CharacterController characterController;    
    private PlayerController playerController;
    [SerializeField] GameObject burningPrefab;
    private GameObject burningEffect;
    private bool movedOrMoving;
    private bool entranceOpened;
    [SerializeField] float moveDuration;

    private bool[] doorsOpened = new bool[5];

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
        
        if (vinesWall2 == null)
        {
            Debug.LogError("Fire dungeon manager can't find vinesWall2!");
        }

        if (dungeonEntrance == null)
        {
            Debug.LogError("Fire dungeon manager can't find dungeonEntrance!");
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
        if (enemies[0] == null & enemies[1] == null & !doorsOpened[0])
        {
            doorsOpened[0] = true;
            StartCoroutine(TweenPosition(battleLockedDoors[0], new Vector3(battleLockedDoors[0].transform.localPosition.x, 8, battleLockedDoors[0].transform.localPosition.z), 3));
            StartCoroutine(TweenPosition(battleLockedDoors[1], new Vector3(battleLockedDoors[1].transform.localPosition.x, 8, battleLockedDoors[1].transform.localPosition.z), 3));
            Debug.Log("Opening doors");
        }

        if (enemies[2] == null & enemies[3] == null & !doorsOpened[1])
        {
            doorsOpened[1] = true;
            StartCoroutine(TweenPosition(battleLockedDoors[2], new Vector3(battleLockedDoors[2].transform.localPosition.x, 8, battleLockedDoors[2].transform.localPosition.z), 3));
            Debug.Log("Opening door");
        }

        if (enemies[4] == null & enemies[5] == null & enemies[6] == null & enemies[7] == null & enemies[8] == null & enemies[9] == null & !doorsOpened[2])
        {
            doorsOpened[2] = true;
            StartCoroutine(TweenPosition(battleLockedDoors[3], new Vector3(battleLockedDoors[3].transform.localPosition.x, 8, battleLockedDoors[3].transform.localPosition.z), 3));
            Debug.Log("Opening door");
        }

        if (enemies[10] == null & enemies[11] == null & !movedOrMoving)
        {
            movedOrMoving = true;
            // Start the tweening coroutine
            StartCoroutine(TweenPosition(bridge, new Vector3(17,-19,-60), moveDuration));
            bridge.transform.position = new Vector3(17,-19,-60);
        }

        if (vinesWall2 == null & !entranceOpened)
        {
            entranceOpened = true;
            teleportDoor.SetActive(true);
            // Start the tweening coroutine
            StartCoroutine(TweenPosition(dungeonEntrance, new Vector3(dungeonEntrance.transform.localPosition.x, 0, dungeonEntrance.transform.localPosition.z), 10));
            dungeonEntrance.transform.localPosition = new Vector3(dungeonEntrance.transform.localPosition.x, 0, dungeonEntrance.transform.localPosition.z);
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
