using UnityEngine;

public class TorchLight : MonoBehaviour
{
    [SerializeField] private GameObject fire;
    [SerializeField] AudioClip[] torchSounds;
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
            AudioSource.PlayClipAtPoint(torchSounds[Random.Range(0, torchSounds.Length)], other.transform.position);
        }
    }
}