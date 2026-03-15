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


    protected override void Start()
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
        if (playerInRange) {
            //Make NPC face player
            NPCMesh.transform.LookAt(new Vector3(player.transform.position.x, NPCMesh.transform.position.y, player.transform.position.z));

            if (Input.GetKeyDown(KeyCode.E) && !NPC_Speaking && hasSeenIntro)
            {
                Debug.Log("Dialogue Begin");
                if (SpeakImage != null)
                {
                    Destroy(SpeakImage);
                }
                StartDialogue();
                NPC_Speaking = true;

                // disable player movement
                playerController.canMove = false;
                // hide player mesh
                playerMesh.SetActive(false);
                weaponMesh.SetActive(false);
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
                weaponMesh = player.transform.Find("WeaponSlot").gameObject;
              
            } else
            {
                Debug.Log("NPC Could Not Find/Hide Player Mesh!");
            }

            if (!hasSeenIntro){
                hasSeenIntro = true;
                StartDialogue();
                Debug.Log("Dialogue Begin");
                if (SpeakImage != null)
                {
                    Destroy(SpeakImage);
                }
                NPC_Speaking = true;

                // disable player movement
                playerController.canMove = false;
                // hide player mesh
                playerMesh.SetActive(false);
                weaponMesh.SetActive(false);

            }else{
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
    }

}
