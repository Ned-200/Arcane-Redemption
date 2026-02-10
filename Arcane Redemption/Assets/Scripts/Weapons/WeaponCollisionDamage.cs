using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles collision-based damage for melee weapons
/// Attach to the blade/damage collider of the weapon
/// </summary>
[RequireComponent(typeof(Collider))]
public class WeaponCollisionDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damageMultiplier = 1f;
    [Tooltip("Prevents hitting the same target multiple times in one swing")]
    [SerializeField] private bool preventMultiHit = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool visualizeCollider = true;
    [SerializeField] private bool debugCollisions = true;
    
    // Reference to the weapon
    private WeaponBase weapon;
    private BaseCharacter owner;
    
    // State management
    private bool isDamageEnabled = false;
    private HashSet<BaseCharacter> hitTargetsThisSwing = new HashSet<BaseCharacter>();
    
    // Collider reference
    private Collider weaponCollider;

    private void Awake()
    {
        weaponCollider = GetComponent<Collider>();
        
        if (weaponCollider == null)
        {
            Debug.LogError("WeaponCollisionDamage: No collider found on this GameObject!");
            return;
        }
        
        // Ensure collider is trigger
        if (!weaponCollider.isTrigger)
        {
            Debug.LogWarning("WeaponCollisionDamage: Collider should be a trigger! Setting isTrigger = true");
            weaponCollider.isTrigger = true;
        }
        
        // Start with damage disabled
        DisableDamage();
        
        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] WeaponCollisionDamage Awake - Collider: {weaponCollider.GetType().Name}, IsTrigger: {weaponCollider.isTrigger}");
        }
    }

    /// <summary>
    /// Initialize with weapon reference
    /// </summary>
    public void Initialize(WeaponBase weaponBase, BaseCharacter characterOwner)
    {
        weapon = weaponBase;
        owner = characterOwner;
        
        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] WeaponCollisionDamage initialized - Weapon: {weapon?.WeaponName ?? "NULL"}, Owner: {owner?.gameObject.name ?? "NULL"}");
        }
    }

    /// <summary>
    /// Enable damage detection (call at start of attack animation)
    /// </summary>
    public void EnableDamage()
    {
        isDamageEnabled = true;
        hitTargetsThisSwing.Clear();
        
        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] ⚔️ Damage ENABLED (Weapon: {weapon?.WeaponName ?? "NULL"})");
        }
    }

    /// <summary>
    /// Disable damage detection (call at end of attack animation)
    /// </summary>
    public void DisableDamage()
    {
        isDamageEnabled = false;
        hitTargetsThisSwing.Clear();
        
        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] ❌ Damage DISABLED");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (debugCollisions)
        {
            Debug.Log($"[{gameObject.name}] OnTriggerEnter with: {other.gameObject.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)}) - Damage Enabled: {isDamageEnabled}");
        }

        // Only process if damage is enabled
        if (!isDamageEnabled)
        {
            if (debugCollisions)
            {
                Debug.Log($"[{gameObject.name}] Skipping - damage not enabled");
            }
            return;
        }

        // Check if weapon is initialized
        if (weapon == null)
        {
            Debug.LogError($"[{gameObject.name}] Weapon reference is NULL! Did you call Initialize()?");
            return;
        }

        // Don't hit ourselves - check multiple ways
        if (owner != null)
        {
            // Check root transform
            if (other.transform.root == owner.transform)
            {
                if (debugCollisions)
                {
                    Debug.Log($"[{gameObject.name}] Skipping - hit owner's root transform");
                }
                return;
            }
            
            // Check direct gameObject
            if (other.gameObject == owner.gameObject)
            {
                if (debugCollisions)
                {
                    Debug.Log($"[{gameObject.name}] Skipping - hit owner's gameObject");
                }
                return;
            }
        }

        if (debugCollisions)
        {
            Debug.Log($"[{gameObject.name}] Looking for BaseCharacter on: {other.gameObject.name}");
        }

        // Check if the collider has a BaseCharacter component
        BaseCharacter target = other.GetComponent<BaseCharacter>();
        
        if (target == null)
        {
            // Try to find in parent
            target = other.GetComponentInParent<BaseCharacter>();
            
            if (debugCollisions)
            {
                Debug.Log($"[{gameObject.name}] GetComponentInParent result: {(target != null ? target.gameObject.name : "NULL")}");
            }
        }
        else if (debugCollisions)
        {
            Debug.Log($"[{gameObject.name}] Found BaseCharacter directly: {target.gameObject.name}");
        }

        if (target == null)
        {
            if (debugCollisions)
            {
                Debug.LogWarning($"[{gameObject.name}] No BaseCharacter found on {other.gameObject.name} or its parents!");
            }
            return;
        }

        // Don't hit owner (additional check using BaseCharacter reference)
        if (owner != null && target == owner)
        {
            if (debugCollisions)
            {
                Debug.Log($"[{gameObject.name}] Skipping - target is owner");
            }
            return;
        }

        // Check if we've already hit this target this swing
        if (preventMultiHit && hitTargetsThisSwing.Contains(target))
        {
            if (debugCollisions)
            {
                Debug.Log($"[{gameObject.name}] Skipping - already hit {target.gameObject.name} this swing");
            }
            return;
        }

        // Apply damage
        ApplyDamage(target);
        
        // Track this hit
        if (preventMultiHit)
        {
            hitTargetsThisSwing.Add(target);
        }
    }

    private void ApplyDamage(BaseCharacter target)
    {
        if (weapon == null)
        {
            Debug.LogError($"[{gameObject.name}] WeaponCollisionDamage: No weapon reference!");
            return;
        }

        float finalDamage = weapon.Damage * damageMultiplier;
        
        Debug.Log($"[{gameObject.name}] ⚔️⚔️⚔️ APPLYING DAMAGE: {finalDamage} to {target.gameObject.name} (Base Damage: {weapon.Damage}, Multiplier: {damageMultiplier})");
        
        target.TakeDamage(finalDamage);

        if (showDebugLogs)
        {
            Debug.Log($"[{weapon.WeaponName}] ⚔️ HIT {target.gameObject.name} for {finalDamage} damage!");
        }

        // Notify weapon of hit (for effects, sounds, etc.)
        if (weapon is MeleeWeapon meleeWeapon)
        {
            meleeWeapon.SendMessage("OnCollisionHit", target, SendMessageOptions.DontRequireReceiver);
        }
    }

    // Visualize the collision area in Scene view
    private void OnDrawGizmos()
    {
        if (!visualizeCollider) return;

        Gizmos.color = isDamageEnabled ? Color.red : Color.gray;
        
        if (weaponCollider != null)
        {
            // Draw the collider bounds
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (weaponCollider is BoxCollider boxCollider)
            {
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            }
            else if (weaponCollider is SphereCollider sphereCollider)
            {
                Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
            }
            else if (weaponCollider is CapsuleCollider capsuleCollider)
            {
                // Simplified capsule visualization
                Gizmos.DrawWireSphere(capsuleCollider.center, capsuleCollider.radius);
            }
        }
    }

    // Additional debug method you can call from Inspector or console
    public void DebugStatus()
    {
        Debug.Log($"=== WeaponCollisionDamage Debug Status ===");
        Debug.Log($"GameObject: {gameObject.name}");
        Debug.Log($"Damage Enabled: {isDamageEnabled}");
        Debug.Log($"Weapon: {weapon?.WeaponName ?? "NULL"}");
        Debug.Log($"Owner: {owner?.gameObject.name ?? "NULL"}");
        Debug.Log($"Collider: {weaponCollider?.GetType().Name ?? "NULL"}");
        Debug.Log($"Is Trigger: {weaponCollider?.isTrigger ?? false}");
        Debug.Log($"Damage Multiplier: {damageMultiplier}");
        Debug.Log($"========================================");
    }
}