using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Cinemachine;

public class NPC_Character : BaseCharacter
{
    protected private GameObject player;
    protected private bool playerInRange = false;
    protected private bool NPC_Speaking = false;
    protected private int markUp; // whether text is actually used to augment other text. ex <b> for bold, and shouldn't appear 
    // (0 == is NOT marked up, 1 == IS marked up, 2 has the first ">", 3 has the second ">" and should close)
    protected private string markedUpString;
    protected private Coroutine TypeLineCoroutine;

    [Header("UI")]
    [SerializeField] protected GameObject SpeakImagePrefab;
    protected GameObject SpeakImage;
    [SerializeField] protected GameObject FadeUI;

    [Header("Cutscene Camera")]
    [SerializeField] protected GameObject[] cutsceneCamera;
    [SerializeField] protected int[] cutsceneLine;
    [SerializeField] protected int endCutsceneLine;

    [Header("Text")]
    [SerializeField] protected GameObject DialogueBox;
    public TextMeshProUGUI textComponent;
    public string[] lines;
    [SerializeField] protected bool hasSecondaryLines;
    public string[] secondaryLines;
    public float textSpeed;
    protected int index;
    protected int cutsceneIndex;
    
    [SerializeField] protected int happyIndex = -1;
    [SerializeField] protected int angryIndex = -1;
    [SerializeField] protected int secondaryHappyIndex = -1;
    [SerializeField] protected int secondaryAngryIndex = -1;


    
    protected GameObject NPCMesh;
    protected GameObject CinemachineCamera;
    protected float lookSpeed = 3.0f; // speed of NPC rotation when facing player

    // Reference to player controller to block movement
    protected PlayerController playerController;
    protected GameObject playerMesh;
    protected GameObject weaponMesh;
 

