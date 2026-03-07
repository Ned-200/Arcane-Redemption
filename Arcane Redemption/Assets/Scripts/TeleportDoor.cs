using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TeleportDoor : MonoBehaviour
{
    protected private GameObject playerDataObject;
    protected private PlayerData playerData;
    [SerializeField] protected string destinationSceneName;
    protected private Image interactImage;
    [SerializeField] protected GameObject LoadingUI;
    protected private bool teleporting = false;
    protected private bool playerInRange = false;

    protected void Start()
    {
        playerDataObject = GameObject.FindWithTag("PlayerData");

        if (playerDataObject != null)
        {
            playerData = playerDataObject.GetComponent<PlayerData>();
        } else
        {
            Debug.LogError("No Player Data in Scene! Check Tag!");
        }

        GameObject canvas = GameObject.FindWithTag("MainCanvas");
        interactImage = canvas.transform.Find("InteractImage").GetComponent<Image>();
        if (interactImage == null)
        {
            Debug.LogError("TeleportDoor: interactImage component not found!");
        }
    }

    protected virtual void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !teleporting)
        {
            // Show Loading Screen
            LoadingUI.SetActive(true);

            Invoke(nameof(Teleport), 1.5f);
        }
    }

    protected void Teleport()
    {
        Debug.Log("Teleporting Player to new Scene");

        if (playerData != null) {
            playerData.lastScene = SceneManager.GetActiveScene().name;
        }


        SceneManager.LoadScene(destinationSceneName, LoadSceneMode.Single);
        teleporting = true;
    }

    protected void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {   
            playerInRange = true;
            Debug.Log("Entered Door range");
            interactImage.enabled = true;
        }
    }

    protected void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("Left Door range");
            interactImage.enabled = false;
        }
    }
}
