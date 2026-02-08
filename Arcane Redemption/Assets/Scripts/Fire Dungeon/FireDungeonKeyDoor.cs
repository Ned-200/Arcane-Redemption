using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FireDungeonKeyDoor : MonoBehaviour
{
    [SerializeField] GameObject interactImage;
    [SerializeField] GameObject fireDungeonKey;
    private bool playerInRange = false;

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
        if (playerInRange && Input.GetKeyDown(KeyCode.E) & fireDungeonKey == null)
        {
            Destroy(gameObject);
            Debug.Log("Key opened door");
            interactImage.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player") & fireDungeonKey == null)
        {   
            playerInRange = true;
            Debug.Log("Entered Door range");
            interactImage.SetActive(true);
        }
    }

    protected void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player") & fireDungeonKey == null)
        {
            playerInRange = false;
            Debug.Log("Left Door range");
            interactImage.SetActive(false);
        }
    }
}
