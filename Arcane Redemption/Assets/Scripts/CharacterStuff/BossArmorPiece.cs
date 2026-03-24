using UnityEngine;
using System.Collections;

/// <summary>
/// Represents a single piece of boss armor that can be destroyed at specific health thresholds.
/// Attach this component to each armor GameObject in the boss hierarchy.
/// </summary>
[RequireComponent(typeof(Disintegrate))]
public class BossArmorPiece : MonoBehaviour
{
    [Header("Armor Configuration")]
    [SerializeField] private string armorName = "Armor Piece";
    [SerializeField]
    [Tooltip("Health percentage (0-1) at which this armor piece should be removed")]
    private float removalThreshold = 0.8f;
    [SerializeField] private ArmorRemovalType removalType = ArmorRemovalType.PhysicsDrop;

    [Header("Physics Drop Settings")]
    [SerializeField] private bool addRigidbodyOnDrop = true;
    [SerializeField] private float physicsMass = 5f;
    [SerializeField] private float physicsImpulseForce = 3f;
    [SerializeField] private Vector3 impulseDirection = new Vector3(0.5f, 1f, 0.5f);
    [SerializeField] private bool addRandomSpin = true;
    [SerializeField] private float randomTorqueStrength = 5f;

    [Header("Ground Detection")]
    [SerializeField] private float groundCheckDelay = 0.2f;
    [SerializeField] private float groundCheckInterval = 0.1f;
    [SerializeField] private float velocityThreshold = 0.5f;
    [SerializeField] private LayerMask groundLayer = ~0;

    [Header("Disintegration Settings")]
    [SerializeField] private float disintegrationDelay = 0f;
    [SerializeField] private bool useDisintegrationScript = true;

    [Header("Destruction Effects")]
    [SerializeField] private GameObject destructionVFX;
    [SerializeField] private AudioClip destructionSound;
    [SerializeField] private float destructionDelay = 0f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private bool isRemoved = false;
    private Renderer[] renderers;
    private Rigidbody armorRigidbody;
    private Disintegrate disintegrate;
    private Collider armorCollider;
    private bool isGrounded = false;
    private Coroutine groundCheckCoroutine;

    public string ArmorName => armorName;
    public float RemovalThreshold => removalThreshold;
    public bool IsRemoved => isRemoved;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        disintegrate = GetComponent<Disintegrate>();
        armorCollider = GetComponent<Collider>();

