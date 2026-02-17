using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerData : MonoBehaviour
{
    public bool fireGemObtained;
    public bool waterGemObtained;
    public bool plantGemObtained;

    public bool fireBossDefeated;
    public bool waterBossDefeated;
    public bool plantBossDefeated;

    public string lastScene;

    // called first
    void Awake()
    {
        Debug.Log("Awake");
		DontDestroyOnLoad(gameObject);
		Debug.Log("New Player Data");
    }

    // called second
    void OnEnable()
    {
        Debug.Log("OnEnable called");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // called third
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("OnSceneLoaded: " + lastScene + " to " + scene.name);
        Debug.Log(mode);

        RunCheck(); // Check for necessary changes to new scene
    }

    // called fourth
    void Start()
    {
        Debug.Log("Start");
    }

    // called when the game is terminated
    void OnDisable()
    {
        Debug.Log("OnDisable");
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void RunCheck() {
        Debug.Log("Player Data is running a scene check");

        if (lastScene == "FireDungeonGraybox") // If coming from Fire Dungeon
        {
            // Prevent player from re-entering fire dungeon
            GameObject fireDungeonTeleportDoor = GameObject.Find("FireDungeonTeleportDoor");
            if (fireDungeonTeleportDoor != null) 
            {
                fireDungeonTeleportDoor.SetActive(false);
            } else
            {
                Debug.LogError("PlayerData can't find FireDungeonTeleportDoor!");
            }

            // Hide Mayor NPC
            if (GameObject.Find("MayorNPC") != null) 
            {
                GameObject.Find("MayorNPC").SetActive(false);
            } else
            {
                Debug.LogError("PlayerData can't find MayorNPC!");
            }

            // Spawn Plant Boss in Town
            GameObject plantBoss = GameObject.Find("TreeBoss");
            if (plantBoss != null) 
            {
                plantBoss.SetActive(true);
            } else
            {
                Debug.LogError("PlayerData can't find PlantBoss!");
            }
            // Spawn Potions in Town
            GameObject bossPotions = GameObject.Find("BossPotions");
            if (bossPotions != null) 
            {
                bossPotions.SetActive(true);
            } else
            {
                Debug.LogError("PlayerData can't find BossPotions!");
            }

            // Hide town NPC
            GameObject townNPC = GameObject.Find("TownNPC");
            if (townNPC != null) 
            {
                townNPC.SetActive(false);
            } else
            {
                Debug.LogError("PlayerData can't find TownNPC!");
            }

            // Get Player
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                CharacterController characterController = player.GetComponent<CharacterController>();

                if (characterController != null) {
                    characterController.enabled = false;
                    characterController.transform.position = new Vector3(-180, 4, -6);
                    characterController.enabled = true;
                } else
                {
                    Debug.LogError("PlayerData can't find Character Controller!");
                }
            } else
            {
                Debug.LogError("PlayerData can't find Player!");
            }


        } else if (SceneManager.GetActiveScene().name == "GrayboxingV1") // If current scene is main area and NOT coming from Fire Dungeon, hide Door NPC
        {
            // Despawn Plant Boss in Town
            GameObject plantBoss = GameObject.Find("TreeBoss");
            if (plantBoss != null) 
            {
                plantBoss.SetActive(false);
            } else
            {
                Debug.LogError("PlayerData can't find PlantBoss!");
            }
            // Despawn Boss Potions in Town
            GameObject bossPotions = GameObject.Find("BossPotions");
            if (bossPotions != null) 
            {
                bossPotions.SetActive(false);
            } else
            {
                Debug.LogError("PlayerData can't find BossPotions!");
            }

            // Hide Door NPC
            if (GameObject.Find("DoorNPC") != null) 
            {
                GameObject.Find("DoorNPC").SetActive(false);
            } else
            {
                Debug.LogError("PlayerData can't find DoorNPC!");
            }

            // Hide Mayor NPC
            if (GameObject.Find("MayorNPC") != null) 
            {
                GameObject.Find("MayorNPC").SetActive(false);
            } else
            {
                Debug.LogError("PlayerData can't find MayorNPC!");
            }
            
        }
    }

}
