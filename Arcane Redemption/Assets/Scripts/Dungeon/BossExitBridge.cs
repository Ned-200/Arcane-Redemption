using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class BossExitBridge : MonoBehaviour
{
    
    [SerializeField] private GameObject bridgeCamera;
    private CinemachineImpulseSource impulseSource;
    [SerializeField] private int moveElevation;
    [SerializeField] private int moveDuration;
    private AudioSource audioSource;
    [SerializeField] private AudioClip moveSound;
    private bool movedOrMoving;
    

    void Start()
    {
        if (bridgeCamera == null)
        {
            Debug.LogError("BossExitBridge: bridgeCamera not assigned!");
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("BossExitBridge: audioSource not found! Check components!");
        }

        if (moveSound == null)
        {
            Debug.LogError("BossExitBridge: moveSound not assigned!");
        }

        impulseSource = GetComponent<CinemachineImpulseSource>();
        if (impulseSource == null)
        {
            Debug.LogError("BossExitBridge: impulseSource not found! Check components!");
        }
    }

    public void moveBridge()
    {
        if (!movedOrMoving) {
            movedOrMoving = true;
            bridgeCamera.SetActive(true);
            StartCoroutine(TweenPosition(this.gameObject, new Vector3(transform.position.x, moveElevation, transform.position.z), moveDuration));
                        
            if (impulseSource != null) {
                impulseSource.GenerateImpulse(1.5f);
            }
            
            if (moveSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(moveSound);
            }

            Invoke(nameof(disableBridgeCamera), moveDuration);
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

    public void disableBridgeCamera()
    {
        bridgeCamera.SetActive(false);
    }
}
