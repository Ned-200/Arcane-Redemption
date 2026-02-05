using UnityEngine;
using UnityEngine.SceneManagement;

public class TeleportDoor : MonoBehaviour
{
    [SerializeField] string destinationSceneName;
    [SerializeField] GameObject interactImage;
    private bool teleporting = false;
    private bool playerInRange = false;


    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !teleporting)
        {
            Debug.Log("Teleporting Player to new Scene");
            SceneManager.LoadScene(destinationSceneName, LoadSceneMode.Single);
            teleporting = true;
        }
    }

    void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {   
            playerInRange = true;
            Debug.Log("Entered Door range");
            interactImage.SetActive(true);
        }
    }

    protected void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("Left Door range");
            interactImage.SetActive(false);
        }
    }
}
