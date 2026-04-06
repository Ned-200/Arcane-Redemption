using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public Button playButton;
    [SerializeField] GameObject LoadingUI;
    [SerializeField] GameObject controls;
    private bool teleporting = false;
    
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] hoverSounds;
    [SerializeField] private AudioClip[] clickSounds;
    [SerializeField] private AudioClip quitSound;


	void Start ()
    {
        
    }

    void Update ()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (controls.activeSelf)
            {
                PlayClickSounds();
                controls.SetActive(false);
            }
        }
    }

	public void PlayGame(){
		Debug.Log ("Play Game");

        PlayClickSounds();

		// Show Loading Screen
        LoadingUI.SetActive(true);

        if (!teleporting) {
            teleporting = true;
		    Invoke(nameof(Teleport), 1.5f);
        }
	}

    public void ToggleControls()
    {
        PlayClickSounds();
        if (controls.activeSelf) {
            controls.SetActive(false);
        } else
        {
            controls.SetActive(true);
        }
    }

    void Teleport()
    {
        Debug.Log("Teleporting Player to new Scene");
        SceneManager.LoadScene("JailGraybox", LoadSceneMode.Single);
    }

    public void QuitGame()
    {
        if (quitSound != null) {
            audioSource.PlayOneShot(quitSound);
        } else {
            Debug.LogError("InventoryUI: Not assigned quit sound.");
        }

        Debug.Log("QUITTING GAME");
        Application.Quit();
    }

    public void PlayClickSounds()
    {
        if (clickSounds.Length > 0) {
            audioSource.PlayOneShot(clickSounds[Random.Range(0, clickSounds.Length)]);
        } else {
            Debug.LogError("InventoryUI: Not assigned click sounds.");
        }
    }

    public void PlayHoverSounds()
    {
        if (hoverSounds.Length > 0) {
            audioSource.PlayOneShot(hoverSounds[Random.Range(0, hoverSounds.Length)]);
        } else {
            Debug.LogError("InventoryUI: Not assigned hover sounds.");
        }
    }
}
