using UnityEngine;
using System.Collections;

public class FireGem : MonoBehaviour
{
    [SerializeField] GameObject interactImage;

    private bool playerInRange = false;

    [SerializeField] GameObject gemDoor;
    private BoxCollider boxCollider;

    private bool movedOrMoving;
    [SerializeField] float moveDuration;
    
    void Start()
    {
        if (interactImage == null)
        {
            Debug.LogError("FireGem can't find interactImage!");
        }

        if (gemDoor == null)
        {
            Debug.LogError("FireGem can't find gemDoor!");
        } else
        {
            boxCollider = gemDoor.GetComponent<BoxCollider>();
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E) & !movedOrMoving)
        {
            movedOrMoving = true;
            interactImage.SetActive(false);
            Debug.Log("Fire Gem Obtained!");

            // GIVE PLAYER FIRE ABILITIES HERE

            // Start the tweening coroutine
            boxCollider.enabled = true;
            StartCoroutine(TweenPosition(new Vector3(gemDoor.transform.localPosition.x, 3, gemDoor.transform.localPosition.z), moveDuration));
            Invoke(nameof(DestroyFireGem), moveDuration);
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

    void DestroyFireGem()
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
            interactImage.SetActive(true);
        }
    }

    protected void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player") & !movedOrMoving)
        {
            playerInRange = false;
            Debug.Log("Left Gem range");
            interactImage.SetActive(false);
        }
    }
}
