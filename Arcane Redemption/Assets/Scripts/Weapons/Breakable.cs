using UnityEngine;

public class Breakable : MonoBehaviour
{
    [SerializeField] float despawnDelay;
    [SerializeField] GameObject breakableEffectPrefab;
    [SerializeField] GameObject healthPotionPrefab;
    [SerializeField] GameObject manaPotionPrefab;
    private int potionChance;
    private bool broken = false; // to prevent running multiple times


    void Start()
    {
        if (healthPotionPrefab == null)
        {
            Debug.LogError("Breakable: No healthPotionPrefab found!");
        }
        if (manaPotionPrefab == null)
        {
            Debug.LogError("Breakable: No manaPotionPrefab found!");
        }
        if (breakableEffectPrefab == null)
        {
            Debug.LogError("Breakable: No breaking particle prefab found!");
        }


    }

    public void Break()
    {
        if (broken) return;
        broken = true;

        // Spawn breaking particle effect
        if (breakableEffectPrefab != null)
        {
            Instantiate(breakableEffectPrefab, transform.position, breakableEffectPrefab.transform.rotation);
        } else
        {
            Debug.LogError("Breakable: No breaking particle prefab found!");
        }


        potionChance = Random.Range(0, 10); // One in 5 chance for a potion, 50/50 whether its health or mana

        if (potionChance == 0 || potionChance == 1)
        {
            Invoke(nameof(SpawnPotion), despawnDelay);
        } else
        {
            Destroy(this.gameObject, despawnDelay);
        }

    }

    private void SpawnPotion() // 0 for health, 1 for mana
    {
        if (potionChance == 0)
        {

            if (healthPotionPrefab != null)
            {
                Instantiate(healthPotionPrefab, new Vector3(transform.position.x, transform.position.y-0.6f, transform.position.z), healthPotionPrefab.transform.rotation);
                Debug.Log("Breakable: spawned health potion!");
            } else
            {
                Debug.LogError("Breakable: No health potion prefab found!");
            }

        } else if (potionChance == 1)
        {
            
            if (manaPotionPrefab != null)
            {
                Instantiate(manaPotionPrefab, new Vector3(transform.position.x, transform.position.y-0.6f, transform.position.z), manaPotionPrefab.transform.rotation);
            } else
            {
                Debug.Log("Breakable: spawned mana potion!");
            }

        }

        Destroy(this.gameObject, 0.1f); // Destroy breakable once potion has spawned, with minor delay to be safe
    }
}
