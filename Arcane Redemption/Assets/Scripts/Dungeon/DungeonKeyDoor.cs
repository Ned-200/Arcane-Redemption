using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DungeonKeyDoor : MonoBehaviour
{    
    [Header("UI")]
    [SerializeField] private GameObject interactPromptPrefab;
    private GameObject interactPrompt;
    [SerializeField] private Transform promptPosition;
    private GameObject keyUI;
    
    [Header("References")]
    [SerializeField] DungeonKey dungeonKey;
    [SerializeField] Vector3 targetPosition;
    [SerializeField] GameObject MovingDoorPart;
    private bool playerInRange = false;
    public bool movedOrMoving;
    [SerializeField] float moveDuration;

    void Start()
    {
        if (dungeonKey == null)
        {
            Debug.LogError("DungeonKeyDoor: dungeonKey not assigned!");
        }

        // Get Key UI
        GameObject canvas = GameObject.FindWithTag("MainCanvas");
        GameObject inventoryMenu = canvas.transform.Find("InventoryMenu").gameObject;
        if (inventoryMenu != null)
        {
            keyUI = inventoryMenu.transform.Find("Key").gameObject;
            if (keyUI == null)
            {
                Debug.LogError("DungeonKeyDoor: Could not find keyUI! Check naming and children!");
            }
        } else {
            Debug.LogError("DungeonKeyDoor: Could not find InventoryMenu! Check naming and children!");
        }

        // Get interactPrompt prefab
        if (interactPromptPrefab == null)
        {
            Debug.LogError("DungeonKeyDoor: interactPromptPrefab not assigned! Please assign the prefab.");
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && dungeonKey.pickedUp && !movedOrMoving)
        {
            movedOrMoving = true; // prevent repeated interaction
            keyUI.GetComponent<Image>().enabled = true; // hide key once more, since it was used
            Debug.Log("Key opened door");
            Destroy(interactPrompt);
            StartCoroutine(TweenPosition(targetPosition, moveDuration));
        }
    }

    IEnumerator TweenPosition(Vector3 targetPos, float duration)
    {
        Vector3 startPosition = MovingDoorPart.transform.localPosition;
        float timeElapsed = 0.0f;

        while (timeElapsed < duration)
        {
            // Calculate the interpolation percentage (0 to 1)
            float t = timeElapsed / duration;

            // Interpolate the position
            MovingDoorPart.transform.localPosition = Vector3.Lerp(startPosition, targetPos, t);
            
            // Increment time and wait for the next frame
            timeElapsed += Time.deltaTime;
            yield return null; // Wait until the next frame
        }
        // Ensure the object reaches the exact target position
        MovingDoorPart.transform.localPosition  = targetPos;
        Debug.Log("Gem Door Sealed!");
    }

    void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player") && dungeonKey.pickedUp & !movedOrMoving)
        {   
            playerInRange = true;
            Debug.Log("Entered Door range");
            if (interactPromptPrefab != null)
            {
                interactPrompt = Instantiate(interactPromptPrefab, new Vector3(promptPosition.position.x, promptPosition.position.y, promptPosition.position.z), promptPosition.rotation);
            } else {
                Debug.LogError("PotionPickup: Interact Prompt prefab not assigned! " + this.gameObject.name);
            }
        }
    }

    protected void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player") && dungeonKey.pickedUp & !movedOrMoving)
        {
            playerInRange = false;
            Debug.Log("Left Door range");
            Destroy(interactPrompt);
        }
    }
}
