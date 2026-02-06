using UnityEngine;

/// <summary>
/// Base class for melee weapons (Sword, Axe, etc.)
/// Handles collision-based damage
/// </summary>
public abstract class MeleeWeapon : WeaponBase
{
    [Header("Melee Settings")]
    [SerializeField] protected float attackRange = 2f;
    [SerializeField] protected float attackAngle = 60f;
    [SerializeField] protected LayerMask targetLayers;
    [SerializeField] protected Transform attackPoint;

    [Header("Visual Effects")]
    [SerializeField] protected ParticleSystem slashEffect;
    [SerializeField] protected TrailRenderer weaponTrail;

    protected override void PerformPrimaryAttack()
    {
        // Play attack animation trigger
        PlayAttackAnimation();

        // Enable weapon trail
        if (weaponTrail != null)
        {
            weaponTrail.enabled = true;
            weaponTrail.Clear();
        }

        // Detect enemies in range
        DetectAndDamageTargets();

        // Play slash effect
        if (slashEffect != null)
        {
            slashEffect.Play();
        }

        // Play attack sound
        PlayAttackSound();

        // Disable trail after a delay
        if (weaponTrail != null)
        {
            Invoke(nameof(DisableTrail), 0.3f);
        }
    }

    protected virtual void DetectAndDamageTargets()
    {
        if (attackPoint == null)
        {
            Debug.LogWarning($"{weaponName}: Attack point not assigned!");
            return;
        }

        // Find all colliders in attack range
        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, targetLayers);

        foreach (Collider hit in hits)
        {
            // Skip if hitting self
            if (owner != null && hit.transform.root == owner.transform)
            {
                continue;
            }

            // Check if target is within attack angle
            Vector3 directionToTarget = (hit.transform.position - attackPoint.position).normalized;
            float angleToTarget = Vector3.Angle(attackPoint.forward, directionToTarget);

            if (angleToTarget <= attackAngle / 2f)
            {
                // Apply damage
                BaseCharacter targetCharacter = hit.GetComponent<BaseCharacter>();
                if (targetCharacter != null)
                {
                    targetCharacter.TakeDamage(damage);
                    OnTargetHit(targetCharacter);
                }
            }
        }
    }

    protected virtual void PlayAttackAnimation()
    {
        // Override in derived classes to trigger specific animations
    }

    protected virtual void PlayAttackSound()
    {
        if (attackSound != null)
        {
            AudioSource.PlayClipAtPoint(attackSound, transform.position);
        }
    }

    protected virtual void OnTargetHit(BaseCharacter target)
    {
        // Play impact effects
        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, target.transform.position);
        }
    }

    private void DisableTrail()
    {
        if (weaponTrail != null)
        {
            weaponTrail.enabled = false;
        }
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);

        // Draw attack angle
        Vector3 forward = attackPoint.forward * attackRange;
        Vector3 leftBound = Quaternion.Euler(0, -attackAngle / 2f, 0) * forward;
        Vector3 rightBound = Quaternion.Euler(0, attackAngle / 2f, 0) * forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(attackPoint.position, attackPoint.position + leftBound);
        Gizmos.DrawLine(attackPoint.position, attackPoint.position + rightBound);
    }
}