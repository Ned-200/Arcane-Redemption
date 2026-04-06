using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerData : MonoBehaviour
{
    private Transform fireDungeonSpawn;
    private Transform waterDungeonSpawn;
    private Transform plantDungeonSpawn;
    private Transform volcanoSpawn;
    private Transform baySpawn;

    public bool fireGemObtained;
    public bool waterGemObtained;
    public bool plantGemObtained;

    public bool fireBossDefeated;
    public bool waterBossDefeated;
    public bool plantBossDefeated;
    private bool hideMayor = true;

    private GameObject Player;
    private CharacterController characterController;

    // Tower Stuff
    public bool vineWallBurned;
    public bool fireWallDoused;
    public bool plantBridgeGrown;

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
            GameObject treeBossCheckpoint = GameObject.Find("TreeBossCheckpoint");
            if (treeBossCheckpoint != null) 
            {
                treeBossCheckpoint.SetActive(true);
            } else
            {
                Debug.LogError("PlayerData can't find TreeBossCheckpoint!");
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
            
            if (lastScene == "PlantDungeonGraybox") { // If coming from Plant Dungeon

                // Set player spawn position
                plantDungeonSpawn = GameObject.Find("PlantDungeonSpawn").transform;
                if (plantDungeonSpawn != null) {
                    characterController.enabled = false;
                    characterController.transform.position = plantDungeonSpawn.position;
                    characterController.enabled = true;
                } else
                {
                    Debug.LogError("PlayerData can't find plantDungeonSpawn!");
                }
            }

            if (lastScene == "VolcanoBattleArena") { // If coming from Volcano Boss Arena

                // Set player spawn position
                volcanoSpawn = GameObject.Find("VolcanoSpawn").transform;
                if (volcanoSpawn != null) {
                    characterController.enabled = false;
                    characterController.transform.position = volcanoSpawn.position;
                    characterController.enabled = true;
                } else
                {
                    Debug.LogError("PlayerData can't find VolcanoSpawn!");
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

                // Update Mayor NPC
                if (GameObject.Find("MayorNPC") != null) 
                {
                    hideMayor = false;

                    NPC_Character MayorNPC = GameObject.Find("MayorNPC").GetComponent<NPC_Character>();
                    if (MayorNPC != null) {
                        MayorNPC.cutsceneLine = new int[0];
                        MayorNPC.lines = new string[7];
                        MayorNPC.lines[0] = "Excellent work, my friend!";
                        MayorNPC.lines[1] = "You've restored <b><color=#ff3300>Fire</color></b> to the realm! Our <b><color=#0073bf>torches</color></b> are lit once more!";
                        MayorNPC.transform.position = new Vector3(-60.3f, 26.5f, -4.7f);

                        if (waterBossDefeated)
                        {
                            MayorNPC.lines[2] = "Now only one challenge remains... As long as <b><color=#541834>Skar</color></b> is around, the innocent people of this realm will never truly be safe...";
                            MayorNPC.lines[3] = "After sealing the elements away, he sought refuge in the <b><color=#541834>Great Lookout Tower</color></b>, and no soul has seen him since.";
                            MayorNPC.lines[4] = "With all the power he has consumed, I fear it has consumed him as well...";
                            MayorNPC.lines[5] = "I cannot fathome the <b><color=#541834>monster</color></b> he's become, what he made himself into...";
                            MayorNPC.lines[6] = "It is up to you to put an end to his reign. Head to the tower and stop him. I believe in you, Maguso.";
                            
                            MayorNPC.secondaryLines = new string[1];
                            MayorNPC.secondaryLines[0] = "Well? Go on! Head to the <b><color=#541834>Tower</color></b> behind the large stone gate outside town, <b><541834=#ff3300>Skar</color></b> must be stopped once and for all!";

                        } else {
                            MayorNPC.lines[2] = "However, one more beast awaits you... a <b><color=#0073bf>Great Squid of Tides</color></b> protects the last element sealed element, <b><color=#0073bf>Water</color></b>.";
                            MayorNPC.lines[3] = "It resides deep within <b><color=#0073bf>Swoosh Bay</color></b>, and will not be easy to reach.";
                            MayorNPC.lines[4] = "To defeat it and bring water back to our realm, you must first obtain the <b><color=#1c8c20>Plant Emerald</color></b>.";
                            MayorNPC.lines[5] = "Enter the <b><color=#1c8c20>Plant Labyrinth</color></b> and retrive it, and then use its power to restore the element of <b><color=#0073bf>Water</color></b>.";
                            MayorNPC.lines[6] = "Best of luck, Maguso... And thank you.";

                            MayorNPC.secondaryLines = new string[1];
                            MayorNPC.secondaryLines[0] = "What's the hold up? Enter the <b><color=#1c8c20>Plant Labyrinth</color></b> and begin your trek to restore <b><color=#0073bf>Water</color></b> to the world!";

                        }
                    } else
                    {
                        Debug.LogError("PlayerData can't find MayorNPC's  Character script component!");
                    }
                } else
                {
                    Debug.LogError("PlayerData can't find MayorNPC!");
                }
            }

            
            if (lastScene == "BayBattleArena") { // If coming from Bay Boss Arena

                // Set player spawn position
                baySpawn = GameObject.Find("BaySpawn").transform;
                if (baySpawn != null) {
                    characterController.enabled = false;
                    characterController.transform.position = baySpawn.position;
                    characterController.enabled = true;
                } else
                {
                    Debug.LogError("PlayerData can't find BaySpawn!");
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

                // Update Mayor NPC
                if (GameObject.Find("MayorNPC") != null) 
                {
                    hideMayor = false;
                    
                    NPC_Character MayorNPC = GameObject.Find("MayorNPC").GetComponent<NPC_Character>();
                    if (MayorNPC != null) {
                        MayorNPC.cutsceneLine = new int[0];
                        MayorNPC.lines = new string[6];
                        MayorNPC.lines[0] = "You've done it again!";
                        MayorNPC.lines[1] = "You've restored <b><color=#0073bf>Water</color></b> to the realm! Our <b><color=#0073bf>wells</color></b> are filled and <b><color=#0073bf>rivers</color></b> run free!";
                        MayorNPC.transform.position = new Vector3(-60.3f, 26.5f, -4.7f);

                        if (fireBossDefeated)
                        {
                            MayorNPC.lines[2] = "Now only one challenge remains... As long as <b><color=#541834>Skar</color></b> is around, the innocent people of this realm will never truly be safe...";
                            MayorNPC.lines[3] = "After sealing the elements away, he sought refuge in the <b><color=#541834>Great Lookout Tower</color></b>, and no soul has seen him since.";
                            MayorNPC.lines[4] = "With all the power he has consumed, I fear it has consumed him as well...";
                            MayorNPC.lines[5] = "I cannot fathome the <b><color=#541834>monster</color></b> he's become, what he made himself into...";
                            MayorNPC.lines[6] = "It is up to you to put an end to his reign. Head to the tower and stop him. I believe in you, Maguso.";
                            
                            MayorNPC.secondaryLines = new string[1];
                            MayorNPC.secondaryLines[0] = "Well? Go on! Head to the <b><color=#541834>Tower</color></b> behind the large stone gate outside town, <b><541834=#ff3300>Skar</color></b> must be stopped once and for all!";

                        } else {
                            MayorNPC.lines[2] = "However, another beast lies ahead... a <b><color=#ff3300>Giant Snail of Magma</color></b> gaurds the last element sealed element, <b><color=#ff3300>Fire</color></b>.";
                            MayorNPC.lines[3] = "It resides deep within the Volcano <b><color=#ff3300>Mount Fwoosh</color></b>, and will not be easy to reach.";
                            MayorNPC.lines[4] = "To defeat it and bring warmth back to our realm, you must first obtain the <b><color=#0073bf>Water Sapphire</color></b>.";
                            MayorNPC.lines[5] = "Enter the <b><color=#0073bf>Water Trials</color></b> and retrive it, and then use its power to restore the element of <b><color=#ff3300>Fire</color></b>.";
                            MayorNPC.lines[6] = "Best of luck, Maguso... And thank you.";

                            MayorNPC.secondaryLines = new string[1];
                            MayorNPC.secondaryLines[0] = "What's the hold up? Enter the <b><color=#0073bf>Water Trials</color></b> and begin your trek to restore <b><color=#ff3300>Fire</color></b> to the world!";

                        }
                    } else
                    {
                        Debug.LogError("PlayerData can't find MayorNPC's  Character script component!");
                    }
                } else
                {
                    Debug.LogError("PlayerData can't find MayorNPC!");
                }
            }

            // ALWAYS DO IF IN MAIN SCENE, REGARDLESS OF LAST SCENE
            if (plantBridgeGrown) {
                GameObject towerPlantBridgeCastPoint = GameObject.Find("TowerPlantBridgeCastPoint");
                if (towerPlantBridgeCastPoint != null) 
                {
                    PlantBridge plantBridge = towerPlantBridgeCastPoint.GetComponent<PlantBridge>();
                    if (plantBridge == null)
                    {
                        Debug.LogError($"{towerPlantBridgeCastPoint.gameObject.name}: PlantBridge missing PlantBridge component!");
                    }

                    plantBridge.GrowBridge();
                } else
                {
                    Debug.LogError("PlayerData can't find towerPlantBridgeCastPoint!");
                }
            }
            if (vineWallBurned) {
                GameObject towerVineWall = GameObject.Find("TowerVineWall");
                if (towerVineWall != null) 
                {
                    Destroy(towerVineWall);
                } else
                {
                    Debug.LogError("PlayerData can't find towerVineWall!");
                }
            }
            if (fireWallDoused) {
                GameObject towerFlameWall = GameObject.Find("TowerFlameWall");
                if (towerFlameWall != null) 
                {
                    Destroy(towerFlameWall);
                } else
                {
                    Debug.LogError("PlayerData can't find towerFlameWall!");
                }
            }

            // Make tree calm after boss fight (by hiding sad tree)
            if (plantBossDefeated) { // to ensure not before

                // Prevent Player from re-entering fire dungeon
                GameObject fireDungeonTeleportDoor = GameObject.Find("FireDungeonTeleportDoor");
                if (fireDungeonTeleportDoor != null) 
                {
                    fireDungeonTeleportDoor.SetActive(false);
                } else
                {
                    Debug.LogError("PlayerData can't find FireDungeonTeleportDoor!");
                }

                GameObject SadTree = GameObject.Find("SadTree");
                if (SadTree != null) 
                {
                    SadTree.SetActive(false);
                } else
                {
                    Debug.LogError("PlayerData can't find SadTree!");
                }

                // Also swap terrain if has plant boss was defeated and not coming from fire dungeon
                GameObject TerrainSwap = GameObject.Find("TerrainSwap");
                if (TerrainSwap != null) 
                {
                    TerrainSwap TerrainSwapScript = TerrainSwap.GetComponent<TerrainSwap>();
                    TerrainSwapScript.SetCheckpointReached(true);
                } else
                {
                    Debug.LogError("PlayerData can't find TerrainSwap!");
                }

                GameObject WaterDungeonPlantWall = GameObject.Find("WaterDungeonVineWall");
                GameObject PlantDungeonPlantWall = GameObject.Find("PlantDungeonVineWall");
                // Make vines burnable if boss was already defeated
                if (WaterDungeonPlantWall != null && PlantDungeonPlantWall != null)
                {
                    if (!plantGemObtained) // make plant dungeon entrance burnable if plant gem not been entered before
                    {
                        PlantDungeonPlantWall.tag = "PlantWall";
                    }
                    if (!waterGemObtained) // make water dungeon entrance burnable if water gem not been entered before
                    {
                        WaterDungeonPlantWall.tag = "PlantWall";
                    }
                } else
                {
                    Debug.LogError("PlayerData can't find Water / Plant Dungeon Vine Walls!");
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

            // Despawn Plant Boss in Town
            GameObject plantBoss = GameObject.Find("TreeBoss");
            if (plantBoss != null) 
            {
                plantBoss.SetActive(false);
            } else
            {
                Debug.LogError("PlayerData can't find PlantBoss!");
            }
            GameObject treeBossCheckpoint = GameObject.Find("TreeBossCheckpoint");
            if (treeBossCheckpoint != null) 
            {
                treeBossCheckpoint.SetActive(false);
            } else
            {
                Debug.LogError("PlayerData can't find TreeBossCheckpoint!");
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
            if (hideMayor) {
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

}