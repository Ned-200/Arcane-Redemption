using UnityEngine;
using System.Collections;
using Unity.Cinemachine;
using TMPro;

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
    
    [Header("Cutscene")]
    [SerializeField] private GameObject CinematicCamera;
    [SerializeField] private GameObject[] birdgeCameras;
    [SerializeField] private float cinematicDuration = 5.0f;
    [SerializeField] private TextMeshProUGUI BossNameDisplay; // change within scene
    [SerializeField] private TextMeshProUGUI BossDescriptionDisplay;  // change within scene



    void Start()
    {
        if (characterController == null)
        {
            Debug.LogError("MainRegionManager: Can't find characterController!");
        }
        if (playerController == null)
        {
            Debug.LogError("MainRegionManager: Can't find playerController!");
        }

        if (environmentDeathEffectPrefab == null)
        {
            Debug.LogError("MainRegionManager: Can't find environmentDeathEffectPrefab!");
        }

        if (CinematicCamera == null)
        {
            Debug.LogError("MainRegionManager: CinematicCamera not assigned!");
        } else
        {
            playerController.canMove = false;
            Invoke(nameof(EnableCinematicCamera), 2.0f);
        }

        linearSoundDropoff = true;
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
                if (birdgeCameras[doorsOpened] != null) {
                    birdgeCameras[doorsOpened].SetActive(true);
                    playerController.canMove = false; // disable player movement
                    Invoke(nameof(DisableBridgeCamera), moveDuration);
                }
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

    void DisableBridgeCamera() {
        birdgeCameras[doorsOpened-1].SetActive(false);
        playerController.canMove = true; // re-enable player movement
    }

    void EnableCinematicCamera()
    {
        CinematicCamera.SetActive(true);
        StartCoroutine(FadeText(BossNameDisplay, 1, 1));
        StartCoroutine(FadeText(BossDescriptionDisplay, 1, 1.25f));
        Invoke(nameof(DisableCinematicCamera), cinematicDuration);
    }

    void DisableCinematicCamera()
    {
        CinematicCamera.SetActive(false);
        playerController.canMove = true; // re-enable player movement
        StartCoroutine(FadeText(BossNameDisplay, 0, 1));
        StartCoroutine(FadeText(BossDescriptionDisplay, 0, 0.75f));
    }

    private IEnumerator FadeText(TextMeshProUGUI text, float targetAlpha, float duration)
    {
        float currentAlpha = text.color.a;
        float startAlpha = text.color.a;
        float timeElapsed = 0.0f;

        while (timeElapsed < duration)
        {
            // Calculate the interpolation percentage (0 to 1)
            float t = timeElapsed / duration;

            // Interpolate the position
            currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            text.color = new Color(text.color.r, text.color.g, text.color.b, currentAlpha);
            
            // Increment time and wait for the next frame
            timeElapsed += Time.deltaTime;
            yield return null; // Wait until the next frame
        }

        // Ensure the object reaches the exact target position
        text.color = new Color(text.color.r, text.color.g, text.color.b, targetAlpha);
    }

}
