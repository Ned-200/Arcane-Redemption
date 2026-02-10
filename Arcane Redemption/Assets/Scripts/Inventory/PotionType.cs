using UnityEngine;

/// <summary>
/// Defines the type and properties of a potion
/// </summary>
[CreateAssetMenu(fileName = "New Potion", menuName = "Inventory/Potion")]
public class PotionType : ScriptableObject
{
    [Header("Potion Info")]
    public string potionName = "Health Potion";
    public Sprite icon;
    
    [Header("Potion Type")]
    public bool isHealthPotion = true;
    public bool isManaPotion = false;
    
    [Header("Potion Effects")]
    [Tooltip("Amount of health/mana restored")]
    public float restoreAmount = 50f;
    
    [Header("Visual")]
    public Color potionColor = Color.red;
}