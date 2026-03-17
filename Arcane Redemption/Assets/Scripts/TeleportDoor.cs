using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TeleportDoor : MonoBehaviour
{
    protected private GameObject playerDataObject;
    protected private PlayerData playerData;
    [SerializeField] protected string destinationSceneName;

    [Header("UI")]
    [SerializeField] private GameObject interactPromptPrefab;
    private GameObject interactPrompt;
    [SerializeField] private Transform promptPosition;
    [SerializeField] protected GameObject LoadingUI;
    protected private bool teleporting = false;
    protected private bool playerInRange = false;

    protected void Start()
    {
        // Get player data
        playerDataObject = GameObject.FindWithTag("PlayerData");
        if (playerDataObject != null)
        {
            playerData = playerDataObject.GetComponent<PlayerData>();
        } else
        {
            Debug.LogError("No Player Data in Scene! Check Tag!");
        }

        // Get interactPrompt prefab
        if (interactPromptPrefab == null)
        {
            Debug.LogError("TeleportDoor: interactPromptPrefab not assigned! Please assign the prefab.");
        }
    }

    protected virtual void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !teleporting)
        {
            // Show Loading Screen
            LoadingUI.SetActive(true);

            Invoke(nameof(Teleport), 1.5f);
        }
    }

    protected void Teleport()
    {
        Debug.Log("Teleporting Player to new Scene");

        if (playerData != null) {
            playerData.lastScene = SceneManager.GetActiveScene().name;
        }


        SceneManager.LoadScene(destinationSceneName, LoadSceneMode.Single);
        teleporting = true;
    }

    protected virtual void OnTriggerEnter(Collider collision)
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
                Debug.LogError("TeleportDoor: Interact Prompt prefab not assigned! " + this.gameObject.name);
            }
        }
    }

    protected virtual void OnTriggerExit(Collider collision)
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
