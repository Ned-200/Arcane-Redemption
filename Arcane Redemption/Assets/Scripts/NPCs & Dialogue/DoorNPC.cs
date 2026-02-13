using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;

public class DoorNPC : NPC_Character
{
    private bool hasSeenIntro = false;

    protected GameObject ReturnToTownCamera1;
    protected GameObject ReturnToTownCamera2;


    void Start()
    {
        base.Start();

        
        ReturnToTownCamera1 = this.transform.Find("ReturnToTownCamera1").gameObject;
        ReturnToTownCamera2 = this.transform.Find("ReturnToTownCamera2").gameObject;

        if (ReturnToTownCamera1 == null || ReturnToTownCamera2 == null) 
        {   
            Debug.Log("CUTSCENE CAMERAS NOT FOUND BY DOOR NPC! Check camera names.");
        }

        playerController = player.GetComponent<PlayerController>();
        Invoke(nameof(EnableCamera2), 2);
        playerController.canMove = false;

    }

    private void EnableCamera2()
    {
        ReturnToTownCamera2.SetActive(true);
        textComponent.text = "*Distant screaming and sounds of chaos*";
        DialogueBox.SetActive(true);
        Invoke(nameof(DisableCamera2), 4);
    }

    private void DisableCamera2()
    {
        ReturnToTownCamera2.SetActive(false);        
        textComponent.text = string.Empty;
        DialogueBox.SetActive(false);
        Invoke(nameof(DisableCamera1), 2);
    }

    private void DisableCamera1()
    {
        ReturnToTownCamera1.SetActive(false);
        playerController.canMove = true;
    }

    protected override void Update()
    {
        if (playerInRange && !NPC_Speaking && Input.GetKeyDown(KeyCode.E) && hasSeenIntro)
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

    protected override void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player") && !NPC_Speaking)
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

            if (!hasSeenIntro){
                hasSeenIntro = true;
                StartDialogue();
                Debug.Log("Dialogue Begin");
                SpeakImage.SetActive(false);
                NPC_Speaking = true;

                // disable player movement
                playerController.canMove = false;
                // hide player mesh
                playerMesh.SetActive(false);
                playerAccessory.SetActive(false);
                weaponMesh.SetActive(false);

            }else{
                playerInRange = true;
                Debug.Log("Entered NPC range");
                SpeakImage.SetActive(true);
            }
        }
    }

    protected override void NextLine()
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
            // Change Dialogue if player speaks with NPC again
            System.Array.Resize(ref lines, 2);
            lines[0] = "What are you still standing here for, help us!!";
            lines[1] = "I know there's still some good left in you..";

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

            // Since dialogue is changed, remove disable the cutscene camera references. AFTER camera is deactivated!
            cutsceneCamera = null;
            cutsceneLine = -1;
            endCutsceneLine = -1;

            // Re-enable player movement
            playerController.canMove = true;
            // Un-hide player mesh
            playerMesh.SetActive(true);
            playerAccessory.SetActive(true);
            weaponMesh.SetActive(true);
        }
    }



}
