using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class DungeonManager : MonoBehaviour
{    
    [Header("Enemies")]
    [SerializeField] protected GameObject[] enemies;

    [Header("Bridges/Doors")]
    [SerializeField] protected GameObject[] battleLockedDoors;
    protected bool movedOrMoving;
    [SerializeField] protected GameObject teleportDoor;
    [SerializeField] protected float moveDuration = 5.0f;
    
    [Header("Player/EnvironmentDeath")]
    protected int defaultGravity = -20;
    protected int drowningGravity = -15;
    [SerializeField] protected CharacterController characterController;    
    [SerializeField] protected PlayerController playerController;
    [SerializeField] protected GameObject environmentDeathEffectPrefab;
    protected bool dyingToEnvironment;
    [SerializeField] protected bool drownedOrBurned; // true is drowned, false is burned
    public Transform checkpoint;


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    protected IEnumerator TweenPosition(GameObject movingObject, Vector3 targetPos, float duration)
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
    
    protected void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player") && !dyingToEnvironment)
        {   
            dyingToEnvironment = true;
            Debug.Log("Player fell in pit!");
            playerController = other.GetComponent<PlayerController>();
            
            Invoke(nameof(SpawnPlayerAtopPit), 2);
            GameObject environmentDeathEffect = Instantiate(environmentDeathEffectPrefab, characterController.transform.position, environmentDeathEffectPrefab.transform.rotation);
            
            if (drownedOrBurned) // if true, drowned
            {
                environmentDeathEffect.transform.SetParent(other.transform);
                playerController.playerAnim.SetBool("Drowned", true);
                playerController.velocity.y = -5;
                playerController.gravity = drowningGravity;
            } else // if false, burned
            {
                playerController.canMove = false;
                playerController.playerAnim.SetBool("Burned", true);

            }
        }
    }

    protected virtual void SpawnPlayerAtopPit()
    {
        characterController.enabled = false;
        characterController.transform.position = checkpoint.position;
        characterController.enabled = true;

        if (drownedOrBurned) // if true, drowned
        {
            playerController.gravity = defaultGravity;
            playerController.playerAnim.SetBool("Drowned", false);
        } else // if false, burned
        {
            playerController.canMove = true; 
            playerController.playerAnim.SetBool("Burned", false);
        }
        dyingToEnvironment = false;
    }
}
