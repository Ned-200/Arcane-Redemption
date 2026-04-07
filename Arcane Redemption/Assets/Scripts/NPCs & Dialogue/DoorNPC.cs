using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;

public class DoorNPC : NPC_Character
{
    protected GameObject ReturnToTownCamera1;
    protected GameObject ReturnToTownCamera2;
    private bool notFirstInteraction;
    [SerializeField] private AudioClip townPanicSound;


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

    protected override void Update()
    {
        if (!notFirstInteraction && playerInRange)
        {
            Debug.Log("Dialogue Begin");
            if (SpeakImage != null)
            {
                Destroy(SpeakImage);
            }
            StartDialogue();
        }

        base.Update();
    }
    
    protected override void StartDialogue()
    {
        base.StartDialogue();
        notFirstInteraction = true;
    }

    private void EnableCamera2()
    {
        ReturnToTownCamera2.SetActive(true);
        textComponent.text = "*Distant screaming and sounds of chaos*";
        audioSource.PlayOneShot(townPanicSound);
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
}
