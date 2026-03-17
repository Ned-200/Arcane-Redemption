using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Base class for ranged weapons (Staff, Bow, etc.)
/// Handles projectile-based attacks
/// </summary>
public abstract class RangedWeapon : WeaponBase
{
    [Header("Ranged Settings")]
    [SerializeField] protected GameObject projectilePrefab;
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected float projectileSpeed = 20f;

    [Header("Aiming")]
    [SerializeField] protected bool hasAimMode = true;
    [SerializeField] protected float aimFOV = 40f;
    [SerializeField] protected float normalFOV = 60f;
    [SerializeField] protected float aimSpeed = 5f;

    [Header("Combo Animation")]
    protected private float timeOfLastAttack;
    [SerializeField] protected private float comboWindow = 2.0f;
    protected private int comboStack;
    [SerializeField] protected private int maxComboStack = 1; // starts at 0

    protected bool isAiming = false;
    protected CinemachineCamera playerCamera;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        playerCamera = GameObject.FindWithTag("MainCamera").GetComponent<CinemachineCamera>();
    }

    protected override void PerformPrimaryAttack()
    {
        // Play attack animation
        PlayAttackAnimation();

        if (projectilePrefab == null)
        {
            Debug.LogError($"{weaponName}: Projectile prefab not assigned!");
            return;
        }

        if (firePoint == null)
        {
            Debug.LogError($"{weaponName}: Fire point not assigned!");
            return;
        }

        // Spawn projectile
        GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, playerCamera.transform.rotation);
        ProjectileBase projectile = projectileObj.GetComponent<ProjectileBase>();

        if (projectile != null)
        {
            projectile.Initialize(damage, owner, projectileSpeed);
        }
        else
        {
            // Fallback: add rigidbody velocity
            Rigidbody rb = projectileObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = firePoint.forward * projectileSpeed;
            }
        }

        // Play attack sound
        PlayAttackSound();

        OnProjectileFired(projectile);
    }

    public override bool TrySecondaryAttack()
    {
        if (!hasAimMode) return false;

        ToggleAim();
        return true;
    }

    protected virtual void ToggleAim()
    {
        isAiming = !isAiming;
        OnAimStateChanged(isAiming);
    }

    protected virtual void Update()
    {
        if (hasAimMode && playerCamera != null)
        {
            // Smoothly transition FOV when aiming
            float targetFOV = isAiming ? aimFOV : normalFOV;
            playerCamera.Lens.FieldOfView = Mathf.Lerp(playerCamera.Lens.FieldOfView, targetFOV, Time.deltaTime * aimSpeed);
        }
    }

    protected virtual void PlayAttackSound()
    {
        if (attackSound != null)
        {
            AudioSource.PlayClipAtPoint(attackSound, transform.position);
        }
    }

    protected virtual void OnProjectileFired(ProjectileBase projectile)
    {
        // Override for custom behavior
    }

    protected virtual void OnAimStateChanged(bool aiming)
    {
        // Override for custom behavior (UI crosshair, aim animations, etc.)
    }

    public override void OnUnequipped()
    {
        base.OnUnequipped();
        
        // Exit aim mode when unequipped
        if (isAiming)
        {
            isAiming = false;
            if (playerCamera != null)
            {
                playerCamera.Lens.FieldOfView = normalFOV;
            }
        }
    }

    protected virtual void PlayAttackAnimation()
    {
        if (timeOfLastAttack + comboWindow > Time.time)
        {
            if (comboStack < maxComboStack)
            {
                comboStack++; // play next combo animation if not at final animation
            } else
            {
                comboStack = 0; // play first combo animation if just played final one
            }
        } else
        {
            comboStack = 0; // play first combo animation if been too long since last attack
        }

        timeOfLastAttack = Time.time;

        // Override in derived classes to trigger specific animations
    }

}