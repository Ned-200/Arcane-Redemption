using UnityEngine;

public class PotionBelt : MonoBehaviour
{
    private GameObject manaBottle;
    private GameObject healthBottle;

    [SerializeField] private Material glassMaterial;
    [SerializeField] private Material healthMaterial;
    [SerializeField] private Material manaMaterial;
    private bool showHealthPotion;
    private bool showManaPotion;
    private InventorySystem playerInventory;


    void Start()
    {
        GameObject PlayerData = GameObject.FindWithTag("PlayerData");
        if (PlayerData != null)
        {
            playerInventory = PlayerData.GetComponent<InventorySystem>();
            if (playerInventory == null)
            {
                Debug.LogError("PotionBelt: No InventorySystem component found!");
            }
        } else
        {
            Debug.LogError("PotionPickup: No PlayerData found!");
        }

        manaBottle = this.gameObject.transform.Find("ManaBottle").gameObject;
        if (manaBottle == null)
        {
            Debug.LogError("PotionBelt: Cannot find ManaBottle child object");
        }

        healthBottle = this.gameObject.transform.Find("HealthBottle").gameObject;
        if (healthBottle == null)
        {
            Debug.LogError("PotionBelt: Cannot find HealthBottle child object");
        }

        if (glassMaterial == null || healthMaterial == null || manaMaterial == null)
        {
            Debug.LogError("PotionBelt: Missing assigned material(s)! Check properties!");
        }
    }

    void Update()
    {
        // Health Potions
        if (playerInventory.HealthPotionCount > 0 && !showHealthPotion)
        {
            showHealthPotion = true;
            Renderer rend = healthBottle.GetComponent<Renderer>();
            rend.sharedMaterial = healthMaterial;

            GameObject bottleneck = healthBottle.transform.Find("Bottleneck").gameObject;
            rend = bottleneck.GetComponent<Renderer>();
            rend.sharedMaterial = healthMaterial;

        } else if (playerInventory.HealthPotionCount == 0 && showHealthPotion)
        {
            showHealthPotion = false;
            Renderer rend = healthBottle.GetComponent<Renderer>();
            rend.sharedMaterial = glassMaterial;

            GameObject bottleneck = healthBottle.transform.Find("Bottleneck").gameObject;
            rend = bottleneck.GetComponent<Renderer>();
            rend.sharedMaterial = glassMaterial;
        }

        // Mana Potions
        if (playerInventory.ManaPotionCount > 0 && !showManaPotion)
        {
            showManaPotion = true;
            Renderer rend = manaBottle.GetComponent<Renderer>();
            rend.sharedMaterial = manaMaterial;

            GameObject bottleneck = manaBottle.transform.Find("Bottleneck").gameObject;
            rend = bottleneck.GetComponent<Renderer>();
            rend.sharedMaterial = manaMaterial;

        } else if (playerInventory.ManaPotionCount == 0 && showManaPotion)
        {
            showManaPotion = false;
            Renderer rend = manaBottle.GetComponent<Renderer>();
            rend.sharedMaterial = glassMaterial;

            GameObject bottleneck = manaBottle.transform.Find("Bottleneck").gameObject;
            rend = bottleneck.GetComponent<Renderer>();
            rend.sharedMaterial = glassMaterial;
        }
    }
}
