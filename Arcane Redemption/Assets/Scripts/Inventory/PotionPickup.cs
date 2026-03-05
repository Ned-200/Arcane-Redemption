using UnityEngine;
using UnityEngine.UI;

public class PotionPickup : MonoBehaviour
{
    [Header("Potion Configuration")]
    [SerializeField] private PotionType potionType;
    
    [Header("UI")]
    private Image interactImage;

    private bool playerInRange = false;
    private InventorySystem playerInventory;

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


        GameObject canvas = GameObject.FindWithTag("MainCanvas");
        interactImage = canvas.transform.Find("InteractImage").GetComponent<Image>();
        if (interactImage == null)
        {
            Debug.LogError("PotionPickup: interactImage component not found!");
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
                        interactImage.enabled = false;
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
            
            if (interactImage != null)
            {
                interactImage.enabled = true;
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
            
            if (interactImage != null)
            {
                interactImage.enabled = false;
            }
        }
    }
}
