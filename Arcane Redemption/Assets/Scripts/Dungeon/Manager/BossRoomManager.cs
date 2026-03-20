using UnityEngine;
using Unity.Cinemachine;

public class BossRoomManager : DungeonManager
{
    [SerializeField] private Checkpoint cutsceneTrigger; 
    [SerializeField] private GameObject CinematicCamera;
    [SerializeField] private GameObject BossCamera;
    [SerializeField] private float moveYPosition = 75;
    private bool cutscenePlayed;
    private Animator playerAnim;


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

            StartCoroutine(TweenPosition(battleLockedDoors[0], new Vector3(battleLockedDoors[0].transform.position.x, moveYPosition, battleLockedDoors[0].transform.position.z), moveDuration));
            
            CinemachineImpulseSource impulseSource = battleLockedDoors[0].GetComponent<CinemachineImpulseSource>();
            if (impulseSource) {
                impulseSource.GenerateImpulse(0.5f);
            }

            ParticleSystem particles = battleLockedDoors[0].GetComponent<ParticleSystem>();
            if (particles) {
                particles.Play();
            }
            
            Invoke(nameof(EnableBossCamera), moveDuration + 1.0f);
        }
    }

    void EnableBossCamera()
    {
        BossCamera.SetActive(true);
        CinematicCamera.SetActive(false);

        CinemachineImpulseSource impulseSource = battleLockedDoors[0].GetComponent<CinemachineImpulseSource>();
        if (impulseSource) {
            impulseSource.GenerateImpulse(0.5f);
        }

        Invoke(nameof(DisableBossCamera), 4);
    }

    void DisableBossCamera()
    {
        BossCamera.SetActive(false);
        playerController.canMove = true; // re-enable player movement
    }
}
