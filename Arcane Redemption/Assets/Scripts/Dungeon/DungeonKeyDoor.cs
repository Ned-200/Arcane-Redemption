using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DungeonKeyDoor : MonoBehaviour
{
    private Image interactImage;
    [SerializeField] GameObject DungeonKey;
    [SerializeField] Vector3 targetPosition;
    [SerializeField] GameObject MovingDoorPart;
    private bool playerInRange = false;
    private bool movedOrMoving;
    [SerializeField] float moveDuration;

    void Start()
    {
        if (DungeonKey == null)
        {
            Debug.LogError("Dungeon door can't find key!");
        }

        GameObject canvas = GameObject.FindWithTag("MainCanvas");
        interactImage = canvas.transform.Find("InteractImage").GetComponent<Image>();
        if (interactImage == null)
        {
            Debug.LogError("Dungeon door can't find interact image!");
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E) & DungeonKey == null & !movedOrMoving)
        {
            movedOrMoving = true;
            Debug.Log("Key opened door");
            interactImage.enabled = false;
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

        if (other.CompareTag("Player") & DungeonKey == null & !movedOrMoving)
        {   
            playerInRange = true;
            Debug.Log("Entered Door range");
            interactImage.enabled = true;
        }
    }

    protected void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player") & DungeonKey == null & !movedOrMoving)
        {
            playerInRange = false;
            Debug.Log("Left Door range");
            interactImage.enabled = false;
        }
    }
}
