using UnityEngine;

public class BaseCharacter : MonoBehaviour
{
    [Header("Character Stats")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float maxStamina = 100f;
    [SerializeField] protected float maxMana = 100f;

    protected float currentHealth;
    protected float currentStamina;
    protected float currentMana;

    [Header("Regeneration")]
    [SerializeField] protected float staminaRegenRate = 20f;
    [SerializeField] protected float manaRegenRate = 10f;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        currentMana = maxMana;
    }

    protected virtual void Update()
    {
        RegenerateStamina();
        RegenerateMana();
    }

    protected virtual void RegenerateStamina()
    {
        if (currentStamina < maxStamina)
        {
            currentStamina = Mathf.Min(currentStamina + staminaRegenRate * Time.deltaTime, maxStamina);
        }
    }

    protected virtual void RegenerateMana()
    {
        if (currentMana < maxMana)
        {
            currentMana = Mathf.Min(currentMana + manaRegenRate * Time.deltaTime, maxMana);
        }
    }
}