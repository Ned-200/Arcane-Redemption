using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;

public class GhostNPC : NPC_Character
{
    private GameObject NPC_Mesh;

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
        SpeakImage.SetActive(false);
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
                } else if (index == lines.Length - 1) // last line
                {
                    MeshRenderer meshRenderer = NPC_Mesh.GetComponent<MeshRenderer>();
                    if (meshRenderer != null)
                    {
                        meshRenderer.enabled = false;
                    } else
                    {
                        Debug.LogError("GhostNPC: Can't find meshRenderer component!");
                    }
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
