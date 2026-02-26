using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    private Canvas canvas;
    private GameObject Player;
    private PlayerCharacter playerCharacter;
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

    private Image MagusoIcon;
    private Sprite MagusoIconHealthy;
    private Sprite MagusoIconInjured;
    
    private bool InventoryOpen;

    void Start()
    {
        // Get and enable canvas, so it can be left disabled in scenes to not get in the way while editing
        canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = true;
        } else
        {
            Debug.LogError("InventoryUI: Canvas component was not found!");
        }

        // Get Player
        Player = GameObject.FindWithTag("Player");
        if (Player == null)
        {
            Debug.LogError("InventoryUI: Player game object was not found!");
        } else
        {
            // Get Player Controller
            playerController = Player.GetComponent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("InventoryUI: playerController component was not found!");
            }
            // Get Player Character
            playerCharacter = Player.GetComponent<PlayerCharacter>();
            if (playerCharacter == null)
            {
                Debug.LogError("InventoryUI: playerCharacter component was not found!");
            }
        }

        // Get PlayerData
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

        // Get inventory UI components
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

            //Get Maguso Icon
            MagusoIcon = inventoryMenu.transform.Find("MagusoIcon").GetComponent<Image>();
            MagusoIconHealthy = Resources.Load<Sprite>("MagusoIcon");
            MagusoIconInjured = Resources.Load<Sprite>("MagusoIcon_LowHealth");
            
            if (MagusoIcon == null || MagusoIconHealthy == null || MagusoIconInjured == null)
            {
                Debug.LogError("InventoryUI: MagusoIcon UI not found!! Check Canvas Gameobject.");
            }

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
        //Show injured icon when below half health
        if (playerCharacter.HealthPercent*100 > 50)
        {
            if (MagusoIcon.sprite != MagusoIconHealthy) {
                Debug.Log("PlayerCharacter: Set Maguso Icon to Healthy");
                MagusoIcon.sprite = MagusoIconHealthy;
            }
        } 
        else if (MagusoIcon.sprite != MagusoIconInjured) 
        {
            Debug.Log("PlayerCharacter: Set Maguso Icon to Injured");
            MagusoIcon.sprite = MagusoIconInjured;
        }

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
