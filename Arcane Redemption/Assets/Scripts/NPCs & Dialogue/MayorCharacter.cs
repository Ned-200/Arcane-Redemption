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

        base.Start();

        Debug.Log("Dialogue Begin");
        if (SpeakImage != null)
        {
            Destroy(SpeakImage);
        }
        StartDialogue();
        NPC_Speaking = true;

        playerInRange = true;
        Debug.Log("Beginning Intro Dialogue, Freezing Player");
        
        // Fade out of black
        FadeUI.SetActive(true);
        FadeUI.GetComponent<Image>().CrossFadeAlpha(0, 8.0f, true);
        Debug.Log("FADING UI");
    }

}
