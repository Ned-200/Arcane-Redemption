using UnityEngine;

/// <summary>
/// Base class for all projectiles
/// Handles movement, collision, and damage dealing
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ProjectileBase : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] protected float damage = 10f;
    [SerializeField] protected float speed = 20f;
    [SerializeField] protected float lifetime = 5f;

    [Header("Visual Effects")]
    [SerializeField] protected GameObject hitEffect;
    [SerializeField] protected TrailRenderer trail;

    [Header("Audio")]
    [SerializeField] protected AudioClip impactSound;

    protected BaseCharacter owner;
    protected Rigidbody rb;
    protected bool hasHit = false;

    public BaseCharacter GetOwner() => owner;

    public virtual void Initialize(float projectileDamage, BaseCharacter projectileOwner, float projectileSpeed)
    {
        damage = projectileDamage;
        owner = projectileOwner;
        speed = projectileSpeed;

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = false;
        rb.linearVelocity = transform.forward * speed;

        Destroy(gameObject, lifetime);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // IGNORE owner (don't hit yourself)
        if (other.GetComponent<BaseCharacter>() == owner)
        {
            return;
        }

        // Check if we hit a FallingRock - trigger it but DON'T destroy projectile
        FallingRock rock = other.GetComponent<FallingRock>();
        if (rock != null)
        {
            Debug.Log($"[ProjectileBase] Hit FallingRock - triggering rock but continuing flight");
            // Rock's OnTriggerEnter will handle the trigger logic
            // Projectile passes through and continues
            return;
        }

        // For everything else, handle collision normally
        hasHit = true;

        // Try to damage the target
        BaseCharacter target = other.GetComponent<BaseCharacter>();
        if (target != null)
        {
            target.TakeDamage(damage);
            OnTargetHit(target); // NEW: Call virtual method for derived classes
            Debug.Log($"[ProjectileBase] Hit {target.name} for {damage} damage!");
        }

        // Spawn impact effect
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        // Play impact sound
        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, transform.position);
        }

        // Destroy the projectile
        Destroy(gameObject);
    }

    /// <summary>
    /// NEW: Virtual method called when projectile hits a character target.
    /// Override this in derived classes for custom hit behavior.
    /// </summary>
    /// <param name="target">The character that was hit</param>
    protected virtual void OnTargetHit(BaseCharacter target)
    {
        // Base implementation does nothing
        // Derived classes can override for custom behavior
    }
}