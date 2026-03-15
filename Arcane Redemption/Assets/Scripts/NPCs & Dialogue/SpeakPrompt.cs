using UnityEngine;

public class SpeakPrompt : MonoBehaviour
{
    private GameObject mainCamera;
    private float lookSpeed = 5.0f;

    void Start()
    {
        // Get camera
        mainCamera = GameObject.FindWithTag("MainCamera");
        if (mainCamera == null) {
            Debug.LogError("SpeakPrompt: Can't find MainCamera! Check Camera Tag.");
        } else {   
            // Set initial rotation
            Vector3 lookDirection = mainCamera.transform.position - this.transform.position;
            lookDirection.Normalize();
            this.transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Make NPC face camera
        Vector3 lookDirection = mainCamera.transform.position - this.transform.position;
        lookDirection.Normalize();
        this.transform.rotation = Quaternion.Slerp(this.transform.rotation, Quaternion.LookRotation(lookDirection), lookSpeed * Time.deltaTime);

    }
}
