using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TeleportDoor : MonoBehaviour
{
    [SerializeField] string destinationSceneName;
    [SerializeField] GameObject interactImage;
    [SerializeField] GameObject LoadingUI;
    private bool teleporting = false;
    private bool playerInRange = false;

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !teleporting)
        {
            // Show Loading Screen
            LoadingUI.SetActive(true);

            Invoke(nameof(Teleport), 1.5f);
        }
    }

    void Teleport()
    {
        Debug.Log("Teleporting Player to new Scene");
        SceneManager.LoadScene(destinationSceneName, LoadSceneMode.Single);
        teleporting = true;
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
