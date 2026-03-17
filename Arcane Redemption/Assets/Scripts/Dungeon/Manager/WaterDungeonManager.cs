using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class WaterDungeonManager : DungeonManager
{
    [SerializeField] Collider exitFireWall;
    [SerializeField] GameObject dungeonExit;
    private bool exitOpened;

    private bool[] doorsOpened = new bool[5];

    void Start()
    {
        if (characterController == null)
        {
            Debug.LogError("Fire dungeon manager can't find characterController!");
        }

        if (environmentDeathEffectPrefab == null)
        {
            Debug.LogError("Fire dungeon manager can't find environmentDeathEffectPrefab!");
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

    private void Update() {
        if (enemies[0] == null & !doorsOpened[0])
        {
            doorsOpened[0] = true;
            StartCoroutine(TweenPosition(battleLockedDoors[0], new Vector3(battleLockedDoors[0].transform.localPosition.x, 0, battleLockedDoors[0].transform.localPosition.z), moveDuration));
            Debug.Log("Opening doors");

            CinemachineImpulseSource impulseSource = battleLockedDoors[0].GetComponent<CinemachineImpulseSource>();
            if (impulseSource) {
                battleLockedDoors[0].GetComponent<CinemachineImpulseSource>().GenerateImpulse(0.5f);
            }

        }

        if (enemies[1] == null & enemies[2] == null & !doorsOpened[1])
        {
            doorsOpened[1] = true;
            StartCoroutine(TweenPosition(battleLockedDoors[1], new Vector3(battleLockedDoors[1].transform.localPosition.x, 0, battleLockedDoors[1].transform.localPosition.z), moveDuration));
            Debug.Log("Opening door");

            CinemachineImpulseSource impulseSource = battleLockedDoors[1].GetComponent<CinemachineImpulseSource>();
            if (impulseSource) {
                battleLockedDoors[1].GetComponent<CinemachineImpulseSource>().GenerateImpulse(0.5f);
            }

        }

        if (enemies[3] == null & !doorsOpened[2])
        {
            doorsOpened[2] = true;
            StartCoroutine(TweenPosition(battleLockedDoors[2], new Vector3(battleLockedDoors[2].transform.localPosition.x, -23, battleLockedDoors[2].transform.localPosition.z), moveDuration-1.0f));
            StartCoroutine(TweenPosition(battleLockedDoors[3], new Vector3(battleLockedDoors[3].transform.localPosition.x, -26, battleLockedDoors[3].transform.localPosition.z), moveDuration));
            StartCoroutine(TweenPosition(battleLockedDoors[4], new Vector3(battleLockedDoors[4].transform.localPosition.x, -33, battleLockedDoors[4].transform.localPosition.z), moveDuration+1.0f));
            
            CinemachineImpulseSource impulseSource = battleLockedDoors[2].GetComponent<CinemachineImpulseSource>();
            if (impulseSource) {
                battleLockedDoors[2].GetComponent<CinemachineImpulseSource>().GenerateImpulse(0.5f);
            }

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
}