        if (disintegrate == null && useDisintegrationScript)
        {
            Debug.LogWarning($"[{name}] Disintegrate component not found! Adding one...");
            disintegrate = gameObject.AddComponent<Disintegrate>();
        }
    }

    /// <summary>
    /// Removes this armor piece using the configured removal type.
    /// </summary>
    public void RemoveArmor()
    {
        if (isRemoved) return;

        isRemoved = true;

        if (showDebugLogs)
        {
            Debug.Log($"[{name}] 💥 Armor piece '{armorName}' removed at threshold {removalThreshold * 100}%");
        }

        PlayDestructionEffects();

        if (destructionDelay > 0)
        {
            StartCoroutine(DelayedRemoval());
        }
        else
        {
            ExecuteRemoval();
        }
    }

    private IEnumerator DelayedRemoval()
    {
        yield return new WaitForSeconds(destructionDelay);
        ExecuteRemoval();
    }

    private void ExecuteRemoval()
    {
        switch (removalType)
        {
            case ArmorRemovalType.Disable:
                DisableArmor();
                break;
            case ArmorRemovalType.DisableRenderers:
                DisableRenderers();
                break;
            case ArmorRemovalType.Destroy:
                DestroyArmor();
                break;
            case ArmorRemovalType.PhysicsDrop:
                PhysicsDropArmor();
                break;
            case ArmorRemovalType.DisintegratRenderers:
                DisintegratRenderers();
                break;
        }
    }

    private void DisableArmor()
    {
        gameObject.SetActive(false);
    }

    private void DisableRenderers()
    {
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
    }

    private void DestroyArmor()
    {
        Destroy(gameObject);
    }

    private void DisintegratRenderers()
    {
        StartCoroutine(TriggerDisintegrationAfterDelay());
    }

    /// <summary>
    /// NEW: Detaches armor from parent, applies physics, and triggers disintegration on ground impact.
    /// </summary>
    private void PhysicsDropArmor()
    {
        if (showDebugLogs)
        {
            Debug.Log($"[{name}] 🌀 Physics drop initiated for '{armorName}'");
        }

        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            if (renderer != null)
            {
                if (renderer.rootBone) {
                    renderer.rootBone = null;
                    renderer.bones = new Transform[0];
                }
            }
        }

        // Detach from parent (boss)
        transform.SetParent(null);
        transform.SetParent(null);

        // Add or configure Rigidbody for physics
        SetupPhysics();

        // Apply impulse force to launch armor away
        ApplyImpulseForce();

        // Start checking for ground contact
        if (groundCheckCoroutine != null)
        {
            StopCoroutine(groundCheckCoroutine);
        }
        groundCheckCoroutine = StartCoroutine(CheckForGroundContact());
    }

    /// <summary>
    /// Sets up physics components for the armor piece.
    /// </summary>
    private void SetupPhysics()
    {
        if (!addRigidbodyOnDrop)
        {
            armorRigidbody = GetComponent<Rigidbody>();
            if (armorRigidbody == null)
            {
                Debug.LogWarning($"[{name}] PhysicsDrop requires a Rigidbody but none found!");
                return;
            }
        }
        else
        {
            armorRigidbody = GetComponent<Rigidbody>();
            if (armorRigidbody == null)
            {
                armorRigidbody = gameObject.AddComponent<Rigidbody>();
            }
        }

        // Configure Rigidbody
        armorRigidbody.mass = physicsMass;
        armorRigidbody.useGravity = true;
        armorRigidbody.isKinematic = false;
        armorRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Ensure collider exists
        if (armorCollider == null)
        {
            armorCollider = gameObject.AddComponent<BoxCollider>();
            if (showDebugLogs)
            {
                Debug.LogWarning($"[{name}] No collider found! Added BoxCollider automatically.");
            }
        }

        if (armorCollider != null)
        {
            armorCollider.isTrigger = false; // Must be solid for physics
        }
    }

    /// <summary>
    /// Applies an impulse force to launch the armor piece away from the boss.
    /// </summary>
    private void ApplyImpulseForce()
    {
        if (armorRigidbody == null) return;

        // Normalize and apply force
        Vector3 impulse = impulseDirection.normalized * physicsImpulseForce;
        armorRigidbody.AddForce(impulse, ForceMode.VelocityChange);

        if (showDebugLogs)
        {
            Debug.Log($"[{name}] Applied impulse: {impulse}");
        }

        // Add random spin if enabled
        if (addRandomSpin)
        {
            Vector3 randomTorque = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ).normalized * randomTorqueStrength;

            armorRigidbody.AddTorque(randomTorque, ForceMode.VelocityChange);
        }
    }

    /// <summary>
    /// Coroutine that checks if the armor has landed on the ground.
    /// </summary>
    private IEnumerator CheckForGroundContact()
    {
        // Wait initial delay before checking
        yield return new WaitForSeconds(groundCheckDelay);

        while (!isGrounded)
        {
            // Check if velocity is low enough (armor has settled)
            if (armorRigidbody != null && armorRigidbody.linearVelocity.magnitude < velocityThreshold)
            {
                // Raycast down to confirm ground contact
                if (IsOnGround())
                {
                    OnGroundContact();
                    yield break;
                }
            }

            yield return new WaitForSeconds(groundCheckInterval);
        }
    }

    /// <summary>
    /// Checks if the armor is on the ground using raycast.
    /// </summary>
    private bool IsOnGround()
    {
        if (armorCollider == null) return false;

        // Get the bottom of the collider
        Vector3 colliderBottom = armorCollider.bounds.min;
        float rayDistance = 0.2f;

        // Raycast down from bottom
        bool hit = Physics.Raycast(
            colliderBottom + Vector3.up * 0.1f,
            Vector3.down,
            rayDistance,
            groundLayer
        );

        if (showDebugLogs && hit)
        {
            Debug.Log($"[{name}] Ground detected beneath armor");
        }

        return hit;
    }

    /// <summary>
    /// Called when the armor piece contacts the ground.
    /// </summary>
    private void OnGroundContact()
    {
        isGrounded = true;

        if (showDebugLogs)
        {
            Debug.Log($"[{name}] ✓ Armor landed on ground - starting disintegration");
        }

        // Disable physics to prevent further movement
        if (armorRigidbody != null)
        {
            armorRigidbody.isKinematic = true;
            armorRigidbody.linearVelocity = Vector3.zero;
            armorRigidbody.angularVelocity = Vector3.zero;
        }

        // Trigger disintegration after delay
        StartCoroutine(TriggerDisintegrationAfterDelay());
    }

    /// <summary>
    /// Triggers the disintegration effect after a delay.
    /// </summary>
    private IEnumerator TriggerDisintegrationAfterDelay()
    {
        if (disintegrationDelay > 0)
        {
            yield return new WaitForSeconds(disintegrationDelay);
        }

        if (useDisintegrationScript && disintegrate != null)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[{name}] 🔥 Triggering disintegration for '{armorName}'");
            }

            disintegrate.TriggerDisintegration();
        }
        else
        {
            // Fallback: just destroy after a delay
            Destroy(gameObject, 2f);
        }
    }

    private void PlayDestructionEffects()
    {
        if (destructionVFX != null)
        {
            Instantiate(destructionVFX, transform.position, transform.rotation);
        }

        if (destructionSound != null)
        {
            AudioSource.PlayClipAtPoint(destructionSound, transform.position);
        }
    }

    /// <summary>
    /// Restores the armor piece to its original state (useful for boss reset).
    /// </summary>
    public void RestoreArmor()
    {
        if (!isRemoved) return;

        isRemoved = false;
        isGrounded = false;

        // Stop ground checking
        if (groundCheckCoroutine != null)
        {
            StopCoroutine(groundCheckCoroutine);
            groundCheckCoroutine = null;
        }

        // Remove physics components if they were added
        if (armorRigidbody != null)
        {
            Destroy(armorRigidbody);
            armorRigidbody = null;
        }

        // Re-enable visuals
        gameObject.SetActive(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"[{name}] Armor piece '{armorName}' restored");
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugLogs || !Application.isPlaying) return;

        // Draw impulse direction
        if (removalType == ArmorRemovalType.PhysicsDrop)
        {
            Gizmos.color = Color.yellow;
            Vector3 impulseDir = impulseDirection.normalized * physicsImpulseForce;
            Gizmos.DrawRay(transform.position, impulseDir);
            Gizmos.DrawWireSphere(transform.position + impulseDir, 0.2f);
        }

        // Show ground check status
        if (isRemoved && !isGrounded)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
        else if (isGrounded)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}

public enum ArmorRemovalType
{
    Disable,           // Deactivates the GameObject
    DisableRenderers,  // Only disables renderers (keeps colliders active)
    Destroy,           // Destroys the GameObject immediately
    PhysicsDrop,        // NEW: Detaches, applies physics, and disintegrates on ground contact
    DisintegratRenderers  // NEW: Disintegrates without detaching
}