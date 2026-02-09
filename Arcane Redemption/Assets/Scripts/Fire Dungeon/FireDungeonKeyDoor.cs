using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FireDungeonKeyDoor : MonoBehaviour
{
    [SerializeField] GameObject interactImage;
    [SerializeField] GameObject fireDungeonKey;
    private bool playerInRange = false;

    private bool movedOrMoving;
    private Vector3 targetPosition = new Vector3(17, 10, -10);
    [SerializeField] float moveDuration;

    void Start()
    {
        if (fireDungeonKey == null)
        {
            Debug.LogError("Dungeon door can't find key!");
        }

        if (interactImage == null)
        {
            Debug.LogError("Dungeon door can't find interact image!");
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E) & fireDungeonKey == null & !movedOrMoving)
        {
            movedOrMoving = true;
            Debug.Log("Key opened door");
            interactImage.SetActive(false);
            StartCoroutine(TweenPosition(targetPosition, moveDuration));
        }
    }

    IEnumerator TweenPosition(Vector3 targetPos, float duration)
    {
        Vector3 startPosition = gameObject.transform.localPosition;
        float timeElapsed = 0.0f;

        while (timeElapsed < duration)
        {
            // Calculate the interpolation percentage (0 to 1)
            float t = timeElapsed / duration;

            // Interpolate the position
            gameObject.transform.localPosition = Vector3.Lerp(startPosition, targetPos, t);
            
            // Increment time and wait for the next frame
            timeElapsed += Time.deltaTime;
            yield return null; // Wait until the next frame
        }
        // Ensure the object reaches the exact target position
        gameObject.transform.localPosition  = targetPos;
        Debug.Log("Gem Door Sealed!");
    }

    void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player") & fireDungeonKey == null & !movedOrMoving)
        {   
            playerInRange = true;
            Debug.Log("Entered Door range");
            interactImage.SetActive(true);
        }
    }

    protected void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player") & fireDungeonKey == null & !movedOrMoving)
        {
            playerInRange = false;
            Debug.Log("Left Door range");
            interactImage.SetActive(false);
        }
    }
}
