using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;

public class MayorCharacter : NPC_Character
{

    protected override void Start()
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
            Debug.LogError("PLAYER NOT FOUND BY MAYOR! Check Player Tag.");
        }
        
        // Fade out of black
        FadeUI.SetActive(true);
        FadeUI.GetComponent<Image>().CrossFadeAlpha(0, 8.0f, true);
        Debug.Log("FADING UI");
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
                playerAccessory = other.transform.Find("Hat").gameObject;
                weaponMesh = other.transform.Find("WeaponSlot").gameObject;
            } else
            {
                Debug.Log("NPC Could Not Find/Hide Player Mesh!");
            }

            playerInRange = true;
            Debug.Log("Entered NPC range");
            SpeakImage.SetActive(true);
        }
    }

}
