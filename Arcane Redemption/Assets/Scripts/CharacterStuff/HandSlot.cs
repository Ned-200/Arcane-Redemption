using UnityEngine;

/// <summary>
/// Represents a single hand slot that can hold an item
/// </summary>
public class HandSlot
{
    private readonly Transform slotTransform;
    private readonly string slotName;
    private GameObject equippedItem;

    public GameObject EquippedItem => equippedItem;
    public bool IsEmpty => equippedItem == null;
    public Transform SlotTransform => slotTransform;

    public HandSlot(Transform slotTransform, string slotName)
    {
        this.slotTransform = slotTransform;
        this.slotName = slotName;
    }

    /// <summary>
    /// Equips an item to this hand slot
    /// </summary>
    public bool Equip(GameObject itemPrefab)
    {
        if (itemPrefab == null)
        {
            Debug.LogWarning($"Cannot equip null item to {slotName}.");
            return false;
        }

        if (slotTransform == null)
        {
            Debug.LogError($"{slotName} slot transform is not assigned!");
            return false;
        }

        // Unequip current item if any
        Unequip();

        // Instantiate and parent to slot
        equippedItem = Object.Instantiate(itemPrefab, slotTransform);
        equippedItem.transform.localPosition = Vector3.zero;
        equippedItem.transform.localRotation = Quaternion.identity;
        equippedItem.name = itemPrefab.name; // Remove "(Clone)" suffix

        Debug.Log($"Equipped {itemPrefab.name} to {slotName}");
        return true;
    }

    /// <summary>
    /// Unequips the current item from this slot
    /// </summary>
    public void Unequip()
    {
        if (equippedItem != null)
        {
            Debug.Log($"Unequipped {equippedItem.name} from {slotName}");
            Object.Destroy(equippedItem);
            equippedItem = null;
        }
    }

    /// <summary>
    /// Sets the equipped item directly (used for swapping)
    /// </summary>
    public void SetItem(GameObject item)
    {
        equippedItem = item;

        if (item != null && slotTransform != null)
        {
            item.transform.SetParent(slotTransform);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Removes the item reference without destroying it (used for swapping)
    /// </summary>
    public GameObject RemoveItem()
    {
        GameObject item = equippedItem;
        equippedItem = null;
        return item;
    }
}