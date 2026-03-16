using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;

public class GhostNPC : NPC_Character
{
    [SerializeField] protected int mayorIndex;
    private GameObject mayorCamera;
    private bool movedOrMoving;
    protected override void Start()
    {
        base.Start();

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
        base.Update();

        if (Input.GetMouseButtonDown(0) & NPC_Speaking)
        {
            // If cutscene index, make the Ghost rise as the camera pans up
            if (index == cutsceneLine[cutsceneIndex] && !movedOrMoving)
            {
                movedOrMoving = true;
                StartCoroutine(TweenPosition(NPCMesh, new Vector3(NPCMesh.transform.localPosition.x, NPCMesh.transform.localPosition.y+8, NPCMesh.transform.localPosition.z), 2));
            }
            // If index is the index where the camera must pan to the mayor, do that
            if (index == mayorIndex && mayorIndex != 0)
            {
                MeshRenderer meshRenderer = NPCMesh.GetComponent<MeshRenderer>();
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
