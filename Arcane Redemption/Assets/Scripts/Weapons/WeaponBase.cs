using UnityEngine;

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
    [SerializeField] protected AudioClip[] attackSounds;
    [SerializeField] protected AudioClip[] impactSounds;

    protected float lastAttackTime = -999f;
    protected BaseCharacter owner;

    public string WeaponName => weaponName;
    public float Damage => damage;
    public bool CanAttack => Time.time >= lastAttackTime + attackCooldown;

    public virtual void Initialize(BaseCharacter characterOwner)
    {
        owner = characterOwner;
        OnInitialized();
    }

    public virtual bool TryPrimaryAttack()
    {
        if (!CanAttack)
        {
            return false;
        }

        if (owner != null)
        {
            Debug.Log($"[{weaponName}] BEFORE Attack - Stamina: {owner.CurrentStamina:F1}/{owner.MaxStamina} | Mana: {owner.CurrentMana:F1}/{owner.MaxMana}");

            if (staminaCost > 0f && !owner.HasEnoughStamina(staminaCost))
            {
                Debug.LogWarning($"[{weaponName}] NOT ENOUGH STAMINA! Need {staminaCost}, have {owner.CurrentStamina:F1}");
                return false;
            }

            if (manaCost > 0f && !owner.HasEnoughMana(manaCost))
            {
                Debug.LogWarning($"[{weaponName}] NOT ENOUGH MANA! Need {manaCost}, have {owner.CurrentMana:F1}");
                return false;
            }

            if (staminaCost > 0f)
            {
                owner.ConsumeStamina(staminaCost);
            }

            if (manaCost > 0f)
            {
                owner.ConsumeMana(manaCost);
            }
            

            Debug.Log($"[{weaponName}] ✓ Attack Success! AFTER Attack - Stamina: {owner.CurrentStamina:F1}/{owner.MaxStamina} (-{staminaCost}) | Mana: {owner.CurrentMana:F1}/{owner.MaxMana} (-{manaCost})");
        }

        lastAttackTime = Time.time;
        PerformPrimaryAttack();
        return true;
    }

    public virtual bool TrySecondaryAttack()
    {
        return false;
    }

    public virtual void OnEquipped()
    {
        gameObject.SetActive(true);
    }

    public virtual void OnUnequipped()
    {
        gameObject.SetActive(false);
    }

    protected abstract void PerformPrimaryAttack();
    protected virtual void OnInitialized() { }
}