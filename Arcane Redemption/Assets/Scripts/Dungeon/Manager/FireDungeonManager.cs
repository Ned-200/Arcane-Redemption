using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class FireDungeonManager : DungeonManager
{
    [SerializeField] GameObject bridge;
    [SerializeField] GameObject vinesWall2;
    [SerializeField] GameObject dungeonEntrance;
    private bool entranceOpened;

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

        if (environmentDeathEffectPrefab == null)
        {
            Debug.LogError("Fire dungeon manager can't find environmentDeathEffectPrefab!");
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

    private void Update() {
        if (enemies[0] == null & enemies[1] == null & !doorsOpened[0])
        {
            doorsOpened[0] = true;
            StartCoroutine(TweenPosition(battleLockedDoors[0], new Vector3(battleLockedDoors[0].transform.localPosition.x, 8, battleLockedDoors[0].transform.localPosition.z), 3));
            StartCoroutine(TweenPosition(battleLockedDoors[1], new Vector3(battleLockedDoors[1].transform.localPosition.x, 8, battleLockedDoors[1].transform.localPosition.z), 3));
            
            CinemachineImpulseSource impulseSource = battleLockedDoors[0].GetComponent<CinemachineImpulseSource>();
            if (impulseSource) {
                battleLockedDoors[0].GetComponent<CinemachineImpulseSource>().GenerateImpulse(0.5f);
            }
            
            Debug.Log("Opening doors");
        }

        if (enemies[2] == null & enemies[3] == null & !doorsOpened[1])
        {
            doorsOpened[1] = true;
            
            StartCoroutine(TweenPosition(battleLockedDoors[2], new Vector3(battleLockedDoors[2].transform.localPosition.x, 8, battleLockedDoors[2].transform.localPosition.z), 3));
            
            CinemachineImpulseSource impulseSource = battleLockedDoors[2].GetComponent<CinemachineImpulseSource>();
            if (impulseSource) {
                battleLockedDoors[2].GetComponent<CinemachineImpulseSource>().GenerateImpulse(0.5f);
            }

            Debug.Log("Opening door");
        }

        if (enemies[4] == null & enemies[5] == null & enemies[6] == null & enemies[7] == null & enemies[8] == null & enemies[9] == null & !doorsOpened[2])
        {
            doorsOpened[2] = true;
            StartCoroutine(TweenPosition(battleLockedDoors[3], new Vector3(battleLockedDoors[3].transform.localPosition.x, 8, battleLockedDoors[3].transform.localPosition.z), 3));
            
            CinemachineImpulseSource impulseSource = battleLockedDoors[3].GetComponent<CinemachineImpulseSource>();
            if (impulseSource) {
                battleLockedDoors[3].GetComponent<CinemachineImpulseSource>().GenerateImpulse(0.5f);
            }

            Debug.Log("Opening door");
        }

        if (enemies[10] == null & enemies[11] == null & !movedOrMoving)
        {
            movedOrMoving = true;
            // Start the tweening coroutine
            StartCoroutine(TweenPosition(bridge, new Vector3(17,-19,-60), moveDuration));
            
            CinemachineImpulseSource impulseSource = bridge.GetComponent<CinemachineImpulseSource>();
            if (impulseSource) {
                bridge.GetComponent<CinemachineImpulseSource>().GenerateImpulse(0.5f);
            }

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
}
