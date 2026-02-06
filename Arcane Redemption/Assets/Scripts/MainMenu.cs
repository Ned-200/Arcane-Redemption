using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public Button playButton;
    [SerializeField] GameObject LoadingUI;
    private bool teleporting = false;

	void Start () {
        playButton.GetComponent<Button>().onClick.AddListener(OnPlayClick);
	}

	void OnPlayClick(){
		Debug.Log ("You have clicked the button!");

		// Show Loading Screen
        LoadingUI.SetActive(true);

		Invoke(nameof(Teleport), 1.5f);
	}

    void Teleport()
    {
        Debug.Log("Teleporting Player to new Scene");
        SceneManager.LoadScene("JailGraybox", LoadSceneMode.Single);
        teleporting = true;
    }
}
