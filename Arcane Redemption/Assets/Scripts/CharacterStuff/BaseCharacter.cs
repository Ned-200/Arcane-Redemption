using UnityEngine;

/// <summary>
/// Base class for all characters (Player, Enemy, NPC)
/// Manages core stats: Health, Stamina, Mana
/// Manages single weapon slot
/// </summary>
public class BaseCharacter : MonoBehaviour
{
    #region Serialized Fields

    [Header("Character Stats")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float maxStamina = 100f;
    [SerializeField] protected float maxMana = 100f;

    [Header("Regeneration")]
    [SerializeField] protected float staminaRegenRate = 20f;
    [SerializeField] protected float manaRegenRate = 10f;
    [SerializeField] protected float staminaRegenDelay = 1f;
    [SerializeField] protected float manaRegenDelay = 2f;

    [Header("Weapon Slot")]
    [SerializeField] protected Transform weaponSlotTransform;

    #endregion

    #region Private Fields

    // Current stat values
    protected float currentHealth;
    protected float currentStamina;
    protected float currentMana;

    // Regeneration timers
    private float staminaRegenTimer;
    private float manaRegenTimer;

    // Single weapon slot
    protected HandSlot weaponSlot;

    #endregion

    #region Public Properties

    // Health properties
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercent => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public bool IsAlive => currentHealth > 0f;

    // Stamina properties
    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public float StaminaPercent => maxStamina > 0 ? currentStamina / maxStamina : 0f;

    // Mana properties
    public float CurrentMana => currentMana;
    public float MaxMana => maxMana;
    public float ManaPercent => maxMana > 0 ? currentMana / maxMana : 0f;

    // Weapon slot properties
    public HandSlot WeaponSlot => weaponSlot;
    public GameObject EquippedWeapon => weaponSlot?.EquippedItem;
    public bool IsWeaponSlotEmpty => weaponSlot?.IsEmpty ?? true;

    #endregion

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        InitializeStats();
        InitializeWeaponSlot();
    }

    protected virtual void Update()
    {
        UpdateStaminaRegeneration();
        UpdateManaRegeneration();
    }

    #endregion

    #region Initialization

