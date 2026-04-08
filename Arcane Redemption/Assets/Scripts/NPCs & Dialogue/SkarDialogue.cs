using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;

public class SkarDialogue : NPC_Character
{
    [SerializeField] private bool endCutscene;
    [SerializeField] private GameObject CinematicCamera1;
    [SerializeField] private GameObject CinematicCamera2;
    private bool notFirstInteraction;
    [SerializeField] private GameObject Skar;
    [SerializeField] private AudioClip thunderSFX;
    private Light thunderLight;
    string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    protected override void Start()
    {
        base.Start();

        if (!endCutscene) {
            Skar.SetActive(false);

            if (CinematicCamera1 == null || CinematicCamera2 == null) 
            {   
                Debug.Log("SkarDialogue: Cinematic Cameras not assigned!");
            }
        
            thunderLight = CinematicCamera2.GetComponent<Light>();
            if (thunderLight == null) 
            {   
                Debug.Log("SkarDialogue: thunderLight not found!");
            }
        } else
        {
            StartDialogue();
        }
    }

    protected override void Update()
    {

        if (endCutscene)
        {
            //Make NPC face player
            Vector3 lookDirection = player.transform.position - NPCMesh.transform.position;
            lookDirection.Normalize();
            NPCMesh.transform.rotation = Quaternion.Slerp(NPCMesh.transform.rotation, Quaternion.LookRotation(lookDirection), lookSpeed * Time.deltaTime);
        }

        if (!notFirstInteraction && playerInRange && !endCutscene)
        {
            notFirstInteraction = true;
            Invoke(nameof(EnableCamera1), 2);
            playerController.canMove = false;
            playerAnim.SetBool("isWalking", false);
            playerAnim.SetBool("isSprinting", false);
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
    

    protected override IEnumerator TypeLine()
    {
        TalkingAnim(true);

        if (index == happyIndex) {
            PlayHappySound();
        } else if (index == angryIndex) {
            PlayAngrySound();
        } else {
            PlaySkipSounds();
        }

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
                    for (int i = 0; i < 5; i++)
                    {
                        char a = alphabet[Random.Range(0, alphabet.Length)];
                        textComponent.text += a;
                        yield return new WaitForSeconds(textSpeed);
                        textComponent.text = textComponent.text.Substring(0, textComponent.text.Length - 1);
                    }
                    textComponent.text += c;
                    yield return new WaitForSeconds(textSpeed);
                }
            }
        }
        TalkingAnim(false);
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

        Debug.Log("Dialogue Begin");
        if (SpeakImage != null)
        {
            Destroy(SpeakImage);
        }
    }

    private void EnableCamera1()
    {
        CinematicCamera1.SetActive(true);
            Invoke(nameof(EnableCamera2), 2);
    }

    private void EnableCamera2()
    {
        Skar.SetActive(true);
        CinematicCamera2.SetActive(true);
        Invoke(nameof(DisableCamera2), 5);   
        Invoke(nameof(DisableCamera1), 2);
    }

    private void DisableCamera2()
    {
        CinematicCamera2.SetActive(false);
    }

    private void DisableCamera1()
    {
        audioSource.PlayOneShot(thunderSFX);
        thunderLight.enabled = true;
        Invoke(nameof(disableThunder), 0.2f);
    }

    private void disableThunder()
    {
        thunderLight.enabled = false;
        CinematicCamera1.SetActive(false);
        // playerController.canMove = true;
        Invoke(nameof(StartDialogue), 1);
    }
}
