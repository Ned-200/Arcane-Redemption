using UnityEngine;

public class FallingRock : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private bool requiresPlayerTrigger = true;
    [SerializeField] private float triggerRadius = 2f;

    [Header("Physics Settings")]
    [SerializeField] private float fallSpeed = 10f;
    [SerializeField] private float lifeTime = 10f;

    [Header("Damage Settings")]
    [SerializeField] private float playerDamage = 20f;
    [SerializeField] private float impactRadius = 1f;

    [Header("Visual Effects")]
    [SerializeField] private GameObject impactVFX;
    [SerializeField] private GameObject idleVFX;
    [SerializeField] private Material idleMaterial;
    [SerializeField] private Material fallingMaterial;

    [Header("Audio")]
    [SerializeField] private AudioClip triggerSound;
    [SerializeField] private AudioClip impactSound;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    private Rigidbody rb;
    private bool hasBeenTriggered;
    private bool hasImpacted;
    private float spawnTime;
    private SphereCollider triggerCollider;
    private Renderer rockRenderer;

    public bool HasBeenTriggered => hasBeenTriggered;
    public bool IsAvailable => !hasBeenTriggered && !hasImpacted;
    public Vector3 GroundPosition => new Vector3(transform.position.x, 0f, transform.position.z);

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rockRenderer = GetComponent<Renderer>();

        if (requiresPlayerTrigger)
        {
            ConfigureAsStationaryRock();
        }
        else
        {
            ConfigureAsFallingRock();
        }

        spawnTime = Time.time;
    }

    private void ConfigureAsStationaryRock()
    {
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        triggerCollider = gameObject.AddComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = triggerRadius;

        if (rockRenderer != null && idleMaterial != null)
        {
            rockRenderer.material = idleMaterial;
        }

        if (idleVFX != null)
        {
            Instantiate(idleVFX, transform.position, Quaternion.identity, transform);
        }
    }

    private void ConfigureAsFallingRock()
    {
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.mass = 50f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        hasBeenTriggered = true;
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!requiresPlayerTrigger || hasBeenTriggered) return;

        if (IsPlayerProjectile(other))
        {
            TriggerRockFall();
        }
    }

    private bool IsPlayerProjectile(Collider other)
    {
        ProjectileBase projectile = other.GetComponent<ProjectileBase>();
        if (projectile == null) return false;

        BaseCharacter owner = projectile.GetOwner();
        if (owner == null) return false;

        return owner.GetComponent<PlayerCharacter>() != null;
    }

    public void TriggerRockFall()
    {
        if (hasBeenTriggered) return;

        hasBeenTriggered = true;

        if (triggerCollider != null)
        {
            Destroy(triggerCollider);
        }

        SphereCollider solidCollider = gameObject.AddComponent<SphereCollider>();
        solidCollider.isTrigger = false;
        solidCollider.radius = 1f;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        rb.mass = 50f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (rockRenderer != null && fallingMaterial != null)
        {
            rockRenderer.material = fallingMaterial;
        }

        if (triggerSound != null)
        {
            AudioSource.PlayClipAtPoint(triggerSound, transform.position);
        }

        if (idleVFX != null)
        {
            foreach (Transform child in transform)
            {
                if (child.name.Contains(idleVFX.name))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        Destroy(gameObject, lifeTime);

        Debug.Log($"[FallingRock] Triggered by player projectile - falling!");
    }

    private void FixedUpdate()
    {
        if (hasBeenTriggered && !hasImpacted && rb != null)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, -fallSpeed, rb.linearVelocity.z);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!hasBeenTriggered) return;
        if (hasImpacted) return;

        hasImpacted = true;

        HandleRockImpact(collision);

        if (impactVFX != null)
        {
            Instantiate(impactVFX, transform.position, Quaternion.identity);
        }

        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, transform.position);
        }

        Destroy(gameObject, 0.1f);
    }

    private void HandleRockImpact(Collision collision)
    {
        ShellBoss boss = collision.gameObject.GetComponent<ShellBoss>();
        if (boss != null)
        {
            boss.OnRockHit(this);
            return;
        }

        PlayerCharacter player = collision.gameObject.GetComponent<PlayerCharacter>();
        if (player != null)
        {
            DamagePlayer(player);
            return;
        }
    }

    private void DamagePlayer(PlayerCharacter player)
    {
        BaseCharacter baseChar = player.GetComponent<BaseCharacter>();
        if (baseChar != null)
        {
            baseChar.TakeDamage(playerDamage);
            Debug.Log($"[FallingRock] Hit player for {playerDamage} damage!");
        }
    }

    public Vector3 GetImpactPosition()
    {
        return transform.position;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        if (requiresPlayerTrigger && !hasBeenTriggered)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, triggerRadius);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);
        }
        else if (hasBeenTriggered)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}