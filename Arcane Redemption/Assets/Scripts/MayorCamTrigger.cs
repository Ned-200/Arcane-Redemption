using UnityEngine;
using TMPro;

public class MayorCamTrigger : MonoBehaviour
{
    private GameObject mayorCamera;
    private bool cutsceneTriggered = false;
    private GameObject player;    
    private PlayerController playerController;
    private Animator playerAnim;

    [Header("Text")]
    [SerializeField] private GameObject DialogueBox;
    [SerializeField] private TextMeshProUGUI textComponent;
    [SerializeField] private GameObject skipBox;
    
    void Start()
    {
        Transform mayorMesh = GameObject.Find("MayorNPC").transform.Find("NPCMesh");
        if (mayorMesh != null) {
            mayorCamera = mayorMesh.Find("CinemachineCamera").gameObject;
        } else {
            Debug.LogError("MayorCamTrigger: Can't find mayor's NPCMesh, so could not get camera within!");
        }

        // Get Player  
        player = GameObject.FindWithTag("Player");
        if (player != null) {
            
            // Get playerController
            playerController = player.GetComponent<PlayerController>();
            if (playerController == null) {
                Debug.LogError("NPC_Character: playerController NOT FOUND BY NPC! Check Player Hierarchy.");
            }
            // Get playerAnim
            playerAnim = player.GetComponent<Animator>();
            if (playerAnim == null) {
                Debug.LogError("NPC: playerAnim NOT FOUND! Check Player Hierarchy.");
            }

        } else {   
            Debug.LogError("NPC_Character: PLAYER NOT FOUND BY NPC! Check Player Tag.");
        }
    }

    void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player") && !cutsceneTriggered)
        {   
            cutsceneTriggered = true;
            Debug.Log("Entered MayorCamTrigger range");

            // Enable text box
            textComponent.text = "Hey! Over here!";
            DialogueBox.SetActive(true);
            skipBox.SetActive(false);

            // Disable player movement
            playerController.canMove = false;
            playerAnim.SetBool("isWalking", false);
            playerAnim.SetBool("isSprinting", false);

            // Enable mayor camera        
            if (mayorCamera != null) {
                mayorCamera.SetActive(true);
                Invoke(nameof(DisableMayorCam), 3);
            } else {
                Debug.LogError("MayorCamTrigger: Can't find mayorCamera gameobject!");
            }

        } else
        {
            Debug.LogError("MayorCamTrigger: Can't find mayorCamera gameobject!");
        }
    }

    void DisableMayorCam()
    {

        // Enable player movement
        playerController.canMove = true;

        // Disable text box
        textComponent.text = string.Empty;
        DialogueBox.SetActive(false);
        skipBox.SetActive(true);

        // Disable mayor camera
        if (mayorCamera != null) {
            mayorCamera.SetActive(false);
        } else {
            Debug.LogError("MayorCamTrigger: Can't find mayorCamera gameobject!");
        }
    }

    void Update()
    {
        
    }
}
