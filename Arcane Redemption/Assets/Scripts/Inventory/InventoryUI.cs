using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class InventoryUI : MonoBehaviour
{
    private Canvas canvas;
    private GameObject Player;
    private PlayerCharacter playerCharacter;
    private PlayerController playerController;
    private InventorySystem playerInventory;
    private PlayerData playerData;
    private GameObject inventoryMenu;
    private GameObject Key;
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
    private GameObject pauseMenu;
    private GameObject controls;
    private bool gamePaused;
    private bool quittingToMenu;
    [SerializeField] private GameObject loadingScreen;
    
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] hoverSounds;
    [SerializeField] private AudioClip[] clickSounds;
    [SerializeField] private AudioClip quitSound;

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

        // Get pause screen
        pauseMenu = this.transform.Find("PauseMenu").gameObject;
        if (pauseMenu == null)
        {
            Debug.LogError("InventoryUI: Could not find PauseMenu! Check naming and children!");
        } else
        {
            pauseMenu.SetActive(false);
            controls = pauseMenu.transform.Find("Controls").gameObject;
            if (controls == null)
            {
                Debug.LogError("InventoryUI: Could not find Controls! Check naming and children!");
            }
        }
        
        // Get inventory UI components
        inventoryMenu = this.transform.Find("InventoryMenu").gameObject;
        if (inventoryMenu == null)
        {
            Debug.LogError("InventoryUI: Could not find InventoryMenu! Check naming and children!");
        } else {
            Key = inventoryMenu.transform.Find("Key").gameObject;

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

        // Pause Game
        if (Input.GetKeyDown(KeyCode.Escape)) {
            // Close inventory if open when pausing
            if (InventoryOpen) {
                InventoryOpen = false;
                inventoryMenu.SetActive(false);
            }
            TogglePause();
        }

        // Toggle Inventory
        if (Input.GetKeyDown(KeyCode.I) && playerController.canMove)
        {
            PlayClickSounds();
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
        
        // Hide controls
        if (Input.GetMouseButtonDown(0) && controls.activeSelf)
        {
            PlayClickSounds();
            controls.SetActive(false);
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

    public void ToggleControls()
    {
        PlayClickSounds();

        if (controls.activeSelf) {
            controls.SetActive(false);
        } else
        {
            controls.SetActive(true);
        }
    }

    public void TogglePause()
    {
        if (gamePaused)
        {
            PlayClickSounds();
            Debug.Log("InventoryUI: Resuming Game");
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1;
            gamePaused = false;
            pauseMenu.SetActive(false);
            playerController.canMove = true;
        } else if (playerController.canMove) // to pause, check if player can move
        {
            PlayClickSounds();
            Debug.Log("InventoryUI: Pausing Game");
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0;
            gamePaused = true;
            pauseMenu.SetActive(true);
            playerController.canMove = false;
        }
    }

    public void QuitToMenu()
    {
        if (!quittingToMenu) // only can quit while not already quittingToMenu
        {
            quittingToMenu = true;

            Debug.Log("InventoryUI: Quitting to menu & destroying player save");
            if (playerData != null) {
                Destroy(playerData.gameObject); // If has player data, destroy it
            } else {
                playerData = GameObject.FindWithTag("PlayerData").GetComponent<PlayerData>();
                if (playerData != null) {
                    Destroy(playerData.gameObject); // if no data found, try to find it again
                } else {
                    Debug.LogError("InventoryUI: No PlayerData found to destroy. Check Data object Tag.");
                }
            }

            // Resume Game
            Debug.Log("InventoryUI: Resuming Game");
            Time.timeScale = 1;
            gamePaused = false;

            if (quitSound != null) {
                audioSource.PlayOneShot(quitSound);
            } else {
                Debug.LogError("InventoryUI: Not assigned quit sound.");
            }

            loadingScreen.SetActive(true);
            Invoke(nameof(Teleport), quitSound.length);
        }
    }

    private void Teleport()
    {
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }

    public void PlayClickSounds()
    {
        if (clickSounds.Length > 0) {
            audioSource.PlayOneShot(clickSounds[Random.Range(0, clickSounds.Length)]);
        } else {
            Debug.LogError("InventoryUI: Not assigned click sounds.");
        }
    }

    public void PlayHoverSounds()
    {
        if (hoverSounds.Length > 0) {
            audioSource.PlayOneShot(hoverSounds[Random.Range(0, hoverSounds.Length)]);
        } else {
            Debug.LogError("InventoryUI: Not assigned hover sounds.");
        }
    }
    
}
