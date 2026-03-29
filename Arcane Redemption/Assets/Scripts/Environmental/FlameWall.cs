using UnityEngine;

public class FlameWall : MonoBehaviour
{
    private GameObject player;
    private PlayerController playerController;
    private BaseCharacter playerCharacter;
    private bool playerInRange;
    [SerializeField] private GameObject burningPrefab;
    [SerializeField] private float damage = 10.0f;
    [SerializeField] private float cooldownDuration = 2.0f;
    [SerializeField] private AudioClip burnSound;
    private bool damageCooldown;

    void Update()
    {
        if (playerInRange && !damageCooldown)
        {
            // Damage player
            damageCooldown = true;
            playerCharacter.TakeDamage(damage);

            // Run player animations
            playerController.playerAnim.Play("HurtFire", 1); 
            playerController.playerAnim.Play("HurtFire", 2); 

            // Play burning sound
            if (burnSound != null)
            {
                AudioSource.PlayClipAtPoint(burnSound, transform.position);
            }

            // Spawn burning effect
            if (burningPrefab != null)
            {
                GameObject burning = Instantiate(burningPrefab, player.transform.position, burningPrefab.transform.rotation);
                if (player != null) {
                    burning.transform.SetParent(player.transform);
                }
            } else
            {
                Debug.LogError("FlameWall: burningPrefab not assigned!");
            }

            // Set debounce    
            Invoke(nameof(SetCooldown), cooldownDuration);
        }
    }

    void SetCooldown()
    {
        damageCooldown = false;
    }

    void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        // Get player from collision
        if (other.CompareTag("Player"))
        {   
            player = other;
            playerInRange = true;
            Debug.Log("FlameWall: Entered burning range");

            // Get playerController
            playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("FlameWall: playerController not found!");
            }

            // Get playerCharacter
            playerCharacter = player.GetComponent<BaseCharacter>();
            if (playerCharacter == null)
            {
                Debug.LogError("FlameWall: playerCharacter not found!");
            }
        }
    }

    void OnTriggerExit(Collider collision)
    {
        GameObject other = collision.gameObject;

        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("FlameWall: Left burning range");
        }
    }
}
