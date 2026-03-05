using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ElementalGem : MonoBehaviour
{
    
    [SerializeField] int gemElement; // 1 = Fire Ruby, 2 = Water Sapphire, 3 = Plant Emerald
    private Image interactImage;
    private bool playerInRange = false;

    private PlayerData playerData;

    [SerializeField] GameObject gemDoor;
    [SerializeField] int gemDoorHeight; // Y position to set door when it seals after gem is picked up!
    private BoxCollider boxCollider;

    private bool movedOrMoving;
    [SerializeField] float moveDuration;
    
    void Start()
    {
        GameObject playerDataObject = GameObject.FindWithTag("PlayerData");
        playerData = playerDataObject.GetComponent<PlayerData>();

        GameObject canvas = GameObject.FindWithTag("MainCanvas");
        interactImage = canvas.transform.Find("InteractImage").GetComponent<Image>();
        if (interactImage == null)
        {
            Debug.LogError("ElementalGem can't find interactImage!");
        }

        if (gemDoor == null)
        {
            Debug.LogError("ElementalGem can't find gemDoor!");
        } else
        {
            boxCollider = gemDoor.GetComponent<BoxCollider>(); // Invisible barricade to prevent player from running past closing gem door
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E) & !movedOrMoving)
        {
            movedOrMoving = true;
            interactImage.enabled = false;
            Debug.Log("Gem Obtained!");

            // GIVE PLAYER ABILITIES HERE
            if (gemElement == 1) {
                playerData.fireGemObtained = true;
            } else if (gemElement == 2) {
                playerData.waterGemObtained = true;
            } else if (gemElement == 3) {
                playerData.plantGemObtained = true;
            } else {
                Debug.LogError("Gem element int not recognized! Check ElementalGem Script");
            }

            // Start the tweening coroutine
            boxCollider.enabled = true;
            StartCoroutine(TweenPosition(new Vector3(gemDoor.transform.localPosition.x, gemDoorHeight, gemDoor.transform.localPosition.z), moveDuration));
            Invoke(nameof(DestroyGem), moveDuration);
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

    void DestroyGem()
    {
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player") & !movedOrMoving)
        {   
            playerInRange = true;
            Debug.Log("Entered Gem range");
            interactImage.enabled = true;
        }
    }

    protected void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player") & !movedOrMoving)
        {
            playerInRange = false;
            Debug.Log("Left Gem range");
            interactImage.enabled = false;
        }
    }
}