    protected virtual void Start()
    {
        // Get NPC Mesh and Cinemachine Camera
        NPCMesh = this.transform.Find("NPCMesh").gameObject;
        if (NPCMesh != null)
        {
            CinemachineCamera = NPCMesh.transform.Find("CinemachineCamera").gameObject;
            if (CinemachineCamera == null)
            {
                Debug.LogError("CinemachineCamera NOT FOUND BY NPC! Check CinemachineCamera Hierarchy, it should be within mesh.");
            }
        } else
        {
            Debug.LogError("NPCMesh NOT FOUND BY NPC! Check NPCMesh Name.");
        }

        // Get Player
        player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            // Get playerController
            playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("NPC_Character: playerController NOT FOUND BY NPC! Check Player Hierarchy.");
            }
            // Get PlayerMesh
            if (player.transform.Find("PlayerMesh").gameObject)
            {
                playerMesh = player.transform.Find("PlayerMesh").gameObject;
                weaponMesh = player.transform.Find("WeaponSlot").gameObject;
            } else
            {
                Debug.Log("NPC Could Not Find/Hide Player Mesh!");
            }
        } else {   
            Debug.LogError("NPC_Character: PLAYER NOT FOUND BY NPC! Check Player Tag.");
        }

        // Get UI if it is not assigned
        if ( DialogueBox == null || textComponent == null)
        {
            GameObject mainCanvas = GameObject.FindWithTag("MainCanvas").gameObject;
            DialogueBox = mainCanvas.transform.Find("DialogueBox").gameObject;
            textComponent = DialogueBox.transform.Find("Text").GetComponent<TextMeshProUGUI>();
        }
        if (SpeakImagePrefab == null)
        {
            Debug.LogError("NPC_Character: Speak Prompt prefab not assigned! " + this.gameObject.name);
        }
    }

    protected override void Update()
    {
        if (playerInRange) {
            //Make NPC face player
            Vector3 lookDirection = player.transform.position - NPCMesh.transform.position;
            lookDirection.Normalize();
            NPCMesh.transform.rotation = Quaternion.Slerp(NPCMesh.transform.rotation, Quaternion.LookRotation(lookDirection), lookSpeed * Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.E) && !NPC_Speaking)
            {
                Debug.Log("Dialogue Begin");
                if (SpeakImage != null)
                {
                    Destroy(SpeakImage);
                }
                StartDialogue();
            }
        }

        if (Input.GetMouseButtonDown(0) && NPC_Speaking)
        {
            if (textComponent.text == lines[index])
            {
                NextLine();
            } else
            {
                StopAllCoroutines();
                TalkingAnim(false);
                textComponent.text = lines[index];
            }
        }
    }

    protected virtual void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player") && !NPC_Speaking)
        {   
            // Get player from collision
            player = other;
            if (player != null)
            {
                // Get playerController
                playerController = player.GetComponent<PlayerController>();
                if (playerController == null)
                {
                    Debug.LogError("NPC_Character: playerController NOT FOUND BY NPC! Check Player Hierarchy.");
                }

                // Get PlayerMesh
                if (player.transform.Find("PlayerMesh").gameObject)
                {
                    playerMesh = player.transform.Find("PlayerMesh").gameObject;
                    weaponMesh = player.transform.Find("WeaponSlot").gameObject;
                } else
                {
                    Debug.LogError("NPC_Character: NPC Could Not Find/Hide Player Mesh!");
                }

            } else {   
                Debug.LogError("NPC_Character: PLAYER NOT FOUND BY NPC! Check Player Tag.");
            }
            
            playerInRange = true;
            Debug.Log("Entered NPC range");
            
            if (SpeakImagePrefab != null)
            {
                SpeakImage = Instantiate(SpeakImagePrefab, new Vector3(NPCMesh.transform.position.x, NPCMesh.transform.position.y+3, NPCMesh.transform.position.z), NPCMesh.transform.rotation);
            } else
            {
                Debug.LogError("NPC_Character: Speak Prompt prefab not assigned!");
            }
        }
    }

    protected void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("Left NPC range");
            if (SpeakImage != null)
            {
                Destroy(SpeakImage);
            }
        }
    }

    protected virtual void TalkingAnim(bool isTalking)
    {
        if (NPCMesh != null) {
            if (NPCMesh.GetComponent<Animator>())
            {
                Animator NPC_Anim = NPCMesh.GetComponent<Animator>();
                
                NPC_Anim.SetBool("isTalking", isTalking);
            }
        }
    }

    protected virtual void StartDialogue()
    {
        textComponent.text = string.Empty;
        cutsceneIndex = 0;
        index = 0;
        TypeLineCoroutine = StartCoroutine(TypeLine());
        DialogueBox.SetActive(true);
        CinemachineCamera.SetActive(true);
        NPC_Speaking = true;

        // disable player movement
        playerController.canMove = false;
        // hide player mesh
        playerMesh.SetActive(false);
        weaponMesh.SetActive(false);
    }

    protected IEnumerator TypeLine()
    {
        TalkingAnim(true);

        // If an angry index exists, play the animation on the right line
        if (index == angryIndex) {
            if (NPCMesh.GetComponent<Animator>()) {
                Animator NPC_Anim = NPCMesh.GetComponent<Animator>();
                NPC_Anim.Play("Angry");
            }
        }

        // If an angry index exists, play the animation on the right line
        if (index == happyIndex) {
            if (NPCMesh.GetComponent<Animator>()) {
                Animator NPC_Anim = NPCMesh.GetComponent<Animator>();
                NPC_Anim.Play("Happy");
            }    
        }

        foreach (char c in lines[index].ToCharArray())
        {
            if (markUp > 0)
            {
                markedUpString += c;

                if (c == '>') { // increase markUp until second ">" which ends it
                    markUp++;
                }
                
                if (markUp == 5)
                {
                    // Debug.Log(markedUpString);
                    textComponent.text += markedUpString; // add the whole marked up string at once
                    // reset markup for next one
                    markUp = 0;
                    markedUpString = string.Empty; 
                }

            } else {
                if (c == '<') // start a marked up string
                {
                    markUp = 1;
                    markedUpString += '<';
                } else 
                { // If not marked up
                    textComponent.text += c;
                    yield return new WaitForSeconds(textSpeed);
                }
            }
        }
        TalkingAnim(false);
    }

    protected virtual void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            textComponent.text = string.Empty;
            TypeLineCoroutine = StartCoroutine(TypeLine());

            Debug.Log(cutsceneIndex);

            // If a cutscene camera exists, enable it on the right line
            if (cutsceneLine.Length > 0) {
                if (index == cutsceneLine[cutsceneIndex]) // If current line is a cutscene line enable cutscene camera
                {
                    cutsceneCamera[cutsceneIndex].SetActive(true);
                    if (cutsceneIndex < cutsceneCamera.Length - 1) // only go to next cutscene index if there is more (it breaks otherwise!)
                    {       
                        cutsceneIndex++;
                    }
                }
            }

            // If a cutscene camera exists, disable it on the right line
            if (index == endCutsceneLine) {
                foreach (GameObject cam in cutsceneCamera)
                {
                    if (cam != null)
                    {
                        cam.SetActive(false);
                    }
                }
            }

        } else
        {
            // Change Dialogue if player speaks with NPC again  
            if (hasSecondaryLines) {
                System.Array.Resize(ref lines, secondaryLines.Length);
                lines = secondaryLines;
            }

            // End dialogue
            Debug.Log("Dialogue End");
            DialogueBox.SetActive(false);
            if (CinemachineCamera != null) {
                CinemachineCamera.SetActive(false);
            }
            NPC_Speaking = false;

            // Disable cutscene camera at the end of dialogue if it exists
            foreach (GameObject cam in cutsceneCamera)
            {
                if (cam != null)
                {
                    cam.SetActive(false);
                }
            }

            // Since dialogue is changed, remove disable the cutscene camera references. AFTER camera is deactivated!
            if (hasSecondaryLines) {
                cutsceneCamera = new GameObject[0];
                cutsceneLine = new int[0];
                endCutsceneLine = -1;
                happyIndex = secondaryHappyIndex;
                angryIndex = secondaryAngryIndex;

                // Spawn SpeakImage to talk again if there is secondary dialogue
                if (SpeakImagePrefab != null)
                {
                    SpeakImage = Instantiate(SpeakImagePrefab, new Vector3(NPCMesh.transform.position.x, NPCMesh.transform.position.y+3, NPCMesh.transform.position.z), NPCMesh.transform.rotation);
                } else
                {
                    Debug.LogError("NPC_Character: Speak Prompt prefab not assigned!");
                }
            }

            
            if (player != null) {
                // Re-enable player movement
                playerController.canMove = true;
                // Un-hide player mesh
                playerMesh.SetActive(true);
                weaponMesh.SetActive(true);
            }
        
        }
    }

}
