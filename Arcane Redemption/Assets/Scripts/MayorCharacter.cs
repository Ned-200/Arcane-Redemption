using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Cinemachine;

public class MayorCharacter : NPC_Character
{

    private GameObject player;

    void Start()
    {
        CinemachineCamera = this.transform.Find("CinemachineCamera").gameObject;

        Debug.Log("Dialogue Begin");
        SpeakImage.SetActive(false);
        StartDialogue();
        NPC_Speaking = true;

        // Get player by tag
        player = GameObject.FindWithTag("Player");
        
        if (player != null) 
        {   
            playerController = player.GetComponent<PlayerController>();
            if (player.transform.Find("PlayerMesh").gameObject)
            {
                playerMesh = player.transform.Find("PlayerMesh").gameObject;
            } else
            {
                Debug.Log("NPC Could Not Find/Hide Player Mesh!");
            }

            playerInRange = true;
            Debug.Log("Beginning Intro Dialogue, Freezing Player");
            // Disable player movement
            playerController.canMove = false;
            playerMesh.SetActive(false);

        } else {
            Debug.Log("PLAYER NOT FOUND BY MAYOR! Check Player Tag.");
        }

    }

    protected override void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !NPC_Speaking)
        {
            Debug.Log("Dialogue Begin");
            SpeakImage.SetActive(false);
            StartDialogue();
            NPC_Speaking = true;

            // Disable player movement
            playerController.canMove = false;
            playerMesh.SetActive(false);
        }

        if (Input.GetMouseButtonDown(0))
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

        if (other.CompareTag("Player") && !NPC_Speaking) // Check if not already speaking, since player starts in dialogue
        {   
            // Get player from collision
            playerController = other.GetComponent<PlayerController>();
            if (other.transform.Find("PlayerMesh").gameObject)
            {
                playerMesh = other.transform.Find("PlayerMesh").gameObject;
            } else
            {
                Debug.Log("NPC Could Not Find/Hide Player Mesh!");
            }

            playerInRange = true;
            Debug.Log("Entered NPC range");
            SpeakImage.SetActive(true);
        }
    }
    protected override void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            textComponent.text = string.Empty;
            StartCoroutine(TypeLine());
        } else
        {
            // Change Dialogue if player speaks with NPC again
            System.Array.Resize(ref lines, 2);
            lines[0] = "What are you waiting around for? Go save my town and earn your darn freedom!";
            lines[1] = "Ahem... ...Please.";

            // End dialogue
            Debug.Log("Dialogue End");
            playerInRange = false;   
            DialogueBox.SetActive(false);
            CinemachineCamera.SetActive(false);
            NPC_Speaking = false;

            // Re-enable player movement
            playerController.canMove = true;
            // Un-hide player mesh
            playerMesh.SetActive(true);
        }
    }



}
