using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages weapon switching and attacks for the player
/// Handles input and weapon inventory
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("Weapon Inventory")]
    [SerializeField] private List<GameObject> weaponPrefabs = new List<GameObject>();
    [SerializeField] private int startingWeaponIndex = 0;

    [Header("References")]
    [SerializeField] private Transform weaponSlot;
    [SerializeField] private BaseCharacter character;

    [Header("Input")]
    [SerializeField] private KeyCode switchWeaponKey = KeyCode.Q;

    private List<WeaponBase> instantiatedWeapons = new List<WeaponBase>();
    private int currentWeaponIndex = -1;
    private WeaponBase currentWeapon;

    public WeaponBase CurrentWeapon => currentWeapon;

    private void Awake()
    {
        if (character == null)
        {
            character = GetComponent<BaseCharacter>();
        }

        InitializeWeapons();
    }

    private void Start()
    {
        if (weaponPrefabs.Count > 0)
        {
            EquipWeapon(startingWeaponIndex);
        }
    }

    private void Update()
    {
        HandleWeaponSwitching();
        HandleWeaponInput();
    }

    private void InitializeWeapons()
    {
        if (weaponSlot == null)
        {
            Debug.LogError("WeaponManager: Weapon slot transform not assigned!");
            return;
        }

        // Instantiate all weapons and keep them inactive
        foreach (GameObject weaponPrefab in weaponPrefabs)
        {
            if (weaponPrefab == null) continue;

            GameObject weaponObj = Instantiate(weaponPrefab, weaponSlot);
            weaponObj.transform.localPosition = Vector3.zero;
            weaponObj.transform.localRotation = Quaternion.identity;
            weaponObj.SetActive(false);

            WeaponBase weapon = weaponObj.GetComponent<WeaponBase>();
            if (weapon != null)
            {
                weapon.Initialize(character);
                instantiatedWeapons.Add(weapon);
            }
            else
            {
                Debug.LogWarning($"WeaponManager: {weaponPrefab.name} doesn't have a WeaponBase component!");
                Destroy(weaponObj);
            }
        }

        Debug.Log($"WeaponManager: Initialized {instantiatedWeapons.Count} weapons");
    }

    private void HandleWeaponSwitching()
    {
        if (Input.GetKeyDown(switchWeaponKey))
        {
            SwitchToNextWeapon();
        }
    }

    private void HandleWeaponInput()
    {
        if (currentWeapon == null) return;

        // Primary attack (Left Mouse Button)
        if (Input.GetMouseButtonDown(0))
        {
            currentWeapon.TryPrimaryAttack();
        }

        // Secondary attack (Right Mouse Button)
        if (Input.GetMouseButtonDown(1))
        {
            currentWeapon.TrySecondaryAttack();
        }

        // Release secondary (for aim toggle)
        if (Input.GetMouseButtonUp(1))
        {
            // Some weapons might need to know when button is released
            RangedWeapon rangedWeapon = currentWeapon as RangedWeapon;
            if (rangedWeapon != null)
            {
                rangedWeapon.TrySecondaryAttack(); // Toggle off
            }
        }
    }

    public void SwitchToNextWeapon()
    {
        if (instantiatedWeapons.Count == 0) return;

        int nextIndex = (currentWeaponIndex + 1) % instantiatedWeapons.Count;
        EquipWeapon(nextIndex);
    }

    public void EquipWeapon(int index)
    {
        if (index < 0 || index >= instantiatedWeapons.Count)
        {
            Debug.LogWarning($"WeaponManager: Invalid weapon index {index}");
            return;
        }

        // Unequip current weapon
        if (currentWeapon != null)
        {
            currentWeapon.OnUnequipped();
        }

        // Equip new weapon
        currentWeaponIndex = index;
        currentWeapon = instantiatedWeapons[index];
        currentWeapon.OnEquipped();

        Debug.Log($"WeaponManager: Equipped {currentWeapon.WeaponName}");
    }

    public void EquipWeaponByName(string weaponName)
    {
        for (int i = 0; i < instantiatedWeapons.Count; i++)
        {
            if (instantiatedWeapons[i].WeaponName == weaponName)
            {
                EquipWeapon(i);
                return;
            }
        }

        Debug.LogWarning($"WeaponManager: Weapon '{weaponName}' not found!");
    }
}