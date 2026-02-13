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

    [Header("UI")]
    [SerializeField] protected GameObject SpeakImage;
    [SerializeField] protected GameObject FadeUI;

    [Header("Cutscene Camera")]
    [SerializeField] protected GameObject cutsceneCamera;
    [SerializeField] protected int cutsceneLine;
    [SerializeField] protected int endCutsceneLine;

    [Header("Text")]
    [SerializeField] protected GameObject DialogueBox;
    public TextMeshProUGUI textComponent;
    public string[] lines;
    public float textSpeed;
    protected int index;

    
    protected GameObject CinemachineCamera;

    // Reference to player controller to block movement
    protected PlayerController playerController;
    protected GameObject playerMesh;
    protected GameObject weaponMesh;
    protected GameObject playerAccessory;
 

    protected void Start()
    {
        CinemachineCamera = this.transform.Find("CinemachineCamera").gameObject;

        player = GameObject.FindWithTag("Player");
        
        if (player == null) 
        {   
            Debug.Log("PLAYER NOT FOUND BY DOOR NPC! Check Player Tag.");
        }
    }

    protected override void Update()
    {
        if (playerInRange & Input.GetKeyDown(KeyCode.E) & !NPC_Speaking)
        {
            Debug.Log("Dialogue Begin");
            SpeakImage.SetActive(false);
            StartDialogue();
            NPC_Speaking = true;

            // disable player movement
            playerController.canMove = false;
            // hide player mesh
            playerMesh.SetActive(false);
            playerAccessory.SetActive(false);
            weaponMesh.SetActive(false);
        }

        if (Input.GetMouseButtonDown(0) & NPC_Speaking)
        {
            if (textComponent.text == lines[index])
            {
                NextLine();
            } else
            {
                StopAllCoroutines();
                textComponent.text = lines[index];
            }
        }
    }

    protected virtual void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {   
            // Get player from collision
            player = other;
            playerController = player.GetComponent<PlayerController>();
            if (player.transform.Find("PlayerMesh").gameObject)
            {
                playerMesh = player.transform.Find("PlayerMesh").gameObject;
                playerAccessory = player.transform.Find("Hat").gameObject;
                weaponMesh = player.transform.Find("WeaponSlot").gameObject;
              
            } else
            {
                Debug.Log("NPC Could Not Find/Hide Player Mesh!");
            }

            playerInRange = true;
            Debug.Log("Entered NPC range");
            SpeakImage.SetActive(true);
        }
    }

    protected void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("Left NPC range");
            SpeakImage.SetActive(false);
        }
    }

    protected void StartDialogue()
    {
        textComponent.text = string.Empty;
        index = 0;
        StartCoroutine(TypeLine());
        DialogueBox.SetActive(true);
        CinemachineCamera.SetActive(true);

        if (player == null)
        {
            // Get player by tag
            player = GameObject.FindWithTag("Player");
            
            if (player != null) 
            {   
                playerController = player.GetComponent<PlayerController>();
            }
        }

        if (player.transform.Find("PlayerMesh").gameObject)
        {
            playerMesh = player.transform.Find("PlayerMesh").gameObject;
            playerAccessory = player.transform.Find("Hat").gameObject;
            weaponMesh = player.transform.Find("WeaponSlot").gameObject;
              
        } else
        {
            Debug.Log("NPC Could Not Find/Hide Player Mesh!");
        }

        // disable player movement
        playerController.canMove = false;
        // hide player mesh
        playerMesh.SetActive(false);
        playerAccessory.SetActive(false);
        weaponMesh.SetActive(false);
    }

    protected IEnumerator TypeLine()
    {
        foreach (char c in lines[index].ToCharArray())
        {
            textComponent.text += c;
            yield return new WaitForSeconds(textSpeed);
        }
    }

    protected virtual void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            textComponent.text = string.Empty;
            StartCoroutine(TypeLine());

            // If a cutscene camera exists, enable it on the right line
            if (cutsceneCamera != null && index == cutsceneLine) {
                cutsceneCamera.SetActive(true);
            }

            // If a cutscene camera exists, disable it on the right line
            if (cutsceneCamera != null && index == endCutsceneLine) {
                cutsceneCamera.SetActive(false);
            }

        } else
        {
            // End dialogue
            Debug.Log("Dialogue End");
            playerInRange = false;   
            DialogueBox.SetActive(false);
            CinemachineCamera.SetActive(false);
            NPC_Speaking = false;

            // Disable cutscene camera at the end of dialogue if it exists
            if (cutsceneCamera != null) {
                cutsceneCamera.SetActive(false);
            }

            // Re-enable player movement
            playerController.canMove = true;
            // Un-hide player mesh
            playerMesh.SetActive(true);
            playerAccessory.SetActive(true);
            weaponMesh.SetActive(true);
        
        }
    }

}
