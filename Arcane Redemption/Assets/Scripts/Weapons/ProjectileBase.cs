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
    [SerializeField] protected bool destroyOnImpact = true;
    [SerializeField] protected LayerMask targetLayers;
    [SerializeField] protected string element;
    [SerializeField] protected bool toughVinesBurnable = false; // CHANGE THIS TO TRUE FOR TESTING ONLY

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

        // Check if we hit a FallingRock - trigger it AND destroy projectile
        FallingRock rock = other.GetComponent<FallingRock>();
        if (rock != null)
        {
            Debug.Log($"[ProjectileBase] Hit FallingRock - triggering rock and destroying projectile");
            // Rock's OnTriggerEnter will handle the trigger logic
            // Mark as hit and destroy
            hasHit = true;
            
            // Spawn impact effect on rock
            SpawnImpactEffect();
            PlayImpactSound();
            
            Destroy(gameObject);
            return;
        }

        // Don't continue if not correct layer
        if (((1 << other.gameObject.layer) & targetLayers) == 0)
        {
            return;
        }

        // For everything else, handle collision normally
        hasHit = true;

        // Try to damage the target
        BaseCharacter target = other.GetComponent<BaseCharacter>();
        if (target != null)
        {
            EnemyCharacter enemy = other.GetComponent<EnemyCharacter>(); 
            if (enemy != null) { // if target character is an enemycharacter
                
                if (enemy.element != null && enemy.element != "") { // If enemy has an assigned element
                    // For each possible element, check if projectile overpowers enemy element
                    if ((enemy.element == "Fire" && element == "Water") || (enemy.element == "Water" && element == "Plant") || enemy.element == "Plant" && element == "Fire") 
                    {
                        target.TakeDamage(damage);
                        OnTargetHit(target);
                        Debug.Log($"[ProjectileBase] Hit {target.name} for {damage} damage!");
                    } else
                    {
                        Debug.Log($"[ProjectileBase] Hit {target.name}, but incorrect element matchup!");
                    }
                } else { // if not enemy element is not assigned, deal damage normally
                    target.TakeDamage(damage);
                    OnTargetHit(target);
                    Debug.Log($"[ProjectileBase] Hit {target.name} for {damage} damage!");
                }
            } else { // if not an enemy character, also deal damage normally
                target.TakeDamage(damage);
                OnTargetHit(target);
                Debug.Log($"[ProjectileBase] Hit {target.name} for {damage} damage!");
            }
        }

        // If projectile was shot by a player, check for environmental triggers
        if (IsPlayerProjectile())
        {
            HandleEnvironmentalTriggers(other);
        }

        // Spawn impact effect
        SpawnImpactEffect();

        // Play impact sound
        PlayImpactSound();

        // Destroy the projectile
        if (destroyOnImpact)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        // Handle solid collisions (when rock is falling or other solid objects)
        // Check if we hit a FallingRock that's already falling
        FallingRock rock = collision.gameObject.GetComponent<FallingRock>();
        if (rock != null)
        {
            if (hasHit) return; // Already processed
            
            Debug.Log($"[ProjectileBase] Collision with falling FallingRock - destroying projectile");
            hasHit = true;
            
            // Spawn impact effect
            SpawnImpactEffect();
            PlayImpactSound();
            
            Destroy(gameObject);
            return;
        }

        // For all other collisions, treat as trigger
        OnTriggerEnter(collision.collider);
    }

    /// <summary>
    /// Checks if this projectile belongs to a player character.
    /// </summary>
    protected bool IsPlayerProjectile()
    {
        if (owner == null)
        {
            return false;
        }

        return owner.CompareTag("Player");
    }

    /// <summary>
    /// Virtual method called when projectile hits a character target.
    /// Override this in derived classes for custom hit behavior.
    /// </summary>
    protected virtual void OnTargetHit(BaseCharacter target)
    {
        // Base implementation does nothing
        // Derived classes can override for custom behavior
    }

    /// <summary>
    /// Handles interactions with environmental objects.
    /// </summary>
    protected virtual void HandleEnvironmentalTriggers(Collider other)
    {
        // Delegate to specific handlers based on tag
        if (TryBurnPlantWall(other)) return;
        if (TryBurnToughPlantWall(other)) return;
        if (TryExtinguishFlameWall(other)) return;
        if (TryBreakObject(other)) return;
        if (TryToggleFlower(other)) return;
        if (TryGrowPlantBridge(other)) return;
    }

    #region Environmental Interaction Handlers

    protected virtual bool TryBurnPlantWall(Collider other)
    {
        if (!other.CompareTag("PlantWall") || element != "Fire")
        {
            return false;
        }

        Disintegrate disintegrate = other.GetComponent<Disintegrate>();
        if (disintegrate == null)
        {
            Debug.LogError($"{other.gameObject.name}: PlantWall missing Disintegrate component!");
            return false;
        }

        disintegrate.TriggerDisintegration();
        return true;
    }

    protected virtual bool TryBurnToughPlantWall(Collider other)
    {
        if (!other.CompareTag("Tough Plant Wall") || !toughVinesBurnable)
        {
            return false;
        }

        DisintegrateUP disintegrateup = other.GetComponent<DisintegrateUP>();
        if (disintegrateup == null)
        {
            Debug.LogError($"{other.gameObject.name}: Tough Plant Wall missing DisintegrateUP component!");
            return false;
        }

        disintegrateup.TriggerDisintegrationUP();
        return true;
    }

    protected virtual bool TryExtinguishFlameWall(Collider other)
    {
        if (!other.CompareTag("FlameWall") || element != "Water")
        {
            return false;
        }

        GameObject flameWall = other.gameObject;

        // Handle particles
        Transform fireEffectTransform = flameWall.transform.Find("FireEffect");
        if (fireEffectTransform != null)
        {
            ParticleSystem fireParticles = fireEffectTransform.GetComponent<ParticleSystem>();
            if (fireParticles != null)
            {
                fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
        else
        {
            Debug.LogWarning($"{flameWall.name}: FlameWall missing FireEffect child!");
        }

        // Disable components
        FlameWall flameWallScript = flameWall.GetComponent<FlameWall>();
        Collider flameCollider = flameWall.GetComponent<Collider>();
        Light flameLight = flameWall.GetComponent<Light>();

        if (flameCollider != null) flameCollider.enabled = false;
        if (flameLight != null) flameLight.enabled = false;
        if (flameWallScript != null) flameWallScript.enabled = false;

        Destroy(flameWall, 3f);
        return true;
    }

    protected virtual bool TryBreakObject(Collider other)
    {
        if (!other.CompareTag("Breakable"))
        {
            return false;
        }

        Breakable breakableScript = other.GetComponent<Breakable>();
        if (breakableScript == null)
        {
            Debug.LogError($"{other.gameObject.name}: Breakable missing Breakable component!");
            return false;
        }

        breakableScript.Break();
        return true;
    }

    protected virtual bool TryToggleFlower(Collider other)
    {
        if (!other.CompareTag("Flower"))
        {
            return false;
        }

        SwordHitToggle toggle = other.GetComponent<SwordHitToggle>();
        if (toggle == null)
        {
            Debug.LogWarning($"{other.gameObject.name}: Flower missing SwordHitToggle component!");
            return false;
        }

        toggle.Toggle();
        return true;
    }

    protected virtual bool TryGrowPlantBridge(Collider other)
    {
        if (!other.CompareTag("PlantBridge") || element != "Plant")
        {
            return false;
        }

        PlantBridge plantBridge = other.GetComponent<PlantBridge>();
        if (plantBridge == null)
        {
            Debug.LogError($"{other.gameObject.name}: PlantBridge missing PlantBridge component!");
            return false;
        }

        plantBridge.GrowBridge();
        return true;
    }

    #endregion

    #region Effect Helpers

    protected virtual void SpawnImpactEffect()
    {
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, hitEffect.transform.rotation);
        }
    }

    protected virtual void PlayImpactSound()
    {
        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, transform.position);
        }
    }

    #endregion
}