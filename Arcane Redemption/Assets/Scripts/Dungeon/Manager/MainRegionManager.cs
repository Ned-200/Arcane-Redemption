using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class MainRegionManager : DungeonManager
{
    [Header("Enemies")]
    [SerializeField] int[] enemiesPerBridge;
    private int enemiesDefeatedPerBridge;
    private int enemiesDefeated;
    private int doorsOpened;
    
    [Header("Bridges/Doors")]
    [SerializeField] private int bridgeElevation = 35;
    [SerializeField] DungeonKeyDoor dungeonKeyDoor;


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
    }

    private void Update() {
        if (doorsOpened < enemiesPerBridge.Length) { // if not all doors are open
            enemiesDefeatedPerBridge = 0;
            for(int i = enemiesDefeated; i < enemiesDefeated+enemiesPerBridge[doorsOpened]; i++) // for each enemy in current enemiesPerBridge
            {
                if (enemies[i] == null)
                {
                    enemiesDefeatedPerBridge++; // increase current bridge kill counter
                }
            }
            if (enemiesDefeatedPerBridge == enemiesPerBridge[doorsOpened]) // all enemies in this bridge defeated
            {
                enemiesDefeated += enemiesPerBridge[doorsOpened];
                StartCoroutine(TweenPosition(battleLockedDoors[doorsOpened], new Vector3(battleLockedDoors[doorsOpened].transform.localPosition.x, battleLockedDoors[doorsOpened].transform.localPosition.y+bridgeElevation, battleLockedDoors[doorsOpened].transform.localPosition.z), moveDuration));
                Debug.Log("Opening door " + doorsOpened);
                
                CinemachineImpulseSource impulseSource = battleLockedDoors[doorsOpened].GetComponent<CinemachineImpulseSource>();
                if (impulseSource) {
                    battleLockedDoors[doorsOpened].GetComponent<CinemachineImpulseSource>().GenerateImpulse(0.5f);
                }

                doorsOpened++; // move to next bridge
            }
        }

        if (dungeonKeyDoor.movedOrMoving && !teleportDoor.activeSelf)
        {
            teleportDoor.SetActive(true);
        }
    }

}