    private void InitializeStats()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        currentMana = maxMana;
        staminaRegenTimer = 0f;
        manaRegenTimer = 0f;
    }

    protected virtual void InitializeWeaponSlot()
    {
        weaponSlot = new HandSlot(weaponSlotTransform, "Weapon Slot");

        if (weaponSlotTransform == null)
        {
            Debug.LogWarning($"{gameObject.name}: Weapon slot transform is not assigned!", this);
        }
    }

    #endregion

    #region Stamina Management

    public virtual bool TryConsumeStamina(float amount)
    {
        if (amount < 0f)
        {
            Debug.LogWarning($"{gameObject.name}: Cannot consume negative stamina amount: {amount}", this);
            return false;
        }

        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            currentStamina = Mathf.Max(0f, currentStamina);
            staminaRegenTimer = staminaRegenDelay;
            OnStaminaConsumed(amount);
            return true;
        }

        return false;
    }

    public virtual void ConsumeStamina(float amount)
    {
        if (amount < 0f)
        {
            Debug.LogWarning($"{gameObject.name}: Cannot consume negative stamina amount: {amount}", this);
            return;
        }

        currentStamina -= amount;
        currentStamina = Mathf.Max(0f, currentStamina);
        staminaRegenTimer = staminaRegenDelay;
        OnStaminaConsumed(amount);
    }

    public bool HasEnoughStamina(float amount)
    {
        return currentStamina >= amount;
    }

    public virtual void RestoreStamina(float amount)
    {
        if (amount < 0f)
        {
            Debug.LogWarning($"{gameObject.name}: Cannot restore negative stamina amount: {amount}", this);
            return;
        }

        float previousStamina = currentStamina;
        currentStamina = Mathf.Min(currentStamina + amount, maxStamina);
        
        if (currentStamina > previousStamina)
        {
            OnStaminaRestored(currentStamina - previousStamina);
        }
    }

    private void UpdateStaminaRegeneration()
    {
        if (staminaRegenTimer > 0f)
        {
            staminaRegenTimer -= Time.deltaTime;
            return;
        }

        if (currentStamina < maxStamina)
        {
            float regenAmount = staminaRegenRate * Time.deltaTime;
            float previousStamina = currentStamina;
            currentStamina = Mathf.Min(currentStamina + regenAmount, maxStamina);
            
            if (currentStamina > previousStamina)
            {
                OnStaminaRegenerated(currentStamina - previousStamina);
            }
        }
    }

    #endregion

    #region Mana Management

    public virtual bool TryConsumeMana(float amount)
    {
        if (amount < 0f)
        {
            Debug.LogWarning($"{gameObject.name}: Cannot consume negative mana amount: {amount}", this);
            return false;
        }

        if (currentMana >= amount)
        {
            currentMana -= amount;
            currentMana = Mathf.Max(0f, currentMana);
            manaRegenTimer = manaRegenDelay;
            OnManaConsumed(amount);
            return true;
        }

        return false;
    }

    public virtual void ConsumeMana(float amount)
    {
        if (amount < 0f)
        {
            Debug.LogWarning($"{gameObject.name}: Cannot consume negative mana amount: {amount}", this);
            return;
        }

        currentMana -= amount;
        currentMana = Mathf.Max(0f, currentMana);
        manaRegenTimer = manaRegenDelay;
        OnManaConsumed(amount);
    }

    public bool HasEnoughMana(float amount)
    {
        return currentMana >= amount;
    }

    public virtual void RestoreMana(float amount)
    {
        if (amount < 0f)
        {
            Debug.LogWarning($"{gameObject.name}: Cannot restore negative mana amount: {amount}", this);
            return;
        }

        float previousMana = currentMana;
        currentMana = Mathf.Min(currentMana + amount, maxMana);
        
        if (currentMana > previousMana)
        {
            OnManaRestored(currentMana - previousMana);
        }
    }

    private void UpdateManaRegeneration()
    {
        if (manaRegenTimer > 0f)
        {
            manaRegenTimer -= Time.deltaTime;
            return;
        }

        if (currentMana < maxMana)
        {
            float regenAmount = manaRegenRate * Time.deltaTime;
            float previousMana = currentMana;
            currentMana = Mathf.Min(currentMana + regenAmount, maxMana);
            
            if (currentMana > previousMana)
            {
                OnManaRegenerated(currentMana - previousMana);
            }
        }
    }

    #endregion

    #region Health Management

    public virtual void TakeDamage(float damage)
    {
        if (damage < 0f)
        {
            Debug.LogWarning($"{gameObject.name}: Cannot take negative damage: {damage}", this);
            return;
        }

        if (!IsAlive) return;

        float previousHealth = currentHealth;
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);

        OnDamageTaken(damage);

        if (!IsAlive && previousHealth > 0f)
        {
            OnDeath();
        }
    }

    public virtual void Heal(float amount)
    {
        if (amount < 0f)
        {
            Debug.LogWarning($"{gameObject.name}: Cannot heal negative amount: {amount}", this);
            return;
        }

        if (!IsAlive) return;

        float previousHealth = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        
        if (currentHealth > previousHealth)
        {
            OnHealed(currentHealth - previousHealth);
        }
    }

    #endregion

    #region Weapon Management

    public virtual bool EquipWeapon(GameObject weaponPrefab)
    {
        if (weaponSlot == null)
        {
            Debug.LogError($"{gameObject.name}: Weapon slot not initialized!", this);
            return false;
        }

        bool success = weaponSlot.Equip(weaponPrefab);
        if (success)
        {
            OnWeaponEquipped(weaponPrefab);
        }
        return success;
    }

    public virtual void UnequipWeapon()
    {
        if (weaponSlot != null)
        {
            GameObject weapon = weaponSlot.EquippedItem;
            weaponSlot.Unequip();
            if (weapon != null)
            {
                OnWeaponUnequipped(weapon);
            }
        }
    }

    #endregion

    #region Virtual Event Methods

    // Stamina events
    protected virtual void OnStaminaConsumed(float amount) { }
    protected virtual void OnStaminaRestored(float amount) { }
    protected virtual void OnStaminaRegenerated(float amount) { }

    // Mana events
    protected virtual void OnManaConsumed(float amount) { }
    protected virtual void OnManaRestored(float amount) { }
    protected virtual void OnManaRegenerated(float amount) { }

    // Health events
    protected virtual void OnDamageTaken(float damage) { }
    protected virtual void OnHealed(float amount) { }
    protected virtual void OnDeath() { }

    // Weapon events
    protected virtual void OnWeaponEquipped(GameObject weapon) { }
    protected virtual void OnWeaponUnequipped(GameObject weapon) { }

    #endregion
}