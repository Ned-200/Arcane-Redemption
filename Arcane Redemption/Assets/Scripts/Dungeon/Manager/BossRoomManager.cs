using UnityEngine;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.UI;
using TMPro;

public class BossRoomManager : DungeonManager
{
    [SerializeField] private Checkpoint cutsceneTrigger; 
    [SerializeField] private GameObject CinematicCamera;
    [SerializeField] private GameObject BossCamera;
    [SerializeField] private float moveYPosition = 75;
    private bool cutscenePlayed;
    private Animator playerAnim;
    [SerializeField] private TextMeshProUGUI BossNameDisplay; 
    [SerializeField] private TextMeshProUGUI BossDescriptionDisplay; 

    void Start()
    {
        if (CinematicCamera == null)
        {
            Debug.LogError("BossRoomManager: CinematicCamera not assigned!");
        }
        if (BossCamera == null)
        {
            Debug.LogError("BossRoomManager: BossCamera not assigned!");
        }

        playerController = characterController.GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("BossRoomManager: Could not find playerController from characterController component!");
        }

        playerAnim = characterController.GetComponent<Animator>();
        if (playerAnim == null)
        {
            Debug.LogError("BossRoomManager: Could not find playerAnim from characterController component!");
        }
    }


    void Update()
    {
        if (!cutscenePlayed && cutsceneTrigger.checkpointSet)
        {
            cutscenePlayed = true;
            CinematicCamera.SetActive(true);
            playerController.canMove = false; // stop player movement
            playerAnim.SetBool("isWalking", false);
            playerAnim.SetBool("isSprinting", false);

            if (battleLockedDoors.Length > 0) {
                StartCoroutine(TweenPosition(battleLockedDoors[0], new Vector3(battleLockedDoors[0].transform.position.x, moveYPosition, battleLockedDoors[0].transform.position.z), moveDuration));
                
                CinemachineImpulseSource impulseSource = battleLockedDoors[0].GetComponent<CinemachineImpulseSource>();
                if (impulseSource) {
                    impulseSource.GenerateImpulse(0.5f);
                }

                ParticleSystem particles = battleLockedDoors[0].GetComponent<ParticleSystem>();
                if (particles) {
                    particles.Play();
                }
            }
            
            Invoke(nameof(EnableBossCamera), moveDuration + 2.5f);
        }
    }

    void EnableBossCamera()
    {
        BossCamera.SetActive(true);
        CinematicCamera.SetActive(false);
        Invoke(nameof(RoarShake), 1);
        Invoke(nameof(DisableBossCamera), 4);
    }

    void RoarShake()
    {
        StartCoroutine(FadeText(BossNameDisplay, 1, 1));
        StartCoroutine(FadeText(BossDescriptionDisplay, 1, 1.25f));

        CinemachineImpulseSource impulseSource = BossCamera.transform.parent.GetComponent<CinemachineImpulseSource>();
        if (impulseSource) {
            impulseSource.GenerateImpulse(1.0f);
        }
    }

    void DisableBossCamera()
    {
        BossCamera.SetActive(false);
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
