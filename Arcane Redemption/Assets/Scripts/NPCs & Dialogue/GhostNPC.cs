using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;

public class GhostNPC : NPC_Character
{
    [SerializeField] protected int mayorIndex = -1;
    [SerializeField] protected int bridgeIndex = -1;

    private GameObject mayorCamera;
    private GameObject bossExitBridge;
    private bool movedOrMoving;
    private Vector3 targetPosition;
    [SerializeField] private AudioClip ghostMusic;
    private AudioSource bossMusicSource;

    protected override void Start()
    {
        base.Start();

        Invoke(nameof(BeginGhostDialogue), 3);
        targetPosition = new Vector3(NPCMesh.transform.localPosition.x, NPCMesh.transform.localPosition.y+8, NPCMesh.transform.localPosition.z);
    
        bossMusicSource = GameObject.FindWithTag("BossMusicSource").GetComponent<AudioSource>();
        if (bossMusicSource != null)
        {
            if (ghostMusic != null) {
                bossMusicSource.clip = ghostMusic;
            } else
            {
                Debug.LogError("GhostNPC: not assigned ghostMusic!");
            }
            bossMusicSource.loop = false;
            bossMusicSource.Stop();
        } else
        {
            Debug.LogError("GhostNPC: Can't find BossMusicSource!");
        }

    }

    private void BeginGhostDialogue()
    {
        CinemachineCamera.SetActive(true);

        if (bossMusicSource != null)
        {
            bossMusicSource.Play();
        }

        Debug.Log("Dialogue Begin");
        if (SpeakImage != null)
        {
            Destroy(SpeakImage);
        }
        StartDialogue();
        NPC_Speaking = true;

        // disable player movement
        playerController.canMove = false;
        playerAnim.SetBool("isWalking", false);
        playerAnim.SetBool("isSprinting", false);
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
                if (mayorIndex != 0) {
                    skipSounds = new AudioClip[0];
                }
                
                movedOrMoving = true;
                StartCoroutine(TweenPosition(NPCMesh, targetPosition, 2));
            }
            // if never made it to target position, set it to it.
            if (movedOrMoving && NPCMesh.transform.localPosition != targetPosition) 
            {
                NPCMesh.transform.localPosition = targetPosition;
            }
            // If index is the index where the camera must pan to the mayor, do that
            if (index == mayorIndex && mayorIndex != 0)
            {
                HideNPCMesh();

                Transform mayorMesh = GameObject.Find("MayorNPC").transform.Find("NPCMesh");
                if (mayorMesh != null)
                {
                    mayorCamera = mayorMesh.Find("CinemachineCamera").gameObject;
                } else
                {
                    Debug.LogError("GhostNPC: Can't find mayor's NPCMesh, so could not get camera within!");
                }

                if (mayorCamera != null)
                {
                    mayorCamera.SetActive(true);
                } else
                {
                    Debug.LogError("GhostNPC: Can't find mayorCamera gameobject!");
                }
            }

            if (index == bridgeIndex && bridgeIndex != 0)
            {
                bossExitBridge = GameObject.Find("BossExitBridge");                
            }
        }
    }

    protected override void NextLine()
    {
        base.NextLine();

        if (mayorCamera != null) {
            if (!CinemachineCamera.activeSelf && mayorCamera.activeSelf) //if ghost cam inactive but mayor camera is, disable mayor camera
            {
                mayorCamera.SetActive(false);
            }
        }

        if (bossExitBridge != null && !CinemachineCamera.activeSelf) //if ghost cam inactive and exit bridge has been assigned, play cutscene
        {
            BossExitBridge bridgeScript = bossExitBridge.GetComponent<BossExitBridge>();
            if (bridgeScript != null)
            {
                bridgeScript.moveBridge();
                HideNPCMesh();
            } else
            {
                Debug.LogError("GhostNPC: Can't find bridgeScript component!");
            }
        }
    }

    protected void HideNPCMesh()
    {
        MeshRenderer meshRenderer = NPCMesh.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        } else
        {
            Debug.LogError("GhostNPC: Can't find meshRenderer component!");
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
