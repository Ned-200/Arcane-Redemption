using UnityEngine;

/// <summary>
/// Manages player's potion inventory
/// Handles picking up and consuming potions
/// </summary>
public class InventorySystem : MonoBehaviour
{
    [Header("Inventory")]
    [SerializeField] private int maxHealthPotions = 10;
    [SerializeField] private int maxManaPotions = 10;
    
    [Header("Potion Types")]
    [SerializeField] private PotionType healthPotionType;
    [SerializeField] private PotionType manaPotionType;
    
    [Header("Input")]
    [SerializeField] private KeyCode healthPotionKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode manaPotionKey = KeyCode.Alpha2;
    
    [Header("References")]
    private BaseCharacter character;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Current potion counts
    [SerializeField] private int healthPotionCount = 0;
    [SerializeField] private int manaPotionCount = 0;
    
    public int HealthPotionCount => healthPotionCount;
    public int ManaPotionCount => manaPotionCount;
    
    private void Awake()
    {
        // Get Player
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            character = player.GetComponent<BaseCharacter>();
            if (character == null)
            {
                Debug.LogError("InventorySystem: No BaseCharacter component found!");
            }
        }
    }
    
    private void Update()
    {
        HandlePotionInput();
    }
    
    private void HandlePotionInput()
    {
        // Press 1 to use health potion
        if (Input.GetKeyDown(healthPotionKey))
        {
            UseHealthPotion();
        }
        
        // Press 2 to use mana potion
        if (Input.GetKeyDown(manaPotionKey))
        {
            UseManaPotion();
        }
    }
    
    /// <summary>
    /// Add a potion to the inventory
    /// </summary>
    public bool AddPotion(PotionType potionType)
    {
        if (potionType == null)
        {
            Debug.LogWarning("InventorySystem: Cannot add null potion type!");
            return false;
        }
        
        if (potionType.isHealthPotion)
        {
            return AddHealthPotion();
        }
        else if (potionType.isManaPotion)
        {
            return AddManaPotion();
        }
        
        Debug.LogWarning($"InventorySystem: Potion type '{potionType.potionName}' is neither health nor mana potion!");
        return false;
    }
    
    /// <summary>
    /// Add a health potion to inventory
    /// </summary>
    public bool AddHealthPotion()
    {
        if (healthPotionCount >= maxHealthPotions)
        {
            if (showDebugLogs)
            {
                Debug.Log("InventorySystem: Health potion inventory full!");
            }
            return false;
        }
        
        healthPotionCount++;
        
        if (showDebugLogs)
        {
            Debug.Log($"InventorySystem: Picked up Health Potion! ({healthPotionCount}/{maxHealthPotions})");
        }
        
        return true;
    }
    
    /// <summary>
    /// Add a mana potion to inventory
    /// </summary>
    public bool AddManaPotion()
    {
        if (manaPotionCount >= maxManaPotions)
        {
            if (showDebugLogs)
            {
                Debug.Log("InventorySystem: Mana potion inventory full!");
            }
            return false;
        }
        
        manaPotionCount++;
        
        if (showDebugLogs)
        {
            Debug.Log($"InventorySystem: Picked up Mana Potion! ({manaPotionCount}/{maxManaPotions})");
        }
        
        return true;
    }
    
    /// <summary>
    /// Use a health potion from inventory
    /// </summary>
    public void UseHealthPotion()
    {
        if (healthPotionCount <= 0)
        {
            if (showDebugLogs)
            {
                Debug.Log("InventorySystem: No health potions available!");
            }
            return;
        }
        
        if (character == null)
        {
            Debug.LogError("InventorySystem: No character reference to heal!");
            return;
        }
        
        // Check if player is already at full health
        if (character.CurrentHealth >= character.MaxHealth)
        {
            if (showDebugLogs)
            {
                Debug.Log("InventorySystem: Already at full health!");
            }
            return;
        }
        
        // Consume the potion
        healthPotionCount--;
        
        // Restore health
        float restoreAmount = healthPotionType != null ? healthPotionType.restoreAmount : 50f;
        character.Heal(restoreAmount);
        
        if (showDebugLogs)
        {
            Debug.Log($"InventorySystem: Used Health Potion! Restored {restoreAmount} HP. ({healthPotionCount}/{maxHealthPotions} remaining)");
        }
        
        // TODO: Play potion use sound
        // TODO: Play potion use VFX
    }
    
    /// <summary>
    /// Use a mana potion from inventory
    /// </summary>
    public void UseManaPotion()
    {
        if (manaPotionCount <= 0)
        {
            if (showDebugLogs)
            {
                Debug.Log("InventorySystem: No mana potions available!");
            }
            return;
        }
        
        if (character == null)
        {
            Debug.LogError("InventorySystem: No character reference to restore mana!");
            return;
        }
        
        // Check if player is already at full mana
        if (character.CurrentMana >= character.MaxMana)
        {
            if (showDebugLogs)
            {
                Debug.Log("InventorySystem: Already at full mana!");
            }
            return;
        }
        
        // Consume the potion
        manaPotionCount--;
        
        // Restore mana
        float restoreAmount = manaPotionType != null ? manaPotionType.restoreAmount : 50f;
        character.RestoreMana(restoreAmount);
        
        if (showDebugLogs)
        {
            Debug.Log($"InventorySystem: Used Mana Potion! Restored {restoreAmount} Mana. ({manaPotionCount}/{maxManaPotions} remaining)");
        }
        
        // TODO: Play potion use sound
        // TODO: Play potion use VFX
    }
}