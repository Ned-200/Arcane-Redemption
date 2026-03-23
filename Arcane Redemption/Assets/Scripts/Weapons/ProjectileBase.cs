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
        // Don't continue if not correct layer
        if (((1 << other.gameObject.layer) & targetLayers) == 0)
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

        // If projectile was shot by a player, check for envirnmental triggers (Vine walls, Barrels, growing plant bridges, etc)
        if (owner.gameObject.tag == "Player") {
            EnvironmentalTriggers(other);
        }

        // Spawn impact effect
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, hitEffect.transform.rotation);
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

    protected virtual void EnvironmentalTriggers(Collider other)
    {
        // CHECK FOR ALL ENVIRONMENT COLLISIONS:
        if (other.gameObject.tag == "PlantWall" && element == "Fire") // If a plant wall
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
        } else if (other.gameObject.tag =="Tough Plant Wall" && toughVinesBurnable){
            DisintegrateUP disintegrateup = other.gameObject.GetComponent<DisintegrateUP>();

            if (disintegrateup == null)
            {
                Debug.LogError(other.gameObject.name + ": PlantWall - No attached disintegration script!");
            } else
            {
                //Trigger disintegration material
                disintegrateup.TriggerDisintegrationUP();
            }
        } else if (other.gameObject.tag == "FlameWall" && element == "Water") // If a fire wall
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

            FlameWall flameWallScript = FlameWall.GetComponent<FlameWall>();
            Collider flameCollider = FlameWall.GetComponent<Collider>();
            Light flameLight = FlameWall.GetComponent<Light>();
            if (flameCollider == null || flameLight == null || flameWallScript == null) 
            {
                Debug.LogError(FlameWall.name + ": FlameWall - no collider or light source found!");
            } else
            {
                //Disable flame wall collision
                flameCollider.enabled = false;
                flameLight.enabled = false;
                flameWallScript.enabled = false;
                Destroy(FlameWall, 3);
            }
        } else if (other.gameObject.tag == "Breakable") // If a breakable object
        {
            GameObject BreakableObject = other.gameObject;

            //Play damaged effect
            Breakable breakableScript = BreakableObject.GetComponent<Breakable>();
            if (breakableScript != null)
            {
                breakableScript.Break();
            } else
            {
                Debug.LogError(BreakableObject.name + ": Breakable - no breakableScript found!");
            }
        } else if (other.gameObject.tag == "Flower") //if it's a flower for plant dungeon
        {
            // START FLOWER TOGGLE CHECK
            GameObject ToggleObject = other.gameObject;
            SwordHitToggle toggle = ToggleObject.GetComponent<SwordHitToggle>();
            if (toggle !=null)
            {
                toggle.Toggle();
            } 
        } else if (other.gameObject.tag == "PlantBridge" && element == "Plant") // if it's a plant bridge cast point
        {
            // START FLOWER TOGGLE CHECK
            PlantBridge plantBridge = other.GetComponent<PlantBridge>();
            if (plantBridge != null)
            {
                plantBridge.GrowBridge();
            } else
            {
                Debug.LogError("ProjectileBase: Could not fetch plant bridge component from tagged object!");
            }
        }
    }
}