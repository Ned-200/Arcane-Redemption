using UnityEngine;

public class TorchLight : MonoBehaviour
{
    [SerializeField] private GameObject fire;

    private bool activated = false;

    private void Start()
    {
        if (fire != null)
            fire.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activated) return;

        if (other.CompareTag("Player"))
        {
            fire.SetActive(true);
            activated = true; // ensures it's permanent
        }
    }
}