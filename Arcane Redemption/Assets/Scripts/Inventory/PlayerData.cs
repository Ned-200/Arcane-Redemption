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
    private bool hideTownNPC = false;
    private bool hideDoorNPC = true;
    private bool hideTreeBoss = true;

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
        
        if (SceneManager.GetActiveScene().name == "GrayboxingV1") // If current scene is main area and NOT coming from Fire Dungeon, hide Door NPC
        {
            
            if (lastScene == "FireDungeonGraybox") // If coming from Fire Dungeon
            {
                // Hide NPCs
                hideTownNPC = true;
                hideDoorNPC = false;
                hideTreeBoss = false;

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

                // Hide NPCs
                hideTownNPC = true;
                hideDoorNPC = true;

                // Update Mayor NPC
                if (GameObject.Find("MayorNPC") != null) 
                {
                    hideMayor = false;

                    NPC_Character MayorNPC = GameObject.Find("MayorNPC").GetComponent<NPC_Character>();
                    if (MayorNPC != null) {
                        MayorNPC.lines = new string[7];
                        MayorNPC.lines[0] = "Excellent work, my friend!";
                        MayorNPC.lines[1] = "You've restored <b><color=#ff3300>Fire</color></b> to the realm! Our <b><color=#0073bf>torches</color></b> are lit once more!";
                        MayorNPC.transform.position = new Vector3(-60.3f, 26.5f, -4.7f);
                        
                        MayorNPC.endCutsceneLine = 6;
                        MayorNPC.cutsceneCamera = new GameObject[2];
                        MayorNPC.cutsceneLine = new int[2];
                        if (GameObject.Find("FireCamera") != null) {
                            MayorNPC.cutsceneCamera[0] = GameObject.Find("FireCamera");
                            MayorNPC.cutsceneLine[0] = 1;
                        } else {
                            Debug.LogError("PlayerData: No FireCamera found!");
                        }

                        if (waterBossDefeated)
                        {
                            MayorNPC.lines[2] = "Now only one challenge remains... As long as <b><color=#541834>Skar</color></b> is around, the innocent people of this realm will never truly be safe...";
                            MayorNPC.lines[3] = "After sealing the elements away, he sought refuge in the <b><color=#541834>Great Lookout Tower</color></b>, and no soul has seen him since.";
                            MayorNPC.lines[4] = "With all the power he has consumed, I fear it has consumed him as well...";
                            MayorNPC.lines[5] = "I cannot fathome the <b><color=#541834>monster</color></b> he's become, what he made himself into...";
                            MayorNPC.lines[6] = "It is up to you to put an end to his reign. Head to the tower and stop him. I believe in you, Maguso.";

                            MayorNPC.secondaryLines = new string[1];
                            MayorNPC.secondaryLines[0] = "Well? Go on! Head to the <b><color=#541834>Tower</color></b> behind the large stone gate outside town, <b><color=#541834>Skar</color></b> must be stopped once and for all!";

                            if (GameObject.Find("TowerCamera") != null) {     
                                MayorNPC.cutsceneCamera[1] = GameObject.Find("TowerCamera");
                                MayorNPC.cutsceneLine[1] = 3;
                            } else {
                                Debug.LogError("PlayerData: No TowerCamera found!");
                            }

                        } else {
                            MayorNPC.lines[2] = "However, one more beast awaits you... a <b><color=#0073bf>Great Squid of Tides</color></b> protects the last element sealed element, <b><color=#0073bf>Water</color></b>.";
                            MayorNPC.lines[3] = "It resides deep within <b><color=#0073bf>Swoosh Bay</color></b>, and will not be easy to reach.";
                            MayorNPC.lines[4] = "To defeat it and bring water back to our realm, you must first obtain the <b><color=#1c8c20>Plant Emerald</color></b>.";
                            MayorNPC.lines[5] = "Enter the <b><color=#1c8c20>Plant Labyrinth</color></b> and retrive it, and then use its power to restore the element of <b><color=#0073bf>Water</color></b>.";
                            MayorNPC.lines[6] = "Best of luck, Maguso... And thank you.";

                            if (GameObject.Find("PlantDungeonCamera") != null) {     
                                MayorNPC.cutsceneCamera[1] = GameObject.Find("PlantDungeonCamera");
                                MayorNPC.cutsceneLine[1] = 4;
                            } else {
                                Debug.LogError("PlayerData: No PlantDungeonCamera found!");
                            }

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

                // Hide NPCs
                hideTownNPC = true;
                hideDoorNPC = true;

                // Update Mayor NPC
                if (GameObject.Find("MayorNPC") != null) 
                {
                    hideMayor = false;
                    
                    NPC_Character MayorNPC = GameObject.Find("MayorNPC").GetComponent<NPC_Character>();
                    if (MayorNPC != null) {
                        MayorNPC.lines = new string[7];
                        MayorNPC.lines[0] = "You've done it again!";
                        MayorNPC.lines[1] = "You've restored <b><color=#0073bf>Water</color></b> to the realm! Our <b><color=#0073bf>wells</color></b> are filled and <b><color=#0073bf>rivers</color></b> run free!";
                        MayorNPC.transform.position = new Vector3(-60.3f, 26.5f, -4.7f);

                        MayorNPC.cutsceneCamera = new GameObject[2];
                        MayorNPC.cutsceneLine = new int[2];
                        MayorNPC.endCutsceneLine = 6;
                        if (GameObject.Find("WaterCamera") != null) {
                            MayorNPC.cutsceneCamera[0] = GameObject.Find("WaterCamera");
                            MayorNPC.cutsceneLine[0] = 1;
                        } else {
                            Debug.LogError("PlayerData: No WaterCamera found!");
                        }

                        if (fireBossDefeated)
                        {
                            MayorNPC.lines[2] = "Now only one challenge remains... As long as <b><color=#541834>Skar</color></b> is around, the innocent people of this realm will never truly be safe...";
                            MayorNPC.lines[3] = "After sealing the elements away, he sought refuge in the <b><color=#541834>Great Lookout Tower</color></b>, and no soul has seen him since.";
                            MayorNPC.lines[4] = "With all the power he has consumed, I fear it has consumed him as well...";
                            MayorNPC.lines[5] = "I cannot fathome the <b><color=#541834>monster</color></b> he's become, what he made himself into...";
                            MayorNPC.lines[6] = "It is up to you to put an end to his reign. Head to the tower and stop him. I believe in you, Maguso.";
                            
                            MayorNPC.secondaryLines = new string[1];
                            MayorNPC.secondaryLines[0] = "Well? Go on! Head to the <b><color=#541834>Tower</color></b> behind the large stone gate outside town, <b><color=#541834>Skar</color></b> must be stopped once and for all!";

                            if (GameObject.Find("TowerCamera") != null) {     
                                MayorNPC.cutsceneCamera[1] = GameObject.Find("TowerCamera");
                                MayorNPC.cutsceneLine[1] = 3;
                            } else {
                                Debug.LogError("PlayerData: No TowerCamera found!");
                            }

                        } else {
                            MayorNPC.lines[2] = "However, another beast lies ahead... a <b><color=#ff3300>Giant Snail of Magma</color></b> gaurds the last element sealed element, <b><color=#ff3300>Fire</color></b>.";
                            MayorNPC.lines[3] = "It resides deep within the Volcano <b><color=#ff3300>Mount Fwoosh</color></b>, and will not be easy to reach.";
                            MayorNPC.lines[4] = "To defeat it and bring warmth back to our realm, you must first obtain the <b><color=#0073bf>Water Sapphire</color></b>.";
                            MayorNPC.lines[5] = "Enter the <b><color=#0073bf>Water Trials</color></b> and retrive it, and then use its power to restore the element of <b><color=#ff3300>Fire</color></b>.";
                            MayorNPC.lines[6] = "Best of luck, Maguso... And thank you.";

                            if (GameObject.Find("WaterDungeonCamera") != null) {     
                                MayorNPC.cutsceneCamera[1] = GameObject.Find("WaterDungeonCamera");
                                MayorNPC.cutsceneLine[1] = 4;
                            } else {
                                Debug.LogError("PlayerData: No WaterDungeonCamera found!");
                            }

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

            // Hide additional cameras AFTER retrieving them in Bay and Volcano last scene checks
            if (GameObject.Find("FireCamera") != null) 
            {
                GameObject.Find("FireCamera").SetActive(false);
            } else
            {
                Debug.LogError("PlayerData can't find FireCamera!");
            }

            if (GameObject.Find("WaterCamera") != null) 
            {
                GameObject.Find("WaterCamera").SetActive(false);
            } else
            {
                Debug.LogError("PlayerData can't find WaterCamera!");
            }

            if (GameObject.Find("TowerCamera") != null) 
            {
                GameObject.Find("TowerCamera").SetActive(false);
            } else
            {
                Debug.LogError("PlayerData can't find TowerCamera!");
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

            if (fireGemObtained)
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
            }

            // Make tree calm after boss fight (by hiding sad tree)
            if (plantBossDefeated) { // to ensure not before

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
            if (hideTreeBoss) {
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
            // Hide Town NPC
            if (hideTownNPC) {
                if (GameObject.Find("TownNPC") != null) 
                {
                    GameObject.Find("TownNPC").SetActive(false);
                } else
                {
                    Debug.LogError("PlayerData can't find TownNPC!");
                }
            }
            // Hide Door NPC
            if (hideDoorNPC) {
                if (GameObject.Find("DoorNPC") != null) 
                {
                    GameObject.Find("DoorNPC").SetActive(false);
                } else
                {
                    Debug.LogError("PlayerData can't find DoorNPC!");
                }
            }

            // Restore or hide water, every check, regardless of last scene
            GameObject RestoreWater = GameObject.Find("RestoreWater");
            if (RestoreWater != null) 
            {
                RestoreWater.SetActive(waterBossDefeated);
            } else
            {
                Debug.LogError("PlayerData can't find RestoreWater!");
            }

            // Restore fire or hide, every check, regardless of last scene
            GameObject RestoreFire = GameObject.Find("RestoreFire");
            if (RestoreFire != null) 
            {
                RestoreFire.SetActive(fireBossDefeated);
            } else
            {
                Debug.LogError("PlayerData can't find RestoreFire!");
            }

            // ALWAYS hide dungeon door cameras, don't disable in scene so that they can be found.
            GameObject WaterDungeonCamera = GameObject.Find("WaterDungeonCamera");
            if (WaterDungeonCamera != null) 
            {
                WaterDungeonCamera.SetActive(false);
            } else
            {
                Debug.LogError("PlayerData can't find WaterDungeonCamera!");
            }
            GameObject PlantDungeonCamera = GameObject.Find("PlantDungeonCamera");
            if (PlantDungeonCamera != null) 
            {
                PlantDungeonCamera.SetActive(false);
            } else
            {
                Debug.LogError("PlayerData can't find PlantDungeonCamera!");
            }
        }
    }

}