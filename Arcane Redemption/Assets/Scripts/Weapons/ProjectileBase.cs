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
    [SerializeField] protected string element;
    [SerializeField] protected bool toughVinesBurnable = false; // CHANGE THIS TO TRUE FOR TESTING ONLY
    

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

    public BaseCharacter GetOwner()
    {
        return owner;
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
        } else if (other.gameObject.tag == "PlantWall" && element == "Fire") // If a plant wall
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
        }
        
        else if (other.gameObject.tag == "FlameWall" && element == "Water") // If a fire wall
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