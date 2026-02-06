using UnityEngine;

/// <summary>
/// Base class for all projectiles
/// Handles movement, collision, and damage dealing
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ProjectileBase : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] protected float lifetime = 5f;
    [SerializeField] protected bool destroyOnImpact = true;
    [SerializeField] protected LayerMask targetLayers;

    [Header("Visual Effects")]
    [SerializeField] protected GameObject impactEffectPrefab;
    [SerializeField] protected TrailRenderer trail;

    protected float damage;
    protected BaseCharacter owner;
    protected Rigidbody rb;
    protected bool hasHit = false;

    public virtual void Initialize(float projectileDamage, BaseCharacter projectileOwner, float speed)
    {
        damage = projectileDamage;
        owner = projectileOwner;

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
        }

        // Destroy after lifetime
        Destroy(gameObject, lifetime);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Skip if hitting owner
        if (owner != null && other.transform.root == owner.transform)
        {
            return;
        }

        // Check if target is on valid layer
        if (((1 << other.gameObject.layer) & targetLayers) == 0)
        {
            return;
        }

        hasHit = true;

        // Apply damage
        BaseCharacter targetCharacter = other.GetComponent<BaseCharacter>();
        if (targetCharacter != null)
        {
            targetCharacter.TakeDamage(damage);
            OnTargetHit(targetCharacter);
        }

        // Spawn impact effect
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
        }

        // Destroy projectile
        if (destroyOnImpact)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnTargetHit(BaseCharacter target)
    {
        Debug.Log($"Projectile hit {target.gameObject.name} for {damage} damage!");
    }
}