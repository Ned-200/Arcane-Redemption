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

    [Header("Hand Slots")]
    [SerializeField] protected Transform rightHandSlotTransform;
    [SerializeField] protected Transform leftHandSlotTransform;

    // Hand slot instances
    protected HandSlot rightHand;
    protected HandSlot leftHand;

    // Public accessors
    public HandSlot RightHand => rightHand;
    public HandSlot LeftHand => leftHand;
    public GameObject RightHandItem => rightHand?.EquippedItem;
    public GameObject LeftHandItem => leftHand?.EquippedItem;
    public bool IsRightHandEmpty => rightHand?.IsEmpty ?? true;
    public bool IsLeftHandEmpty => leftHand?.IsEmpty ?? true;

    protected virtual void Awake()
    {
        // Initialize stats
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        currentMana = maxMana;

        // Initialize hand slots
        InitializeHandSlots();
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

    #region Hand Slot Initialization

    /// <summary>
    /// Initializes the hand slot instances
    /// </summary>
    protected virtual void InitializeHandSlots()
    {
        rightHand = new HandSlot(rightHandSlotTransform, "Right Hand");
        leftHand = new HandSlot(leftHandSlotTransform, "Left Hand");

        if (rightHandSlotTransform == null)
        {
            Debug.LogWarning($"{gameObject.name}: Right hand slot transform is not assigned!");
        }

        if (leftHandSlotTransform == null)
        {
            Debug.LogWarning($"{gameObject.name}: Left hand slot transform is not assigned!");
        }
    }

    #endregion

    #region Hand Item Management

    /// <summary>
    /// Equips an item to the right hand
    /// </summary>
    public virtual bool EquipRightHand(GameObject itemPrefab)
    {
        if (rightHand == null)
        {
            Debug.LogError($"{gameObject.name}: Right hand slot not initialized!");
            return false;
        }

        bool success = rightHand.Equip(itemPrefab);
        if (success)
        {
            OnItemEquipped(itemPrefab, true);
        }
        return success;
    }

    /// <summary>
    /// Equips an item to the left hand
    /// </summary>
    public virtual bool EquipLeftHand(GameObject itemPrefab)
    {
        if (leftHand == null)
        {
            Debug.LogError($"{gameObject.name}: Left hand slot not initialized!");
            return false;
        }

        bool success = leftHand.Equip(itemPrefab);
        if (success)
        {
            OnItemEquipped(itemPrefab, false);
        }
        return success;
    }

    /// <summary>
    /// Unequips the right hand item
    /// </summary>
    public virtual void UnequipRightHand()
    {
        if (rightHand != null)
        {
            GameObject item = rightHand.EquippedItem;
            rightHand.Unequip();
            if (item != null)
            {
                OnItemUnequipped(item, true);
            }
        }
    }

    /// <summary>
    /// Unequips the left hand item
    /// </summary>
    public virtual void UnequipLeftHand()
    {
        if (leftHand != null)
        {
            GameObject item = leftHand.EquippedItem;
            leftHand.Unequip();
            if (item != null)
            {
                OnItemUnequipped(item, false);
            }
        }
    }

    /// <summary>
    /// Swaps items between the right and left hands
    /// </summary>
    public virtual void SwapHands()
    {
        if (rightHand == null || leftHand == null)
        {
            Debug.LogError($"{gameObject.name}: Hand slots not initialized!");
            return;
        }

        // Remove items from slots without destroying
        GameObject rightItem = rightHand.RemoveItem();
        GameObject leftItem = leftHand.RemoveItem();

        // Swap items
        rightHand.SetItem(leftItem);
        leftHand.SetItem(rightItem);

        OnHandsSwapped();
    }

    /// <summary>
    /// Unequips all items from both hands
    /// </summary>
    public virtual void UnequipAll()
    {
        UnequipRightHand();
        UnequipLeftHand();
    }

    /// <summary>
    /// Checks if the character has any items equipped
    /// </summary>
    public bool HasAnyItemEquipped()
    {
        return !IsRightHandEmpty || !IsLeftHandEmpty;
    }

    #endregion

    #region Virtual Event Methods

    /// <summary>
    /// Called when an item is equipped. Override in derived classes for custom behavior.
    /// </summary>
    /// <param name="item">The equipped item</param>
    /// <param name="isRightHand">True if right hand, false if left hand</param>
    protected virtual void OnItemEquipped(GameObject item, bool isRightHand)
    {
        // Override in derived classes for custom behavior
    }

    /// <summary>
    /// Called when an item is unequipped. Override in derived classes for custom behavior.
    /// </summary>
    /// <param name="item">The unequipped item</param>
    /// <param name="isRightHand">True if right hand, false if left hand</param>
    protected virtual void OnItemUnequipped(GameObject item, bool isRightHand)
    {
        // Override in derived classes for custom behavior
    }

    /// <summary>
    /// Called when hands are swapped. Override in derived classes for custom behavior.
    /// </summary>
    protected virtual void OnHandsSwapped()
    {
        Debug.Log($"{gameObject.name}: Swapped hands");
    }

    #endregion
}