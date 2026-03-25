using UnityEngine;
using System.Collections;

public class FallingRock : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private bool requiresPlayerTrigger = true;

    [Header("Physics Settings")]
    [SerializeField] private float fallSpeed = 10f;
    [SerializeField] private float lifeTime = 10f;

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
    [SerializeField] private float impactSoundVolume = 1f;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    private Rigidbody rb;
    private bool hasBeenTriggered;
    private bool hasImpacted;
    private float spawnTime;
    private MeshCollider meshCollider;  
    private Renderer rockRenderer;
    private RockSpawnPoint spawnPoint;
    private bool isInitialized = false;

    public bool HasBeenTriggered => hasBeenTriggered;
    public bool IsAvailable => !hasBeenTriggered && !hasImpacted;
    public Vector3 GroundPosition => new Vector3(transform.position.x, 0f, transform.position.z);

    private void Awake()
    {
        int fallingRocksLayer = LayerMask.NameToLayer("FallingRocks");
        int spawnPlatformLayer = LayerMask.NameToLayer("RockSpawnLayer");

        if (fallingRocksLayer != -1 && spawnPlatformLayer != -1)
        {
            Physics.IgnoreLayerCollision(fallingRocksLayer, spawnPlatformLayer, true);
        }

        InitializeComponents();

        if (requiresPlayerTrigger)
        {
            ConfigureAsStationaryRock();
        }
        else
        {
            ConfigureAsFallingRock();
        }

        spawnTime = Time.time;
        isInitialized = true;
    }

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = false;
        rb.isKinematic = true;

        rockRenderer = GetComponent<Renderer>();
        if (rockRenderer == null)
        {
            rockRenderer = GetComponentInChildren<Renderer>();
        }

        meshCollider = GetComponent<MeshCollider>();
    }

    public void SetSpawnPoint(RockSpawnPoint point)
    {
        spawnPoint = point;
    }

    private void ConfigureAsStationaryRock()
    {
        if (meshCollider == null || meshCollider.sharedMesh == null)
        {
            return;
        }

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        meshCollider.convex = true;
        meshCollider.isTrigger = true;

        ApplyIdleMaterial();
        SpawnIdleVFX();
    }

    private void ConfigureAsFallingRock()
    {
        if (meshCollider == null || meshCollider.sharedMesh == null)
        {
            return;
        }

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.mass = 50f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        meshCollider.convex = true;
        meshCollider.isTrigger = false;

        hasBeenTriggered = true;
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isInitialized || !requiresPlayerTrigger || hasBeenTriggered)
        {
            return;
        }

        if (IsPlayerProjectile(other))
        {
            TriggerRockFall();
        }
    }

    private bool IsPlayerProjectile(Collider other)
    {
        ProjectileBase projectile = other.GetComponent<ProjectileBase>();
        if (projectile == null)
        {
            return false;
        }

        BaseCharacter owner = projectile.GetOwner();
        if (owner == null)
        {
            return false;
        }

        return owner.GetComponent<PlayerCharacter>() != null;
    }

    public void TriggerRockFall()
    {
        if (hasBeenTriggered || meshCollider == null || meshCollider.sharedMesh == null)
        {
            return;
        }

        hasBeenTriggered = true;

        meshCollider.isTrigger = false;
        ConfigureFallingPhysics();

        ApplyFallingVisuals();
        PlayTriggerSound();
        RemoveIdleVFX();

        Destroy(gameObject, lifeTime);
    }

    private void ConfigureFallingPhysics()
    {
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        rb.mass = 50f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void ApplyIdleMaterial()
    {
        if (rockRenderer != null && idleMaterial != null)
        {
            rockRenderer.material = idleMaterial;
        }
    }

    private void ApplyFallingVisuals()
    {
        if (rockRenderer != null && fallingMaterial != null)
        {
            rockRenderer.material = fallingMaterial;
        }
    }

    private void SpawnIdleVFX()
    {
        if (idleVFX != null)
        {
            Instantiate(idleVFX, transform.position, Quaternion.identity, transform);
        }
    }

    private void PlayTriggerSound()
    {
        if (triggerSound != null)
        {
            AudioSource.PlayClipAtPoint(triggerSound, transform.position);
        }
    }

    private void RemoveIdleVFX()
    {
        if (idleVFX == null)
        {
            return;
        }

        foreach (Transform child in transform)
        {
            if (child.gameObject.name.Contains(idleVFX.name) ||
                child.gameObject.name.Contains("Clone"))
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!isInitialized || !hasBeenTriggered || hasImpacted || rb == null)
        {
            return;
        }

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, -fallSpeed, rb.linearVelocity.z);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isInitialized || !hasBeenTriggered || hasImpacted)
        {
            return;
        }

        hasImpacted = true;

        Vector3 impactPosition = transform.position;

        HandleRockImpact(collision);
        SpawnImpactEffects(impactPosition);
        RequestRespawn();

        Destroy(gameObject, 0.1f);
    }

    private void HandleRockImpact(Collision collision)
    {
        if (TryHitBoss(collision)) return;
        if (TryHitPlayer(collision)) return;
    }

    private bool TryHitBoss(Collision collision)
    {
        ShellBoss boss = collision.gameObject.GetComponent<ShellBoss>();
        if (boss == null)
        {
            boss = collision.gameObject.GetComponentInParent<ShellBoss>();
        }

        if (boss == null)
        {
            return false;
        }

        boss.OnRockHit(this);
        return true;
    }

    private bool TryHitPlayer(Collision collision)
    {
        PlayerCharacter player = collision.gameObject.GetComponent<PlayerCharacter>();
        if (player == null)
        {
            player = collision.gameObject.GetComponentInParent<PlayerCharacter>();
        }

        if (player == null)
        {
            return false;
        }

        BaseCharacter baseChar = player.GetComponent<BaseCharacter>();
        if (baseChar != null)
        {
            baseChar.TakeDamage(playerDamage);
        }

        return true;
    }

    private void SpawnImpactEffects(Vector3 position)
    {
        if (impactVFX != null)
        {
            Instantiate(impactVFX, position, Quaternion.identity);
        }

        if (impactSound != null)
        {
            GameObject audioObject = new GameObject("RockImpactAudio");
            audioObject.transform.position = position;
            
            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.clip = impactSound;
            audioSource.volume = impactSoundVolume;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 5f;
            audioSource.maxDistance = 50f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.Play();

            Destroy(audioObject, impactSound.length + 0.1f);
        }
    }

    private void RequestRespawn()
    {
        if (spawnPoint != null)
        {
            spawnPoint.RespawnRock(respawnDelay);
        }
    }

    private void OnDestroy()
    {
        if (!hasImpacted && spawnPoint != null && hasBeenTriggered)
        {
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

        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = GetComponentInChildren<Renderer>();
        }

        Bounds bounds;

        if (renderer != null)
        {
            bounds = renderer.bounds;
        }
        else
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                bounds = col.bounds;
            }
            else
            {
                bounds = new Bounds(transform.position, Vector3.one * 2f);
            }
        }

        if (requiresPlayerTrigger && !hasBeenTriggered)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
        else if (hasBeenTriggered && !hasImpacted)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
        else if (hasImpacted)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}