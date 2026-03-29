using UnityEngine;
using UnityEngine.UI;

public class PotionPickup : MonoBehaviour
{
    [Header("Potion Configuration")]
    [SerializeField] private PotionType potionType;
    
    [Header("UI")]
    [SerializeField] private GameObject interactPromptPrefab;
    private GameObject interactPrompt;

    private bool playerInRange = false;
    private InventorySystem playerInventory;
    [Header("Sounds")]
    [SerializeField] private AudioClip pickUpSound;

    void Start()
    {

        // Get Inventory
        GameObject PlayerData = GameObject.FindWithTag("PlayerData");
        if (PlayerData != null)
        {
            playerInventory = PlayerData.GetComponent<InventorySystem>();
            if (playerInventory == null)
            {
                Debug.LogError("PotionPickup: No InventorySystem component found!");
            }
        } else
        {
            Debug.LogError("PotionPickup: No PlayerData found!");
        }

        if (interactPromptPrefab == null)
        {
            Debug.LogError("PotionPickup: interactPromptPrefab not assigned! Please assign the prefab.");
        }
        
        if (potionType == null)
        {
            Debug.LogError("PotionPickup: potionType not assigned! Please assign a PotionType ScriptableObject.");
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (playerInventory != null && potionType != null)
            {
                // Try to add potion to inventory
                bool added = playerInventory.AddPotion(potionType);
                
                if (added)
                {
                    // Play pickedup sound
                    if (pickUpSound != null)
                    {
                        AudioSource.PlayClipAtPoint(pickUpSound, transform.position);
                    }

                    // Successfully picked up - destroy the pickup
                    if (interactPrompt != null)
                    {
                        Destroy(interactPrompt);
                    }
                    Destroy(gameObject);
                }
                else
                {
                    // Inventory full - could play a "inventory full" sound here
                    Debug.Log("PotionPickup: Could not pick up potion - inventory might be full!");
                }
            }
        }
    }

    void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {   
            playerInRange = true;
            
            Debug.Log("Entered Potion range");
            
            if (interactPromptPrefab != null)
            {
                interactPrompt = Instantiate(interactPromptPrefab, new Vector3(this.transform.position.x, this.transform.position.y+1.5f, this.transform.position.z), this.transform.rotation);
            } else {
                Debug.LogError("PotionPickup: Interact Prompt prefab not assigned! " + this.gameObject.name);
            }
        }
    }

    void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            
            Debug.Log("Left Potion range");
            
            if (interactPrompt != null)
            {
                Destroy(interactPrompt);
            }
        }
    }
}
