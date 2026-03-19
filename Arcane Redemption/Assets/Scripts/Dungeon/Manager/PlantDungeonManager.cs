using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class PlantDungeonManager : DungeonManager
{
    [SerializeField] PlantBridge finalPlantBridge;
    [SerializeField] GameObject dungeonExit;
    private bool exitOpened;

    void Start()
    {
        if (finalPlantBridge == null)
        {
            Debug.LogError("PlantDungeonManager: finalPlantBridge not assigned!");
        }

        if (dungeonExit == null)
        {
            Debug.LogError("PlantDungeonManager: dungeonExit not assigned!");
        }
        
    }

    private void Update() {
        if (finalPlantBridge.activated & !exitOpened)
        {
            exitOpened = true;
            // Start the tweening coroutine
            StartCoroutine(TweenPosition(dungeonExit, new Vector3(dungeonExit.transform.localPosition.x, 0, dungeonExit.transform.localPosition.z), 10));
            dungeonExit.transform.localPosition = new Vector3(dungeonExit.transform.localPosition.x, dungeonExit.transform.localPosition.y+8, dungeonExit.transform.localPosition.z);
            
            CinemachineImpulseSource impulseSource = dungeonExit.GetComponent<CinemachineImpulseSource>();
            if (impulseSource) {
                dungeonExit.GetComponent<CinemachineImpulseSource>().GenerateImpulse(0.5f);
            }
        }
        
    }
}
