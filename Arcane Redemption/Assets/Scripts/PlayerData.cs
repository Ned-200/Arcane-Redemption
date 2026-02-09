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
        RunCheck(); // INCLUDE ALL THE SAME CODE AS NEW SCENE, IN CASE PLAYER DATA WAS JUST CREATED
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
            if (GameObject.Find("FireDungeonTeleportDoor") != null) // Prevent player from re-entering fire dungeon
            {
                GameObject.Find("FireDungeonTeleportDoor").SetActive(false);
            } else
            {
                Debug.LogError("PlayerData can't find FireDungeonTeleportDoor!");
            }

            if (GameObject.Find("TownNPC") != null) // Hide town NPC
            {
                GameObject.Find("TownNPC").SetActive(false);
            } else
            {
                Debug.LogError("PlayerData can't find TownNPC!");
            }

            if (GameObject.FindWithTag("Player") != null)
            {
                GameObject player = GameObject.FindWithTag("Player");
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
