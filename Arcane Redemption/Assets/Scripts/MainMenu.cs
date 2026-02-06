using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public Button playButton;
    public Button controlsButton;
    [SerializeField] GameObject LoadingUI;
    [SerializeField] GameObject Controls;
    private bool teleporting = false;

	void Start () 
    {
        playButton.GetComponent<Button>().onClick.AddListener(OnPlayClick);
        controlsButton.GetComponent<Button>().onClick.AddListener(OnControlsClick);
	}

    void Update ()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Controls.activeSelf)
            {
                Controls.SetActive(false);
            }
        }
    }

	void OnPlayClick(){
		Debug.Log ("Play Button Clicked");

		// Show Loading Screen
        LoadingUI.SetActive(true);

		Invoke(nameof(Teleport), 1.5f);
	}

    void OnControlsClick() 
    {
		Debug.Log ("Controls Button Clicked");

		// Show Controls Screen
        Controls.SetActive(true);
	}

    void Teleport()
    {
        Debug.Log("Teleporting Player to new Scene");
        SceneManager.LoadScene("JailGraybox", LoadSceneMode.Single);
        teleporting = true;
    }
}
