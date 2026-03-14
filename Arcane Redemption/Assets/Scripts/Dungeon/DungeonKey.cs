using UnityEngine;
using UnityEngine.UI;

public class DungeonKey : MonoBehaviour
{
    private bool playerInRange = false;

    [Header("UI")]
    [SerializeField] private GameObject interactPromptPrefab;
    private GameObject keyUI;
    private GameObject interactPrompt;
    public bool pickedUp;


    void Start()
    {
        // Get Key UI
        GameObject canvas = GameObject.FindWithTag("MainCanvas");
        GameObject inventoryMenu = canvas.transform.Find("InventoryMenu").gameObject;
        if (inventoryMenu != null)
        {
            keyUI = inventoryMenu.transform.Find("Key").gameObject;
            if (keyUI == null)
            {
                Debug.LogError("DungeonKey: Could not find keyUI! Check naming and children!");
            }
        } else {
            Debug.LogError("DungeonKey: Could not find InventoryMenu! Check naming and children!");
        }
        
        // Get interactPrompt prefab
        if (interactPromptPrefab == null)
        {
            Debug.LogError("DungeonKey: interactPromptPrefab not assigned! Please assign the prefab.");
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            // Remove prompt
            if (interactPrompt != null)
            {
                Destroy(interactPrompt);
            }

            // Hide
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                rend.enabled = false;
            } else
            {
                Debug.LogError("DungeonKey: Could not find own mesh renderer!");
            }
            
            // Show Key in UI
            keyUI.GetComponent<Image>().enabled = true;

            pickedUp = true;
            
        }
    }

    void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player") && !pickedUp)
        {   
            playerInRange = true;
            Debug.Log("Entered Key range");
            if (interactPromptPrefab != null)
            {
                interactPrompt = Instantiate(interactPromptPrefab, new Vector3(this.transform.position.x, this.transform.position.y+1.5f, this.transform.position.z), this.transform.rotation);
            } else {
                Debug.LogError("PotionPickup: Interact Prompt prefab not assigned! " + this.gameObject.name);
            }
        }
    }

    protected void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player") && !pickedUp)
        {
            playerInRange = false;
            Debug.Log("Left Key range");
            Destroy(interactPrompt);
        }
    }
}
