using UnityEngine;
using System.Collections;

public class MovingPlatform : MonoBehaviour
{
    private GameObject movingPlatform;
    private Transform position1;
    private Transform position2;

    private bool direction;

    [SerializeField] private float moveDuration;
    [SerializeField] private float pauseDuration;
    private AudioSource audioSource;
    [SerializeField] private AudioClip[] waterMovingSounds;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("MovingPlatform: Can't find audioSource component!");
        }

        movingPlatform = gameObject.transform.Find("Platform").gameObject;
        position1 = gameObject.transform.Find("Position1");
        position2 = gameObject.transform.Find("Position2");

        if (movingPlatform  == null)
        {
            Debug.LogError("MovingPlatform: Can't find movingPlatform child gameobject! Check children and naming.");
        }

        if (position1  == null || position2 == null)
        {
            Debug.LogError("MovingPlatform: Can't find position child gameobject! Check children and naming.");
        }

        InvokeRepeating(nameof(movePlatform), moveDuration + pauseDuration, moveDuration + pauseDuration);
    }

    void movePlatform()
    {
        if (direction) //Change target position based on direction bool
        {
            StartCoroutine(TweenPosition(movingPlatform, position1.localPosition, moveDuration));
        } else
        {
            StartCoroutine(TweenPosition(movingPlatform, position2.localPosition, moveDuration));
        }

        direction = !direction; // flip direction bool after each call
    }

    IEnumerator TweenPosition(GameObject movingObject, Vector3 targetPos, float duration)
    {
        if (waterMovingSounds.Length > 0)
        {
            audioSource.clip = waterMovingSounds[Random.Range(0, waterMovingSounds.Length)];
            audioSource.Play();
        }

        Vector3 startPosition = movingObject.transform.localPosition;
        float timeElapsed = 0.0f;

        while (timeElapsed < duration)
        {
            // Calculate the interpolation percentage (0 to 1)
            float t = timeElapsed / duration;

            // Interpolate the position
            movingObject.transform.localPosition = Vector3.Lerp(startPosition, targetPos, t);
            Physics.SyncTransforms();
            
            // Increment time and wait for the next frame
            timeElapsed += Time.deltaTime;
            yield return null; // Wait until the next frame
        }

        // Ensure the object reaches the exact target position
        movingObject.transform.localPosition = targetPos;
    }
}
