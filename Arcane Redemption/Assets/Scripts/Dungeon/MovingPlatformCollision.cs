using UnityEngine;
using System.Collections;

public class MovingPlatformCollision : MonoBehaviour
{
    private GameObject platform;
    private bool playerInRange = false;

    void Start()
    {
        platform = this.gameObject;
        if (platform == null)
        {
            Debug.LogError("PlatformCollision: Platform not found! Script not attached to correct object!");
        }
    }

    void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {   
            other.transform.SetParent(platform.transform);
            playerInRange = true;
            Debug.Log("PlatformCollision: Player on platform");
        }
    }

    protected void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {
            other.transform.SetParent(null);
            playerInRange = false;
            Debug.Log("PlatformCollision: Player off platform");
        }
    }

}
