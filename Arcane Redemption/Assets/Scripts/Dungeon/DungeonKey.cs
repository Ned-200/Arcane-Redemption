using UnityEngine;
using UnityEngine.UI;

public class DungeonKey : MonoBehaviour
{
    private Image interactImage;

    private bool playerInRange = false;

    void Start()
    {
        GameObject canvas = GameObject.FindWithTag("MainCanvas");
        interactImage = canvas.transform.Find("InteractImage").GetComponent<Image>();
        if (interactImage == null)
        {
            Debug.LogError("DungeonKey can't find interactImage!");
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Destroy(gameObject);
            interactImage.enabled = false;
        }
    }

    void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {   
            playerInRange = true;
            Debug.Log("Entered Key range");
            interactImage.enabled = true;
        }
    }

    protected void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("Left Key range");
            interactImage.enabled = false;
        }
    }
}
