using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Unity.Cinemachine;

public class ElementalGem : MonoBehaviour
{
    
    [Header("References")]
    [SerializeField] int gemElement; // 1 = Fire Ruby, 2 = Water Sapphire, 3 = Plant Emerald
    [SerializeField] private GameObject interactPromptPrefab;
    [SerializeField] private Transform promptPosition;
    private GameObject interactPrompt;
    private bool playerInRange = false;
    private PlayerData playerData;

    [Header("Gem Rendering")]
    [SerializeField] private GameObject gemMesh;
    [SerializeField] private GameObject gemEffect;

    [Header("Gem Door")]
    [SerializeField] GameObject gemDoor;    
    private BoxCollider boxCollider;
    [SerializeField] int gemDoorHeight; // Y position to set door when it seals after gem is picked up!
    private bool movedOrMoving;
    [SerializeField] float moveDuration;
    
    void Start()
    {
        GameObject playerDataObject = GameObject.FindWithTag("PlayerData");
        playerData = playerDataObject.GetComponent<PlayerData>();

        if (interactPromptPrefab == null)
        {
            Debug.LogError("ElementalGem can't find interactPromptPrefab!");
        }
        if (promptPosition == null)
        {
            Debug.LogError("ElementalGem: promptPosition not assigned!");
        }

        if (gemDoor == null)
        {
            Debug.LogError("ElementalGem can't find gemDoor!");
        } else
        {
            boxCollider = gemDoor.GetComponent<BoxCollider>(); // Invisible barricade to prevent player from running past closing gem door
        }

        if (gemMesh == null)
        {
            Debug.LogError("ElementalGem: gemMesh not assigned!");
        }
        if (gemEffect == null)
        {
            Debug.LogError("ElementalGem: gemEffect not assigned!");
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E) & !movedOrMoving)
        {
            movedOrMoving = true;
            Destroy(interactPrompt);
            Debug.Log("Gem Obtained!");

            // GIVE PLAYER ABILITIES HERE
            if (gemElement == 1) {
                playerData.fireGemObtained = true;
            } else if (gemElement == 2) {
                playerData.waterGemObtained = true;
            } else if (gemElement == 3) {
                playerData.plantGemObtained = true;
            } else {
                Debug.LogError("ElementalGem: Gem element int not recognized!");
            }


            if (gemElement != 3) {
                // Start the tweening coroutine
                boxCollider.enabled = true;
                StartCoroutine(TweenPosition(new Vector3(gemDoor.transform.localPosition.x, gemDoorHeight, gemDoor.transform.localPosition.z), moveDuration));
                
                CinemachineImpulseSource impulseSource = gemDoor.GetComponent<CinemachineImpulseSource>();
                if (impulseSource) {
                    impulseSource.GenerateImpulse(0.5f);
                }
            } else
            {
                DisintegrateUP bridgeDisintegrate = gemDoor.GetComponent<DisintegrateUP>();
                if (bridgeDisintegrate != null)
                {
                    bridgeDisintegrate.TriggerDisintegration(true);
                } else
                {
                    Debug.LogError("ElementalGem: Could not fetch bridgeDisintegrate component!");
                }
            }
            
            gemMesh.SetActive(false);
            gemEffect.SetActive(false);

        }
    }
    IEnumerator TweenPosition(Vector3 targetPos, float duration)
    {
        Vector3 startPosition = gemDoor.transform.localPosition;
        float timeElapsed = 0.0f;

        while (timeElapsed < duration)
        {
            // Calculate the interpolation percentage (0 to 1)
            float t = timeElapsed / duration;

            // Interpolate the position
            gemDoor.transform.localPosition = Vector3.Lerp(startPosition, targetPos, t);
            
            // Increment time and wait for the next frame
            timeElapsed += Time.deltaTime;
            yield return null; // Wait until the next frame
        }
        // Ensure the object reaches the exact target position
        gemDoor.transform.localPosition  = targetPos;
        Debug.Log("Gem Door Sealed!");
    }

    void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player") & !movedOrMoving)
        {   
            playerInRange = true;
            Debug.Log("Entered Gem range");
            if (interactPromptPrefab != null)
            {
                interactPrompt = Instantiate(interactPromptPrefab, promptPosition.position, promptPosition.rotation);
            } else {
                Debug.LogError("ElementalGem: Interact Prompt prefab not assigned! " + this.gameObject.name);
            }
        }
    }

    protected void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player") & !movedOrMoving)
        {
            playerInRange = false;
            Debug.Log("Left Gem range");
            Destroy(interactPrompt);
        }
    }
}
