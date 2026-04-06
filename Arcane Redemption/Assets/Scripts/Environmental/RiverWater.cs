using UnityEngine;

public class RiverWater : MonoBehaviour
{

    private bool playerInWater;
    [SerializeField] private GameObject splashEffectPrefab;
    [SerializeField] private AudioClip splashSound;

    private void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {
            playerInWater = true;

            // Play Sound
            if (splashSound != null)
            {
                AudioSource.PlayClipAtPoint(splashSound, other.transform.position);
            }

            // Play Effect
            if (splashEffectPrefab != null)
            {
                GameObject splashEffect = Instantiate(splashEffectPrefab, other.transform.position, splashEffectPrefab.transform.rotation);
                Destroy(splashEffect.GetComponent<Light>());
            }

        }
    }

    private void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {
            playerInWater = false;
        }
    }

}
