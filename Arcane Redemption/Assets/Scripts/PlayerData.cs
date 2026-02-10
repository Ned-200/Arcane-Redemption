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

    public int healthPotions;
    public int manaPotions;

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


            GameObject plantBoss = GameObject.Find("PlantBoss");
            if (plantBoss != null) // Spawn Plant Boss in Town
            {
                plantBoss.SetActive(true);
            } else
            {
                Debug.LogError("PlayerData can't find PlantBoss!");
            }

            GameObject fireDungeonTeleportDoor = GameObject.Find("FireDungeonTeleportDoor");
            if (fireDungeonTeleportDoor != null) // Prevent player from re-entering fire dungeon
            {
                fireDungeonTeleportDoor.SetActive(false);
            } else
            {
                Debug.LogError("PlayerData can't find FireDungeonTeleportDoor!");
            }

            GameObject townNPC = GameObject.Find("TownNPC");
            if (townNPC != null) // Hide town NPC
            {
                townNPC.SetActive(false);
            } else
            {
                Debug.LogError("PlayerData can't find TownNPC!");
            }

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
            GameObject plantBoss = GameObject.Find("PlantBoss");
            if (plantBoss != null) // Despawn Plant Boss in Town
            {
                plantBoss.SetActive(false);
            } else
            {
                Debug.LogError("PlayerData can't find PlantBoss!");
            }


            if (GameObject.Find("DoorNPC") != null) // Hide Door NPC
            {
                GameObject.Find("DoorNPC").SetActive(false);
            } else
            {
                Debug.LogError("PlayerData can't find DoorNPC!");
            }
            
        }
    }

}
