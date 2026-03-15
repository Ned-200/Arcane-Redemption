using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;

public class GhostNPC : NPC_Character
{
    private GameObject NPC_Mesh;
    [SerializeField] protected int mayorIndex;
    private GameObject mayorCamera;
    protected override void Start()
    {
        base.Start();

        NPC_Mesh = gameObject.transform.Find("NPC_Mesh").gameObject;
        if (NPC_Mesh == null)
        {
            Debug.LogError("GhostNPC: Can't find NPC mesh gameobject");
        }

        playerController = player.GetComponent<PlayerController>();
        if (player.transform.Find("PlayerMesh").gameObject)
        {
            playerMesh = player.transform.Find("PlayerMesh").gameObject;
            weaponMesh = player.transform.Find("WeaponSlot").gameObject;
        
        } else
        {
            Debug.Log("NPC Could Not Find/Hide Player Mesh!");
        }

        Invoke(nameof(BeginGhostDialogue), 3);
    }

    private void BeginGhostDialogue()
    {
        CinemachineCamera.SetActive(true);

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


    protected override void Update()
    {

        if (Input.GetMouseButtonDown(0) & NPC_Speaking)
        {
            if (textComponent.text == lines[index])
            {
                NextLine();

                if (index == cutsceneLine[cutsceneIndex])
                {
                    StartCoroutine(TweenPosition(NPC_Mesh, new Vector3(NPC_Mesh.transform.localPosition.x, NPC_Mesh.transform.localPosition.y+8, NPC_Mesh.transform.localPosition.z), 2));
                }

            } else
            {
                if (TypeLineCoroutine != null) {
                    StopCoroutine(TypeLineCoroutine);
                }
                textComponent.text = lines[index];
            }
        }
    }

    protected override void NextLine()
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

            if (index == mayorIndex && mayorIndex != 0)
            {
                MeshRenderer meshRenderer = NPC_Mesh.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.enabled = false;
                } else
                {
                    Debug.LogError("GhostNPC: Can't find meshRenderer component!");
                }

                mayorCamera = GameObject.Find("MayorNPC").transform.Find("CinemachineCamera").gameObject;
                if (mayorCamera != null)
                {
                    mayorCamera.SetActive(true);
                } else
                {
                    Debug.LogError("GhostNPC: Can't find mayorCamera gameobject!");
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
            playerInRange = false;   
            DialogueBox.SetActive(false);
            CinemachineCamera.SetActive(false);
            NPC_Speaking = false;

            if (mayorCamera != null) // last line
            {
                mayorCamera.SetActive(false);
            }

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
            }

            // Re-enable player movement
            playerController.canMove = true;
            // Un-hide player mesh
            playerMesh.SetActive(true);
            weaponMesh.SetActive(true);
        
        }
    }

    IEnumerator TweenPosition(GameObject movingObject, Vector3 targetPos, float duration)
    {
        Vector3 startPosition = movingObject.transform.localPosition;
        float timeElapsed = 0.0f;

        while (timeElapsed < duration)
        {
            // Calculate the interpolation percentage (0 to 1)
            float t = timeElapsed / duration;

            // Interpolate the position
            movingObject.transform.localPosition = Vector3.Lerp(startPosition, targetPos, t);
            
            // Increment time and wait for the next frame
            timeElapsed += Time.deltaTime;
            yield return null; // Wait until the next frame
        }

        // Ensure the object reaches the exact target position
        movingObject.transform.localPosition = targetPos;
    }

}
