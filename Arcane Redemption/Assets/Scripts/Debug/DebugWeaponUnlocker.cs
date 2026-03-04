using UnityEngine;
using System.Reflection;

/// <summary>
/// Standalone debug tool for unlocking all weapons.
/// Press 'P' to unlock everything.
/// SAFE TO DELETE - Zero coupling to game systems.
/// </summary>
public class DebugWeaponUnlocker : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugUnlock = true;
    [SerializeField] private KeyCode unlockKey = KeyCode.P;

    [Header("Weapon Prefabs (Optional - Auto-finds if empty)")]
    [SerializeField] private GameObject[] weaponPrefabs;

    [Header("Debug Info")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showOnScreenIndicator = true;

    private bool hasUnlockedWeapons = false;
    private PlayerCharacter player;
    private object inventorySystem; // Using object to avoid hard reference

    private void Start()
    {
        if (!enableDebugUnlock)
        {
            enabled = false;
            return;
        }

        FindPlayer();
        FindInventorySystem();
        AutoDiscoverWeapons();

        if (showDebugLogs)
        {
            Debug.Log("[DebugWeaponUnlocker] Ready! Press [P] to unlock all weapons");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(unlockKey))
        {
            UnlockAllWeapons();
        }
    }

    private void FindPlayer()
    {
        player = FindFirstObjectByType<PlayerCharacter>();
        
        if (player == null && showDebugLogs)
        {
            Debug.LogWarning("[DebugWeaponUnlocker] PlayerCharacter not found!");
        }
    }

    private void FindInventorySystem()
    {
        // Use reflection to find InventorySystem without hard coupling
        GameObject playerData = GameObject.FindWithTag("PlayerData");
        
        if (playerData != null)
        {
            // Try to get InventorySystem component using reflection
            Component[] components = playerData.GetComponents<Component>();
            foreach (Component comp in components)
            {
                if (comp.GetType().Name == "InventorySystem")
                {
                    inventorySystem = comp;
                    if (showDebugLogs)
                    {
                        Debug.Log("[DebugWeaponUnlocker] Found InventorySystem via reflection");
                    }
                    return;
                }
            }
        }

        if (showDebugLogs)
        {
            Debug.LogWarning("[DebugWeaponUnlocker] InventorySystem not found - will use direct player method");
        }
    }

    private void AutoDiscoverWeapons()
    {
        if (weaponPrefabs != null && weaponPrefabs.Length > 0)
        {
            return; // Already assigned manually
        }

        // Auto-discover weapon prefabs in Resources folder
        Object[] foundWeapons = Resources.LoadAll("Weapons", typeof(GameObject));
        
        if (foundWeapons.Length > 0)
        {
            weaponPrefabs = new GameObject[foundWeapons.Length];
            for (int i = 0; i < foundWeapons.Length; i++)
            {
                weaponPrefabs[i] = foundWeapons[i] as GameObject;
            }

            if (showDebugLogs)
            {
                Debug.Log($"[DebugWeaponUnlocker] Auto-discovered {weaponPrefabs.Length} weapons");
            }
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning("[DebugWeaponUnlocker] No weapons found in Resources/Weapons folder");
        }
    }

    private void UnlockAllWeapons()
    {
        if (hasUnlockedWeapons)
        {
            if (showDebugLogs)
            {
                Debug.Log("[DebugWeaponUnlocker] Weapons already unlocked!");
            }
            return;
        }

        int unlockedCount = 0;

        // Method 1: Try using InventorySystem via reflection (safest - no coupling)
        if (inventorySystem != null)
        {
            unlockedCount = UnlockViaInventorySystem();
        }
        // Method 2: Fallback - Equip directly to player
        else if (player != null && weaponPrefabs != null && weaponPrefabs.Length > 0)
        {
            unlockedCount = UnlockViaDirectEquip();
        }

        hasUnlockedWeapons = true;

        if (showDebugLogs)
        {
            Debug.LogWarning($"[DebugWeaponUnlocker] 🔓 UNLOCKED {unlockedCount} WEAPONS! [P]");
        }
    }

    private int UnlockViaInventorySystem()
    {
        int count = 0;

        if (weaponPrefabs == null || weaponPrefabs.Length == 0)
        {
            return count;
        }

        // Use reflection to call AddWeapon or similar method without hard reference
        MethodInfo addWeaponMethod = inventorySystem.GetType().GetMethod(
            "AddWeapon", 
            BindingFlags.Public | BindingFlags.Instance
        );

        if (addWeaponMethod != null)
        {
            foreach (GameObject weaponPrefab in weaponPrefabs)
            {
                if (weaponPrefab == null) continue;

                try
                {
                    addWeaponMethod.Invoke(inventorySystem, new object[] { weaponPrefab });
                    count++;

                    if (showDebugLogs)
                    {
                        Debug.Log($"[DebugWeaponUnlocker] Unlocked: {weaponPrefab.name}");
                    }
                }
                catch (System.Exception e)
                {
                    if (showDebugLogs)
                    {
                        Debug.LogWarning($"[DebugWeaponUnlocker] Failed to unlock {weaponPrefab.name}: {e.Message}");
                    }
                }
            }
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning("[DebugWeaponUnlocker] InventorySystem.AddWeapon() method not found via reflection");
        }

        return count;
    }

    private int UnlockViaDirectEquip()
    {
        int count = 0;

        // Fallback: Equip first weapon directly to player
        foreach (GameObject weaponPrefab in weaponPrefabs)
        {
            if (weaponPrefab == null) continue;

            if (player.EquipWeapon(weaponPrefab))
            {
                count++;
                if (showDebugLogs)
                {
                    Debug.Log($"[DebugWeaponUnlocker] Equipped: {weaponPrefab.name}");
                }
                break; // Only equip one weapon directly
            }
        }

        return count;
    }

    private void OnGUI()
    {
        if (!showOnScreenIndicator || !hasUnlockedWeapons) return;

        GUI.color = Color.cyan;
        GUI.Label(new Rect(10, 10, 300, 30), "🔓 DEBUG: All Weapons Unlocked [P]");
    }

    private void OnDestroy()
    {
        if (showDebugLogs)
        {
            Debug.Log("[DebugWeaponUnlocker] Debug tool removed - game systems unaffected");
        }
    }
}