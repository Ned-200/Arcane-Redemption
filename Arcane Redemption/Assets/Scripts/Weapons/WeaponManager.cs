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
    private PlayerData playerData;
    private Animator playerAnim;
    public EquippedElement currentElement = EquippedElement.Fire;
    [SerializeField] private float swapDelay = 1.0f;
    private bool swapCooldown;

    [Header("References")]
    [SerializeField] private Transform weaponSlot;
    [SerializeField] private BaseCharacter character;
    private PlayerController playerController;
    [SerializeField] private GameObject weaponSwapEffectPrefab;
    [SerializeField] private GameObject fireSwapEffectPrefab;
    [SerializeField] private GameObject waterSwapEffectPrefab;
    [SerializeField] private GameObject plantSwapEffectPrefab;
        
    [Header("Staff Materials")]
    [SerializeField] private Material fireOrb;
    [SerializeField] private Material fireFirePoint;
    [SerializeField] private Material waterOrb;
    [SerializeField] private Material waterFirePoint;
    [SerializeField] private Material plantOrb;
    [SerializeField] private Material plantFirePoint;

    [Header("Sounds")]
    [SerializeField] private AudioClip FireElementSwapSound;
    [SerializeField] private AudioClip WaterElementSwapSound;
    [SerializeField] private AudioClip PlantElementSwapSound;



    [Header("Input")]
    [SerializeField] private KeyCode switchWeaponKey = KeyCode.Q;

    private List<WeaponBase> instantiatedWeapons = new List<WeaponBase>();

    private int currentWeaponIndex;
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
        GameObject playerDataObject = GameObject.FindWithTag("PlayerData");
        playerData = playerDataObject.GetComponent<PlayerData>();

        playerAnim = GetComponent<Animator>();
        if (playerAnim == null)
        {
            Debug.LogError("WeaponManager: Could not find player Animator component!");
        }
        playerController = GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("WeaponManager: Could not find playerController component!");
        }

        if (weaponPrefabs.Count > 0)
        {
            EquipWeapon(startingWeaponIndex);
        }
    }

    private void Update()
    {
        HandleWeaponSwitching();
        HandleWeaponInput();
        HandleElementInput();
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
        if (Input.GetKeyDown(switchWeaponKey) && playerData.fireGemObtained && !swapCooldown)
        {
            swapCooldown = true;
            Invoke(nameof(SetCooldown), swapDelay);
            SwitchToNextWeapon();
        }
    }

    private void HandleWeaponInput()
    {
        if (currentWeapon == null) return;

        // Primary attack (Left Mouse Button)
        if (Input.GetMouseButtonDown(0) && playerController.canMove)
        {
            currentWeapon.TryPrimaryAttack();
        }

        // Secondary attack (Right Mouse Button)
        if (Input.GetMouseButtonDown(1) & playerController.canMove)
        {
            currentWeapon.TrySecondaryAttack();
        }

        // Release secondary (for aim toggle)
        if (Input.GetMouseButtonUp(1) & playerController.canMove)
        {
            // Some weapons might need to know when button is released
            RangedWeapon rangedWeapon = currentWeapon as RangedWeapon;
            if (rangedWeapon != null)
            {
                rangedWeapon.TrySecondaryAttack(); // Toggle off
            }
        }
    }

    private void HandleElementInput()
    {
        if (currentWeapon == null) return;

        if (Input.GetKeyDown(KeyCode.R) && playerController.canMove && currentWeaponIndex == 0 && !swapCooldown)
        {
            swapCooldown = true;
            Invoke(nameof(SetCooldown), swapDelay);

            if (currentElement == EquippedElement.Fire)
            {
                if (playerData.waterGemObtained) {
                    SetElement(EquippedElement.Water);
                } else if (playerData.plantGemObtained) {
                    SetElement(EquippedElement.Plant);
                }
            } else if (currentElement == EquippedElement.Water)
            {
                if (playerData.plantGemObtained) {
                    SetElement(EquippedElement.Plant);
                } else {
                    SetElement(EquippedElement.Fire);
                }
            } else if (currentElement == EquippedElement.Plant)
            {
                SetElement(EquippedElement.Fire);
            } 
        }
    }

    public void SwitchToNextWeapon()
    {
        if (instantiatedWeapons.Count == 0) return;

        int nextIndex = (currentWeaponIndex + 1) % instantiatedWeapons.Count;
        EquipWeapon(nextIndex); 
        if (playerAnim != null) {
            playerAnim.Play("WeaponSwap");
        }
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

        // Play Equip Effect
        GameObject swapEffect = Instantiate(weaponSwapEffectPrefab, weaponSlot.position, weaponSwapEffectPrefab.transform.rotation);
        swapEffect.transform.SetParent(weaponSlot);

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

    // ELEMENT STUFF:

    public void SetElement(EquippedElement newElement)
    {
        if (currentElement != newElement)
        {
            currentElement = newElement;
            OnElementChanged(newElement);
        }
    }
    protected virtual void OnElementChanged(EquippedElement newElement)
    {
        Debug.Log("Player swapped to " + newElement);
        if (playerAnim != null) {
            playerAnim.Play("ElementSwap");
        }
        
        if (newElement == EquippedElement.Fire) 
        {
            if (currentWeapon.transform.Find("Handle").Find("Sphere") != null) {
                GameObject Orb = currentWeapon.transform.Find("Handle").Find("Sphere").gameObject;
                Renderer rend = Orb.GetComponent<Renderer>();
                rend.material = fireOrb;
                rend = Orb.transform.Find("FirePoint").GetComponent<Renderer>();
                rend.material = fireFirePoint;
                GameObject swapEffect = Instantiate(fireSwapEffectPrefab, Orb.transform.position, fireSwapEffectPrefab.transform.rotation);
                swapEffect.transform.SetParent(Orb.transform);

                if (FireElementSwapSound != null)
                {
                    AudioSource.PlayClipAtPoint(FireElementSwapSound, Orb.transform.position);
                }

            } else
            {
                Debug.LogError("WeaponManager: Could not find Staff renderer part, checking gameobject naming.");
            }
        } 
        else if (newElement == EquippedElement.Water)
        {
            if (currentWeapon.transform.Find("Handle").Find("Sphere") != null) {
                GameObject Orb = currentWeapon.transform.Find("Handle").Find("Sphere").gameObject;
                Renderer rend = Orb.GetComponent<Renderer>();
                rend.material = waterOrb;
                rend = Orb.transform.Find("FirePoint").GetComponent<Renderer>();
                rend.material = waterFirePoint;
                GameObject swapEffect = Instantiate(waterSwapEffectPrefab, Orb.transform.position, waterSwapEffectPrefab.transform.rotation);
                swapEffect.transform.SetParent(Orb.transform);

                if (WaterElementSwapSound != null)
                {
                    AudioSource.PlayClipAtPoint(WaterElementSwapSound, Orb.transform.position);
                }

            } else
            {
                Debug.LogError("WeaponManager: Could not find Staff renderer part, checking gameobject naming.");
            }
        } 
        else if (newElement == EquippedElement.Plant)
        {
            if (currentWeapon.transform.Find("Handle").Find("Sphere") != null) {
                GameObject Orb = currentWeapon.transform.Find("Handle").Find("Sphere").gameObject;
                Renderer rend = Orb.GetComponent<Renderer>();
                rend.material = plantOrb;
                rend = Orb.transform.Find("FirePoint").GetComponent<Renderer>();
                rend.material = plantFirePoint;
                GameObject swapEffect = Instantiate(plantSwapEffectPrefab, Orb.transform.position, plantSwapEffectPrefab.transform.rotation);
                swapEffect.transform.SetParent(Orb.transform);

                if (PlantElementSwapSound != null)
                {
                    AudioSource.PlayClipAtPoint(PlantElementSwapSound, Orb.transform.position);
                }

            } else
            {
                Debug.LogError("WeaponManager: Could not find Staff renderer part, checking gameobject naming.");
            }
        }
    }

    private void SetCooldown()
    {
        swapCooldown = false;
    }

    public enum EquippedElement
    {
        Fire,
        Water,
        Plant
    }
}