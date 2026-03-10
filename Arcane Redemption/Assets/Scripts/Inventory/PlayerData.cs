using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerData : MonoBehaviour
{
    private Transform fireDungeonSpawn;
    private Transform waterDungeonSpawn;

    public bool fireGemObtained;
    public bool waterGemObtained;
    public bool plantGemObtained;

    public bool fireBossDefeated;
    public bool waterBossDefeated;
    public bool plantBossDefeated;

    private GameObject Player;
    private CharacterController characterController;

    public string lastScene = "No Scene";

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

        // Get Player
        Player = GameObject.FindWithTag("Player");
        if (Player != null)
        {
            characterController = Player.GetComponent<CharacterController>();

            if (characterController == null) {
                Debug.LogError("PlayerData can't find Character Controller!");
            }
        } else
        {
            Debug.LogError("PlayerData can't find Player!");
        }

        if (lastScene == "FireDungeonGraybox") // If coming from Fire Dungeon
        {
            // Prevent Player from re-entering fire dungeon
            GameObject fireDungeonTeleportDoor = GameObject.Find("FireDungeonTeleportDoor");
            if (fireDungeonTeleportDoor != null) 
            {
                fireDungeonTeleportDoor.SetActive(false);
            } else
            {
                Debug.LogError("PlayerData can't find FireDungeonTeleportDoor!");
            }

            // Make tree sad during boss fight (by hiding calm tree)
            GameObject CalmTree = GameObject.Find("CalmTree");
            if (CalmTree != null) 
            {
                CalmTree.SetActive(false);
            } else
            {
                Debug.LogError("PlayerData can't find CalmTree!");
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

            // Set player spawn position
            fireDungeonSpawn = GameObject.Find("FireDungeonSpawn").transform;
            if (fireDungeonSpawn != null) {
                characterController.enabled = false;
                characterController.transform.position = fireDungeonSpawn.position;
                characterController.enabled = true;
            } else
            {
                Debug.LogError("PlayerData can't find fireDungeonSpawn!");
            }

        } else if (SceneManager.GetActiveScene().name == "GrayboxingV1") // If current scene is main area and NOT coming from Fire Dungeon, hide Door NPC
        {
            // MAKE NECESSARY CHANGES FOR ALL OTHER SCENES


            // Make tree calm after boss fight (by hiding sad tree)
            if (fireGemObtained) { // to ensure not before
                GameObject SadTree = GameObject.Find("SadTree");
                if (SadTree != null) 
                {
                    SadTree.SetActive(false);
                } else
                {
                    Debug.LogError("PlayerData can't find SadTree!");
                }
            } else // Make tree sad before boss fight (by hiding calm tree)
            {
                GameObject CalmTree = GameObject.Find("CalmTree");
                if (CalmTree != null) 
                {
                    CalmTree.SetActive(false);
                } else
                {
                    Debug.LogError("PlayerData can't find CalmTree!");
                }
            }

            if (lastScene == "WaterDungeonGraybox") { // If coming from Water Dungeon

                // Set player spawn position
                waterDungeonSpawn = GameObject.Find("WaterDungeonSpawn").transform;
                if (waterDungeonSpawn != null) {
                    characterController.enabled = false;
                    characterController.transform.position = waterDungeonSpawn.position;
                    characterController.enabled = true;
                } else
                {
                    Debug.LogError("PlayerData can't find waterDungeonSpawn!");
                }
            }

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