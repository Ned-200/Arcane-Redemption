using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public Button playButton;
    [SerializeField] GameObject LoadingUI;
    [SerializeField] GameObject controls;
    private bool teleporting = false;

	void Start ()
    {
        
    }

    void Update ()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (controls.activeSelf)
            {
                controls.SetActive(false);
            }
        }
    }

	public void PlayGame(){
		Debug.Log ("Play Game");

		// Show Loading Screen
        LoadingUI.SetActive(true);

        if (!teleporting) {
            teleporting = true;
		    Invoke(nameof(Teleport), 1.5f);
        }
	}

    public void ToggleControls()
    {
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
        Debug.Log("QUITTING GAME");
        Application.Quit();
    }

}
