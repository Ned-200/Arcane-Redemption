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
        // Debug.Log($"Projectile expired from lifetime!");
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
        BaseCharacter targetCharacter = other.GetComponent<BaseCharacter>(); // If a character
        if (targetCharacter != null)
        {
            targetCharacter.TakeDamage(damage);
            OnTargetHit(targetCharacter);
        } else if (other.gameObject.tag == "PlantWall") // If a plant wall - ADD TEST WHETHER PROJECTILE IS FIRE MAGIC
        {
            Disintegrate disintegrate = other.gameObject.GetComponent<Disintegrate>();
            if (disintegrate == null)
            {
                Debug.LogError(other.gameObject.name + ": PlantWall - No attached disintegration script!");
            } else
            {
                //Trigger disintegration material
                disintegrate.TriggerDisintegration();
            }
        } else if (other.gameObject.tag == "FlameWall") // If a fire wall - ADD TEST WHETHER PROJECTILE IS WATER MAGIC
        {
            GameObject FlameWall = other.gameObject;

            //Disable particles
            ParticleSystem fireParticles = FlameWall.transform.Find("FireEffect").GetComponent<ParticleSystem>();
            if (fireParticles == null)
            {
                Debug.LogError(FlameWall.name + ": FlameWall - no particles found! Check particle gameobject name");
            } else
            {
                //Disable flame particles before deleting
                fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting); // stops emmision without clearing
            }

            Collider flameCollider = FlameWall.GetComponent<Collider>();
            Light flameLight = FlameWall.GetComponent<Light>();
            if (flameCollider == null || flameLight == null)
            {
                Debug.LogError(FlameWall.name + ": FlameWall - no collider or light source found!");
            } else
            {
                //Disable flame wall collision
                flameCollider.enabled = false;
                flameLight.enabled = false;
            }
        }

        // Spawn impact effect
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, transform.position, impactEffectPrefab.transform.rotation);
        }

        // Destroy projectile
        if (destroyOnImpact)
        {
            Destroy(gameObject);
            // Debug.Log($"Projectile destroyed on impact!");
        }
    }

    protected virtual void OnTargetHit(BaseCharacter target)
    {
        Debug.Log($"Projectile hit {target.gameObject.name} for {damage} damage!");
    }
    
}