using UnityEngine;

public class TownAmbience : MonoBehaviour
{
    [SerializeField] public AudioClip townAmbience;
    [SerializeField] public AudioClip desertAmbience;
    private PlayerData playerData;
    private bool playerInRange = false;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) {
            Debug.LogError("TownAmbience: Could not get audioSource component!");
        }

        playerData = GameObject.FindWithTag("PlayerData").GetComponent<PlayerData>();
        if (playerData != null)
        {
            if (playerData.plantBossDefeated)
            {
                audioSource.clip = townAmbience;
                audioSource.Play();
            }
        } else
        {
            Debug.LogError("TownAmbience: Could not find PlayerData!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("Entered town range");

            if (audioSource.clip != townAmbience)
            {
                audioSource.clip = townAmbience;
                audioSource.Play();
            }
        }
    }

    protected void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("Left town range");

            if (!playerData.plantBossDefeated)
            {
                audioSource.clip = desertAmbience;
                audioSource.Play();
            }
        }
    }
}
