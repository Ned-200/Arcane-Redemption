using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Cinemachine;

public class NPC_Character : BaseCharacter
{
    private bool playerInRange = false;
    private bool NPC_Speaking = false;

    [Header("UI")]
    [SerializeField] private GameObject SpeakImage;

    [Header("Text")]
    [SerializeField] private GameObject DialogueBox;
    public TextMeshProUGUI textComponent;
    public string[] lines;
    public float textSpeed;
    private int index;

    
    private GameObject CinemachineCamera;

    // Reference to player controller to block movement
    private PlayerController playerController;
    private GameObject playerMesh;

    void Start()
    {
        CinemachineCamera = this.transform.Find("CinemachineCamera").gameObject;
    }

    void Update()
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

    private void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
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

    private void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("Left NPC range");
            SpeakImage.SetActive(false);
        }
    }

    void StartDialogue()
    {
        textComponent.text = string.Empty;
        index = 0;
        StartCoroutine(TypeLine());
        DialogueBox.SetActive(true);
        CinemachineCamera.SetActive(true);
    }

    IEnumerator TypeLine()
    {
        foreach (char c in lines[index].ToCharArray())
        {
            textComponent.text += c;
            yield return new WaitForSeconds(textSpeed);
        }
    }

    void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            textComponent.text = string.Empty;
            StartCoroutine(TypeLine());
        } else
        {
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
