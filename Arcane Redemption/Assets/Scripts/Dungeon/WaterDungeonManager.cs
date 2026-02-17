using UnityEngine;
using System.Collections;

public class WaterDungeonManager : MonoBehaviour
{
    [SerializeField] GameObject[] battleLockedDoors;
    [SerializeField] GameObject[] enemies;
    // [SerializeField] GameObject bridge;
    // [SerializeField] GameObject vinesWall2;
    [SerializeField] GameObject dungeonEntrance;
    [SerializeField] GameObject teleportDoor;
    [SerializeField] CharacterController characterController;    
    private PlayerController playerController;
    [SerializeField] GameObject drowningPrefab;
    private GameObject drowningEffect;
    private bool movedOrMoving;
    private bool entranceOpened;
    [SerializeField] float moveDuration;

    private bool[] doorsOpened = new bool[5];

    void Start()
    {
        // if (bridge == null)
        // {
        //     Debug.LogError("Fire dungeon manager can't find bridge!");
        // }

        if (characterController == null)
        {
            Debug.LogError("Fire dungeon manager can't find characterController!");
        }

        if (drowningPrefab == null)
        {
            Debug.LogError("Fire dungeon manager can't find drowningPrefab!");
        }
        
        // if (vinesWall2 == null)
        // {
        //     Debug.LogError("Fire dungeon manager can't find vinesWall2!");
        // }

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
            StartCoroutine(TweenPosition(battleLockedDoors[2], new Vector3(battleLockedDoors[2].transform.localPosition.x, -25, battleLockedDoors[2].transform.localPosition.z), 3));
            StartCoroutine(TweenPosition(battleLockedDoors[3], new Vector3(battleLockedDoors[3].transform.localPosition.x, -28, battleLockedDoors[3].transform.localPosition.z), 3));
            StartCoroutine(TweenPosition(battleLockedDoors[4], new Vector3(battleLockedDoors[4].transform.localPosition.x, -35, battleLockedDoors[4].transform.localPosition.z), 3));
            Debug.Log("Opening door");
        }

        // if (vinesWall2 == null & !entranceOpened)
        // {
        //     entranceOpened = true;
        //     teleportDoor.SetActive(true);
        //     // Start the tweening coroutine
        //     StartCoroutine(TweenPosition(dungeonEntrance, new Vector3(-28, 8, -12.5f), 10));
        //     dungeonEntrance.transform.position = new Vector3(-28, 8, -12.5f);
        // }
        
    }

    void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {   
            Debug.Log("Player fell in pit!");
            playerController = other.GetComponent<PlayerController>();
            
            Invoke("SpawnPlayerAtopPit", 2);
            drowningEffect = Instantiate(drowningPrefab, characterController.transform.position, drowningPrefab.transform.rotation);
            drowningEffect.transform.SetParent(other.transform);
            // playerController.canMove = false;
            playerController.gravity = 7.5f;


            Invoke("DestroyDrowningEffect", 2);
        }
    }

    void DestroyDrowningEffect()
    {
        Destroy(drowningEffect);
    }

    void SpawnPlayerAtopPit()
    {
        playerController.gravity = -9.81f;
        characterController.enabled = false;
        characterController.transform.position = new Vector3(45,-22, 90);
        characterController.enabled = true;
        // playerController.canMove = true;
    }
}
