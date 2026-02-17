using UnityEngine;

public class PotionPickup : MonoBehaviour
{
    [Header("Potion Configuration")]
    [SerializeField] private PotionType potionType;
    
    [Header("UI")]
    [SerializeField] private GameObject interactImage;

    private bool playerInRange = false;
    private InventorySystem playerInventory;

    void Start()
    {
        if (interactImage == null)
        {
            Debug.LogError("PotionPickup: interactImage not assigned!");
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
                    // Successfully picked up - destroy the pickup
                    if (interactImage != null)
                    {
                        interactImage.SetActive(false);
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
            
            // Get the player's inventory system
            playerInventory = other.GetComponent<InventorySystem>();
            
            if (playerInventory == null)
            {
                Debug.LogError("PotionPickup: Player does not have an InventorySystem component!");
            }
            
            Debug.Log("Entered Potion range");
            
            if (interactImage != null)
            {
                interactImage.SetActive(true);
            }
        }
    }

    void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            playerInventory = null;
            
            Debug.Log("Left Potion range");
            
            if (interactImage != null)
            {
                interactImage.SetActive(false);
            }
        }
    }
}
