using UnityEngine;

public class FinalChoice : MonoBehaviour
{

    [Header("Sounds")]
    [SerializeField] private AudioSource ambienceSource;
    [SerializeField] private AudioClip newAmbience;

    [Header("UI")]
    [SerializeField] private GameObject finalImage;
    
    [Header("Prompt")]
    [SerializeField] private GameObject interactPromptPrefab;
    [SerializeField] private Transform promptPosition;
    private GameObject interactPrompt;
    private bool playerInRange = false;
    private bool choiceMade;

    [Header("Player")]
    protected GameObject player;
    protected PlayerController playerController;
    protected Animator playerAnim;


    void Start()
    {
        // Get interactPrompt prefab
        if (interactPromptPrefab == null)
        {
            Debug.LogError("TeleportDoor: interactPromptPrefab not assigned! Please assign the prefab. "  + this.gameObject.name);
        }

        // Get promptPosition object
        if (promptPosition == null)
        {
            Debug.LogError("TeleportDoor: promptPosition not assigned! Please assign the prefab. "  + this.gameObject.name);
        }

        // Get Player
        player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            // Get playerController
            playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("FinalChoice: playerController NOT FOUND! Check Player Hierarchy.");
            }
            // Get playerAnim
            playerAnim = player.GetComponent<Animator>();
            if (playerAnim == null)
            {
                Debug.LogError("NPC: playerAnim NOT FOUND! Check Player Hierarchy.");
            }

        } else
        {
            Debug.LogError("FinalChoice: player NOT FOUND! Check tag.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !choiceMade)
        {
            choiceMade = true;
            ambienceSource.clip = newAmbience;
            ambienceSource.Play();
            finalImage.SetActive(true);
            
            // disable player movement
            playerController.canMove = false;
            playerAnim.SetBool("isWalking", false);
            playerAnim.SetBool("isSprinting", false);
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {   
            playerInRange = true;
            Debug.Log("Entered Door range");
            if (interactPromptPrefab != null)
            {
                interactPrompt = Instantiate(interactPromptPrefab, new Vector3(promptPosition.position.x, promptPosition.position.y, promptPosition.position.z), promptPosition.rotation);
            } else {
                Debug.LogError("FinalChoice: Interact Prompt prefab not assigned! " + this.gameObject.name);
            }
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("Left Door range");
            Destroy(interactPrompt);
        }
    }
}
