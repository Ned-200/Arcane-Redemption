using UnityEngine;

public class PotionPickup : MonoBehaviour
{
    [SerializeField] GameObject interactImage;

    private bool playerInRange = false;

    void Start()
    {
        if (interactImage == null)
        {
            Debug.LogError("Potion can't find interactImage!");
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {

            // ADD POTION TO INVENTORY HERE


            interactImage.SetActive(false);
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {   
            playerInRange = true;
            Debug.Log("Entered Potion range");
            interactImage.SetActive(true);
        }
    }

    protected void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("Left Potion range");
            interactImage.SetActive(false);
        }
    }
}
