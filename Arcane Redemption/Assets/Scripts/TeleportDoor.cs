using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TeleportDoor : MonoBehaviour
{
    private GameObject playerDataObject;
    private PlayerData playerData;
    [SerializeField] string destinationSceneName;
    [SerializeField] GameObject interactImage;
    [SerializeField] GameObject LoadingUI;
    private bool teleporting = false;
    private bool playerInRange = false;

    void Start()
    {
        playerDataObject = GameObject.FindWithTag("PlayerData");

        if (playerDataObject != null)
        {
            playerData = playerDataObject.GetComponent<PlayerData>();
        } else
        {
            Debug.LogError("No Player Data in Scene! Check Tag!");
        }
    }

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

        if (playerData != null) {
            playerData.lastScene = SceneManager.GetActiveScene().name;
        }


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
