using UnityEngine;

/// <summary>
/// Abstract base class for all weapons
/// Defines common weapon behavior and properties
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    [Header("Weapon Info")]
    [SerializeField] protected string weaponName = "Weapon";
    [SerializeField] protected float damage = 10f;
    
    [Header("Attack Settings")]
    [SerializeField] protected float attackCooldown = 1f;
    [SerializeField] protected float staminaCost = 5f;
    [SerializeField] protected float manaCost = 0f;

    [Header("Audio")]
    [SerializeField] protected AudioClip attackSound;
    [SerializeField] protected AudioClip impactSound;

    protected float lastAttackTime = -999f;
    protected BaseCharacter owner;

    // Public properties
    public string WeaponName => weaponName;
    public float Damage => damage;
    public bool CanAttack => Time.time >= lastAttackTime + attackCooldown;

    /// <summary>
    /// Initialize weapon with owner reference
    /// </summary>
    public virtual void Initialize(BaseCharacter characterOwner)
    {
        owner = characterOwner;
        OnInitialized();
    }

    /// <summary>
    /// Attempt to perform primary attack
    /// </summary>
    public virtual bool TryPrimaryAttack()
    {
        // Check cooldown
        if (!CanAttack)
        {
            return false;
        }

        // Check if owner exists
        if (owner != null)
        {
            // Log stats BEFORE attack
            Debug.Log($"[{weaponName}] BEFORE Attack - Stamina: {owner.CurrentStamina:F1}/{owner.MaxStamina} | Mana: {owner.CurrentMana:F1}/{owner.MaxMana}");

            // Check stamina
            if (staminaCost > 0f && !owner.HasEnoughStamina(staminaCost))
            {
                Debug.LogWarning($"[{weaponName}] ❌ NOT ENOUGH STAMINA! Need {staminaCost}, have {owner.CurrentStamina:F1}");
                return false;
            }

            // Check mana - FIXED: Actually prevent attack if not enough mana
            if (manaCost > 0f && !owner.HasEnoughMana(manaCost))
            {
                Debug.LogWarning($"[{weaponName}] ❌ NOT ENOUGH MANA! Need {manaCost}, have {owner.CurrentMana:F1}");
                return false;
            }

            // Consume resources ONLY if we have enough of both
            if (staminaCost > 0f)
            {
                owner.ConsumeStamina(staminaCost);
            }

            if (manaCost > 0f)
            {
                owner.ConsumeMana(manaCost);
            }

            // Log stats AFTER consumption
            Debug.Log($"[{weaponName}] ✓ Attack Success! AFTER Attack - Stamina: {owner.CurrentStamina:F1}/{owner.MaxStamina} (-{staminaCost}) | Mana: {owner.CurrentMana:F1}/{owner.MaxMana} (-{manaCost})");
        }

        // Attack successful - perform it
        lastAttackTime = Time.time;
        PerformPrimaryAttack();
        return true;
    }

    /// <summary>
    /// Attempt to perform secondary attack
    /// </summary>
    public virtual bool TrySecondaryAttack()
    {
        return false; // Default: no secondary attack
    }

    /// <summary>
    /// Called when weapon is equipped
    /// </summary>
    public virtual void OnEquipped()
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Called when weapon is unequipped
    /// </summary>
    public virtual void OnUnequipped()
    {
        gameObject.SetActive(false);
    }

    // Abstract methods to be implemented by derived classes
    protected abstract void PerformPrimaryAttack();
    protected virtual void OnInitialized() { }
}