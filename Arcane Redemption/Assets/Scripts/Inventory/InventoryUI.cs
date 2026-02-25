using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    private PlayerController playerController;
    private InventorySystem playerInventory;
    private PlayerData playerData;
    private GameObject inventoryMenu;
    private GameObject Fire;
    private GameObject Water;
    private GameObject Plant;
    private GameObject Ruby;
    private GameObject Sapphire;
    private GameObject Emerald;

    private bool InventoryOpen;

    void Start()
    {
        playerController = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("InventoryUI: Player or playerController component was not found!");
        }

        playerData = GameObject.FindWithTag("PlayerData").GetComponent<PlayerData>();
        if (playerData != null)
        {
            playerInventory = playerData.GetComponent<InventorySystem>();
            if (playerInventory == null)
            {
                Debug.LogError("InventoryUI: No InventorySystem component found!");
            }
        } else
        {
            Debug.LogError("InventoryUI: No playerData found!");
        }

        inventoryMenu = this.gameObject.transform.Find("InventoryMenu").gameObject;
        if (inventoryMenu == null)
        {
            Debug.LogError("InventoryUI: Could not find InventoryMenu! Check naming and children!");
        } else {
            Fire = inventoryMenu.transform.Find("Fire").gameObject;
            Water = inventoryMenu.transform.Find("Water").gameObject;
            Plant = inventoryMenu.transform.Find("Plant").gameObject;

            Ruby = inventoryMenu.transform.Find("Ruby").gameObject;
            Sapphire = inventoryMenu.transform.Find("Sapphire").gameObject;
            Emerald = inventoryMenu.transform.Find("Emerald").gameObject;

            if (Fire == null || Water == null || Plant == null || Ruby == null || Sapphire == null || Emerald == null)
            {
                Debug.LogError("InventoryUI component could not be found! Check gem and element naming and children!");
            } else
            {
                // Update progression UI based on PlayerData values:
                if (!playerData.fireGemObtained)
                {
                    Ruby.SetActive(false);
                }
                if (!playerData.waterGemObtained)
                {
                    Sapphire.SetActive(false);
                }
                if (!playerData.plantGemObtained)
                {
                    Emerald.SetActive(false);
                }

                if (!playerData.plantBossDefeated)
                {
                    Plant.SetActive(false);
                }
                if (!playerData.fireBossDefeated)
                {
                    Fire.SetActive(false);
                }
                if (!playerData.waterBossDefeated)
                {
                    Water.SetActive(false);
                }
            }
        }

        // At start, hide Inventory (so it can be obtained during Awake safely)
        inventoryMenu.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I) && playerController.canMove)
        {
            if (InventoryOpen)
            {
                InventoryOpen = false;
                inventoryMenu.SetActive(false);
            } else
            {
                InventoryOpen = true;
                inventoryMenu.SetActive(true);
            }
        }

        // Activate if during playthrough, they were activated. 
        // These will only fire once because it sets UI to active, and only runs if it isnt.
        if (playerData.fireGemObtained && !Ruby.activeSelf)
        {
            Ruby.SetActive(true);
        }
        if (playerData.waterGemObtained && !Sapphire.activeSelf)
        {
            Sapphire.SetActive(true);
        }
        if (playerData.plantGemObtained && !Emerald.activeSelf)
        {
            Emerald.SetActive(true);
        }

        if (playerData.plantBossDefeated && !Plant.activeSelf)
        {
            Plant.SetActive(true);
        }
        if (playerData.fireBossDefeated && !Fire.activeSelf)
        {
            Fire.SetActive(true);
        }
        if (playerData.waterBossDefeated && !Water.activeSelf)
        {
            Water.SetActive(true);
        }
}
}
