using UnityEngine;
using System.Collections;

public class FallingRock : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private bool requiresPlayerTrigger = true;
    [SerializeField] private float triggerRadius = 2f;

    [Header("Physics Settings")]
    [SerializeField] private float fallSpeed = 10f;
    [SerializeField] private float lifeTime = 10f;
    [SerializeField] private float collisionEnableDelay = 0.3f; // NEW: Delay before rock can collide

    [Header("Damage Settings")]
    [SerializeField] private float playerDamage = 20f;
    [SerializeField] private float bossDamage = 1f;
    [SerializeField] private float impactRadius = 1f;

    [Header("Respawn Settings")]
    [SerializeField] private float respawnDelay = 5f;

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
    private SphereCollider rockCollider;
    private Renderer rockRenderer;
    private bool canCollide = false; // NEW: Prevents immediate collision
    private RockSpawnPoint spawnPoint;

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

        rockCollider = GetComponent<SphereCollider>();
        if (rockCollider == null)
        {
            rockCollider = gameObject.AddComponent<SphereCollider>();
        }

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

    public void SetSpawnPoint(RockSpawnPoint point)
    {
        spawnPoint = point;
    }

    private void ConfigureAsStationaryRock()
    {
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        rockCollider.isTrigger = true;
        rockCollider.radius = triggerRadius;

        if (rockRenderer != null && idleMaterial != null)
        {
            rockRenderer.material = idleMaterial;
        }

        if (idleVFX != null)
        {
            Instantiate(idleVFX, transform.position, Quaternion.identity, transform);
        }

        Debug.Log($"[FallingRock] Configured as stationary - waiting for trigger");
    }

    private void ConfigureAsFallingRock()
    {
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.mass = 50f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        rockCollider.isTrigger = false;
        rockCollider.radius = 1f;

        hasBeenTriggered = true;
        canCollide = true; // NEW: Allow collision immediately for pre-falling rocks
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

        Debug.Log($"[FallingRock] Triggered by player - converting to falling rock!");

        // Start collision delay coroutine
        StartCoroutine(EnableCollisionAfterDelay());

        // Configure physics for falling
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        rb.mass = 50f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Visual feedback
        if (rockRenderer != null && fallingMaterial != null)
        {
            rockRenderer.material = fallingMaterial;
        }

        // Audio feedback
        if (triggerSound != null)
        {
            AudioSource.PlayClipAtPoint(triggerSound, transform.position);
        }

        // Remove idle VFX
        if (idleVFX != null)
        {
            foreach (Transform child in transform)
            {
                if (child.gameObject.name.Contains(idleVFX.name) || 
                    child.gameObject.name.Contains("Clone"))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        // Schedule destruction
        Destroy(gameObject, lifeTime);
    }

    // NEW: Coroutine to delay collision enabling
    private IEnumerator EnableCollisionAfterDelay()
    {
        // Keep as trigger briefly to let projectile pass through
        yield return new WaitForSeconds(collisionEnableDelay);

        // Now convert to solid collider
        if (rockCollider != null)
        {
            rockCollider.isTrigger = false;
            rockCollider.radius = 1f;
            canCollide = true;
            Debug.Log($"[FallingRock] Collision enabled - rock can now impact");
        }
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
        // NEW: Ignore collision if not ready yet
        if (!canCollide)
        {
            Debug.Log($"[FallingRock] Ignoring collision with {collision.gameObject.name} - not ready yet");
            return;
        }

        if (!hasBeenTriggered) return;
        if (hasImpacted) return;

        // NEW: Ignore projectiles entirely in collision
        if (collision.gameObject.GetComponent<ProjectileBase>() != null)
        {
            Debug.Log($"[FallingRock] Ignoring projectile collision");
            return;
        }

        hasImpacted = true;

        Debug.Log($"[FallingRock] Impacted with {collision.gameObject.name}");

        HandleRockImpact(collision);

        if (impactVFX != null)
        {
            Instantiate(impactVFX, transform.position, Quaternion.identity);
        }

        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, transform.position);
        }

        // Request respawn before destroying
        RequestRespawn();

        Destroy(gameObject, 0.1f);
    }

    private void HandleRockImpact(Collision collision)
    {
        // Check for ShellBoss first
        ShellBoss boss = collision.gameObject.GetComponent<ShellBoss>();
        if (boss != null)
        {
            boss.OnRockHit(this);
            Debug.Log($"[FallingRock] Hit ShellBoss!");
            return;
        }

        // Check for player
        PlayerCharacter player = collision.gameObject.GetComponent<PlayerCharacter>();
        if (player != null)
        {
            DamagePlayer(player);
            return;
        }

        Debug.Log($"[FallingRock] Hit ground/environment");
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

    private void RequestRespawn()
    {
        if (spawnPoint != null)
        {
            Debug.Log($"[FallingRock] Requesting respawn at {spawnPoint.name} after {respawnDelay}s");
            spawnPoint.RespawnRock(respawnDelay);
        }
        else
        {
            Debug.LogWarning($"[FallingRock] No spawn point assigned - rock will not respawn!");
        }
    }

    private void OnDestroy()
    {
        // If destroyed without impact (e.g., lifetime expired), still request respawn
        if (!hasImpacted && spawnPoint != null)
        {
            Debug.Log($"[FallingRock] Destroyed by lifetime - requesting respawn");
            spawnPoint.RespawnRock(respawnDelay);
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
        else if (hasBeenTriggered && !hasImpacted)
        {
            // NEW: Different color if collision not enabled yet
            Gizmos.color = canCollide ? Color.red : Color.orange;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
        else if (hasImpacted)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}