using UnityEngine;

public class FireDungeonKey : MonoBehaviour
{
    [SerializeField] GameObject interactImage;

    private bool playerInRange = false;

    void Start()
    {
        if (interactImage == null)
        {
            Debug.LogError("FireDungeonKey can't find interactImage!");
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Destroy(gameObject);
            interactImage.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {   
            playerInRange = true;
            Debug.Log("Entered Key range");
            interactImage.SetActive(true);
        }
    }

    protected void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("Left Key range");
            interactImage.SetActive(false);
        }
    }
}
