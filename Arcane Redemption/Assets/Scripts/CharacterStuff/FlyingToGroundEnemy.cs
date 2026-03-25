using UnityEngine;

/// <summary>
/// Flying enemy that transitions between aerial states based on player distance.
/// Drops to the ground and switches to melee combat when health drops to 10 or below.
/// 
/// Phase 1 (Health 20-11): Flying State Machine
///   - Patrol: Fly between two patrol points
///   - Tracking: Move toward player when within ~15m
///   - Orbit: Circle around player when within ~7m
///   - Shooting: Fires projectiles when player is within 20m
/// 
/// Phase 2 (Health ≤10): Ground Combat
///   - Falls to ground and uses EnemyAIController for melee logic
/// </summary>
[RequireComponent(typeof(EnemyAIController))]
public class FlyingToGroundEnemy : EnemyCharacter
{
    #region Serialized Fields

    [Header("Flying Phase Settings")]
    [SerializeField] private Transform patrolPointA;
    [SerializeField] private Transform patrolPointB;
    [SerializeField] private float flyingMoveSpeed = 5f;
    [SerializeField] private float flyingRotationSpeed = 3f;
    
    [Header("State Transition Distances")]
    [SerializeField] private float trackingDistance = 15f;    // Enter Tracking when player is within this range
    [SerializeField] private float orbitDistance = 7f;        // Enter Orbit when player is within this range
    [SerializeField] private float patrolReturnDistance = 20f; // Return to Patrol when player exceeds this range
    
    [Header("Orbit Settings")]
    [SerializeField] private float orbitRadius = 7f;          // How far to stay from player while orbiting
    [SerializeField] private float orbitSpeed = 3f;           // Angular speed of orbit
    [SerializeField] private bool orbitClockwise = true;      // Orbit direction
    
    [Header("Projectile Settings")]
    [SerializeField] private GameObject projectilePrefab;     // TreeBossProjectile prefab
    [SerializeField] private Transform projectileSpawnPoint;  // Where projectiles spawn from
    [SerializeField] private float shootingRange = 20f;       // Max distance to shoot at player
    [SerializeField] private float shootCooldown = 2f;        // Time between shots
    [SerializeField] private float projectileSpeed = 15f;     // Speed of projectiles
    [SerializeField] private float projectileDamage = 10f;    // Damage per projectile
    
    [Header("Phase Transition")]
    [SerializeField] private float groundPhaseHealthThreshold = 10f; // Health value to trigger ground phase
    [SerializeField] private float groundDropSpeed = 8f;      // How fast to fall when transitioning
    [SerializeField] private float groundHeight = 0.5f;       // Y position when grounded
    
    [Header("Debug")]
    [SerializeField] private bool showFlyingDebugGizmos = true;

    #endregion

    #region Private Fields

    // Flying state machine
    private FlyingState currentFlyingState = FlyingState.Patrol;
    
    // Cached components
    private Transform cachedPlayerTransform;
    private Rigidbody rb;
    private EnemyAIController aiController;
    
    // Patrol state
    private Transform currentPatrolTarget;
    private const float PATROL_WAYPOINT_THRESHOLD_SQR = 1f; // sqrMagnitude threshold for reaching waypoint
    
    // Orbit state
    private float currentOrbitAngle;
    
    // Projectile shooting
    private float lastShootTime;
    private float shootingRangeSqr; // Cached squared shooting range
    
    // Phase tracking
    private bool isInFlyingPhase = true;
    private bool isTransitioningToGround = false;

    // Optimization: Cache squared distances to avoid sqrt calculations
    private float trackingDistanceSqr;
    private float orbitDistanceSqr;
    private float patrolReturnDistanceSqr;

    #endregion

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();
        
        InitializeFlyingEnemy();
        CacheDistanceThresholds();
    }

    private void Start()
    {
        CachePlayerReference();
        InitializePatrolState();
    }

    protected override void Update()
    {
        base.Update(); // Handle base stats and regeneration

        if (IsDead) return;

        // Phase-based behavior
        if (isInFlyingPhase)
        {
            UpdateFlyingStateMachine();
            UpdateProjectileShooting(); // Handle shooting during flying phase
        }
        else if (isTransitioningToGround)
        {
            UpdateGroundTransition();
        }
        // Ground phase is handled entirely by EnemyAIController (no manual update needed)
    }

    #endregion

    #region Initialization

    private void InitializeFlyingEnemy()
    {
        rb = GetComponent<Rigidbody>();
        aiController = GetComponent<EnemyAIController>();
        
        // Validate required components
        if (aiController == null)
        {
            Debug.LogError($"[{gameObject.name}] EnemyAIController is required but not found! Add [RequireComponent] or attach manually.", this);
        }
        
        // Configure Rigidbody for flying (if present)
        if (rb != null)
        {
            rb.useGravity = false; // Disable gravity while flying
            rb.isKinematic = false;
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] No Rigidbody found! Add one for physics-based flying movement.", this);
        }
        
        // Disable EnemyAIController during flying phase (enable when grounded)
        if (aiController != null)
        {
            aiController.enabled = false;
            Debug.Log($"[{gameObject.name}] EnemyAIController disabled for flying phase. Will activate on ground transition.");
        }
        
        // Validate patrol points
        if (patrolPointA == null || patrolPointB == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Patrol points not assigned! Flying patrol will not work correctly.", this);
        }
        
        // Validate projectile settings
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Projectile prefab not assigned! Enemy will not be able to shoot.", this);
        }
        
        if (projectileSpawnPoint == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Projectile spawn point not assigned! Using enemy position as spawn point.", this);
        }
        
        // Initialize shooting timer
        lastShootTime = -shootCooldown; // Allow immediate first shot
    }

    private void CachePlayerReference()
    {
        // Try to find player by tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            cachedPlayerTransform = playerObj.transform;
            SetTarget(cachedPlayerTransform); // Set base class target
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] Could not find player! AI will not function.", this);
        }
    }

    private void CacheDistanceThresholds()
    {
        // Pre-calculate squared distances for optimization
        trackingDistanceSqr = trackingDistance * trackingDistance;
        orbitDistanceSqr = orbitDistance * orbitDistance;
        patrolReturnDistanceSqr = patrolReturnDistance * patrolReturnDistance;
        shootingRangeSqr = shootingRange * shootingRange;
    }

    private void InitializePatrolState()
    {
        // Start patrolling toward point A
        currentPatrolTarget = patrolPointA;
        currentFlyingState = FlyingState.Patrol;
    }

    #endregion

    #region Flying State Machine

    private void UpdateFlyingStateMachine()
    {
        if (cachedPlayerTransform == null) return;

        // Calculate squared distance to player (optimization: avoids sqrt)
        float distanceToPlayerSqr = (cachedPlayerTransform.position - transform.position).sqrMagnitude;

        // Determine state transitions based on player distance
        FlyingState newState = DetermineNextFlyingState(distanceToPlayerSqr);
        
        // Transition to new state if changed
        if (newState != currentFlyingState)
        {
            OnFlyingStateChanged(currentFlyingState, newState);
            currentFlyingState = newState;
        }

        // Execute current state behavior
        switch (currentFlyingState)
        {
            case FlyingState.Patrol:
                ExecutePatrolState();
                break;
            
            case FlyingState.Tracking:
                ExecuteTrackingState();
                break;
            
            case FlyingState.Orbit:
                ExecuteOrbitState(distanceToPlayerSqr);
                break;
        }
    }

    /// <summary>
    /// Determines the next flying state based on player distance using sqrMagnitude for optimization
    /// </summary>
    private FlyingState DetermineNextFlyingState(float distanceToPlayerSqr)
    {
        // Orbit state: Player is within orbit distance (~7m)
        if (distanceToPlayerSqr <= orbitDistanceSqr)
        {
            return FlyingState.Orbit;
        }
        
        // Tracking state: Player is within tracking distance (~15m) but outside orbit distance
        if (distanceToPlayerSqr <= trackingDistanceSqr)
        {
            return FlyingState.Tracking;
        }
        
        // Patrol state: Player is far away (>20m when already in Patrol, or >15m when in other states)
        // Hysteresis prevents rapid state switching at boundaries
        if (currentFlyingState == FlyingState.Patrol)
        {
            // Stay in patrol if player is beyond tracking distance
            if (distanceToPlayerSqr > trackingDistanceSqr)
            {
                return FlyingState.Patrol;
            }
        }
        else
        {
            // Return to patrol only if player is very far away
            if (distanceToPlayerSqr > patrolReturnDistanceSqr)
            {
                return FlyingState.Patrol;
            }
        }
        
        // Stay in current state if no transition criteria met
        return currentFlyingState;
    }

    #endregion

    #region State Execution

    /// <summary>
    /// Patrol State: Fly back and forth between patrol points A and B
    /// </summary>
    private void ExecutePatrolState()
    {
        if (currentPatrolTarget == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No patrol target assigned!");
            return;
        }

        Vector3 targetPosition = currentPatrolTarget.position;
        Vector3 directionToTarget = targetPosition - transform.position;
        
        // Check if reached waypoint using sqrMagnitude (optimization)
        if (directionToTarget.sqrMagnitude <= PATROL_WAYPOINT_THRESHOLD_SQR)
        {
            // Switch patrol target
            currentPatrolTarget = (currentPatrolTarget == patrolPointA) ? patrolPointB : patrolPointA;
            return;
        }

        // Move toward patrol target
        MoveToward(targetPosition, flyingMoveSpeed);
        RotateToward(directionToTarget, flyingRotationSpeed);
    }

    /// <summary>
    /// Tracking State: Move directly toward the player to close the gap
    /// </summary>
    private void ExecuteTrackingState()
    {
        if (cachedPlayerTransform == null) return;

        Vector3 targetPosition = cachedPlayerTransform.position;
        Vector3 directionToPlayer = targetPosition - transform.position;

        // Move toward player
        MoveToward(targetPosition, flyingMoveSpeed);
        RotateToward(directionToPlayer, flyingRotationSpeed);
    }

    /// <summary>
    /// Orbit State: Circle around the player while maintaining orbit distance and facing them
    /// </summary>
    private void ExecuteOrbitState(float currentDistanceSqr)
    {
        if (cachedPlayerTransform == null) return;

        Vector3 playerPosition = cachedPlayerTransform.position;
        Vector3 directionToPlayer = transform.position - playerPosition;
        
        // Maintain orbit radius by adjusting distance
        float currentDistance = Mathf.Sqrt(currentDistanceSqr);
        float distanceError = currentDistance - orbitRadius;
        
        // Calculate orbit position: move tangent to circle while correcting distance
        float orbitDirection = orbitClockwise ? -1f : 1f;
        currentOrbitAngle += orbitSpeed * orbitDirection * Time.deltaTime;
        
        // Calculate tangent vector (perpendicular to radius)
        Vector3 tangent = Vector3.Cross(directionToPlayer.normalized, Vector3.up).normalized;
        
        // Combine tangential movement with radial correction
        Vector3 orbitMovement = tangent * orbitSpeed;
        Vector3 radialCorrection = -directionToPlayer.normalized * distanceError * 2f; // Correction factor
        
        Vector3 targetVelocity = orbitMovement + radialCorrection;
        Vector3 targetPosition = transform.position + targetVelocity * Time.deltaTime;
        
        // Move and face the player
        MoveToward(targetPosition, flyingMoveSpeed);
        RotateToward(-directionToPlayer, flyingRotationSpeed * 1.5f); // Faster rotation to keep facing player
    }

    #endregion

    #region Projectile Shooting

    /// <summary>
    /// Updates projectile shooting logic during flying phase.
    /// Enemy shoots when player is within shooting range and cooldown has elapsed.
    /// </summary>
    private void UpdateProjectileShooting()
    {
        if (cachedPlayerTransform == null || projectilePrefab == null) return;

        // Calculate squared distance to player (optimization: avoids sqrt)
        float distanceToPlayerSqr = (cachedPlayerTransform.position - transform.position).sqrMagnitude;

        // Check if player is within shooting range
        if (distanceToPlayerSqr <= shootingRangeSqr)
        {
            // Check if cooldown has elapsed
            if (Time.time - lastShootTime >= shootCooldown)
            {
                ShootProjectile();
                lastShootTime = Time.time;
            }
        }
    }

    /// <summary>
    /// Spawns and fires a projectile at the player
    /// </summary>
    private void ShootProjectile()
    {
        if (projectilePrefab == null || cachedPlayerTransform == null) return;

        // Determine spawn position (use spawn point if assigned, otherwise use enemy position)
        Vector3 spawnPosition = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;

        // Instantiate projectile
        GameObject projectileObj = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        
        // Get TreeBossProjectile component
        TreeBossProjectile projectile = projectileObj.GetComponent<TreeBossProjectile>();
        
        if (projectile != null)
        {
            // Initialize projectile with damage, owner, speed, and target
            projectile.Initialize(projectileDamage, this, projectileSpeed, cachedPlayerTransform);
            
            Debug.Log($"[{gameObject.name}] 🎯 Fired projectile at {cachedPlayerTransform.name}! Damage: {projectileDamage}, Speed: {projectileSpeed}");
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Projectile prefab does not have TreeBossProjectile component!", this);
            Destroy(projectileObj);
        }
    }

    #endregion

    #region Movement Helpers

    /// <summary>
    /// Moves the enemy toward a target position at the specified speed
    /// </summary>
    private void MoveToward(Vector3 targetPosition, float speed)
    {
        Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        
        if (rb != null)
        {
            rb.MovePosition(newPosition);
        }
        else
        {
            transform.position = newPosition;
        }
    }

    /// <summary>
    /// Rotates the enemy to face the given direction at the specified rotation speed
    /// </summary>
    private void RotateToward(Vector3 direction, float rotSpeed)
    {
        if (direction.sqrMagnitude < 0.001f) return; // Avoid rotating toward zero vector

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        Quaternion newRotation = Quaternion.Slerp(transform.rotation, targetRotation, rotSpeed * Time.deltaTime);
        
        if (rb != null)
        {
            rb.MoveRotation(newRotation);
        }
        else
        {
            transform.rotation = newRotation;
        }
    }

    #endregion

    #region Phase Transition

    /// <summary>
    /// Override TakeDamage to detect when health drops to ground phase threshold
    /// </summary>
    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage); // Apply damage using base class logic

        // Check if we should transition to ground phase
        if (isInFlyingPhase && CurrentHealth <= groundPhaseHealthThreshold && !IsDead)
        {
            TransitionToGroundPhase();
        }
    }

    /// <summary>
    /// Transitions from flying phase to ground combat phase
    /// </summary>
    private void TransitionToGroundPhase()
    {
        Debug.Log($"[{gameObject.name}] Health dropped to {CurrentHealth:F1}! Transitioning to ground phase...");
        
        isInFlyingPhase = false;
        isTransitioningToGround = true;
        
        // Enable gravity to make enemy fall
        if (rb != null)
        {
            rb.useGravity = true;
        }
        
        OnEnteredGroundPhase();
    }

    /// <summary>
    /// Updates the transition from flying to grounded state
    /// </summary>
    private void UpdateGroundTransition()
    {
        // Check if enemy has reached the ground
        if (transform.position.y <= groundHeight)
        {
            CompleteGroundTransition();
        }
    }

    /// <summary>
    /// Completes the ground transition and enables EnemyAIController for ground combat.
    /// EnemyAIController uses transform.position directly, no CharacterController needed.
    /// </summary>
    private void CompleteGroundTransition()
    {
        Debug.Log($"[{gameObject.name}] Grounded! Switching to EnemyAIController for melee combat.");
        
        isTransitioningToGround = false;
        
        // Snap to ground height
        Vector3 groundedPosition = transform.position;
        groundedPosition.y = groundHeight;
        transform.position = groundedPosition;
        
        // Stop Rigidbody physics - EnemyAIController will use transform.position directly
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            Debug.Log($"[{gameObject.name}] Rigidbody set to kinematic. EnemyAIController will handle movement via transform.position.");
        }
        
        // Enable EnemyAIController to take over ground combat logic
        if (aiController != null)
        {
            aiController.enabled = true;
            
            // Set initial state to Combat since player is likely nearby
            if (cachedPlayerTransform != null)
            {
                SetState(EnemyState.Combat);
            }
            else
            {
                SetState(EnemyState.Idle);
            }
            
            Debug.Log($"[{gameObject.name}] ✅ EnemyAIController enabled. Ground combat active.");
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] ❌ No EnemyAIController found! Ground combat will not function.", this);
        }
        
        OnGroundPhaseCompleted();
    }

    #endregion

    #region Virtual Event Methods

    /// <summary>
    /// Called when the flying state changes
    /// </summary>
    protected virtual void OnFlyingStateChanged(FlyingState oldState, FlyingState newState)
    {
        Debug.Log($"[{gameObject.name}] Flying state: {oldState} → {newState}");
    }

    /// <summary>
    /// Called when entering ground phase (before falling)
    /// </summary>
    protected virtual void OnEnteredGroundPhase()
    {
        // Override in derived classes for custom behavior (e.g., play scream sound, visual effects)
    }

    /// <summary>
    /// Called when ground transition is complete and EnemyAIController takes over
    /// </summary>
    protected virtual void OnGroundPhaseCompleted()
    {
        // Override in derived classes for custom behavior (e.g., play landing particle effect)
    }

    #endregion

    #region Debug Gizmos

    private void OnDrawGizmosSelected()
    {
        if (!showFlyingDebugGizmos) return;

        // Draw patrol route
        if (patrolPointA != null && patrolPointB != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(patrolPointA.position, patrolPointB.position);
            Gizmos.DrawWireSphere(patrolPointA.position, 0.5f);
            Gizmos.DrawWireSphere(patrolPointB.position, 0.5f);
        }

        // Draw state transition ranges
        Vector3 position = Application.isPlaying ? transform.position : 
                          (patrolPointA != null ? patrolPointA.position : transform.position);

        // Shooting range (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(position, shootingRange);

        // Tracking distance (cyan)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(position, trackingDistance);

        // Orbit distance (magenta)
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(position, orbitDistance);

        // Patrol return distance (gray)
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(position, patrolReturnDistance);

        // Show current state
        if (Application.isPlaying && isInFlyingPhase)
        {
            Gizmos.color = GetStateColor(currentFlyingState);
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
        
        // Draw projectile spawn point
        if (projectileSpawnPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(projectileSpawnPoint.position, 0.3f);
            
            if (Application.isPlaying && cachedPlayerTransform != null)
            {
                Gizmos.DrawLine(projectileSpawnPoint.position, cachedPlayerTransform.position);
            }
        }
    }

    private Color GetStateColor(FlyingState state)
    {
        switch (state)
        {
            case FlyingState.Patrol: return Color.green;
            case FlyingState.Tracking: return Color.yellow;
            case FlyingState.Orbit: return Color.red;
            default: return Color.white;
        }
    }

    #endregion
}

/// <summary>
/// Enum representing the flying state machine states
/// </summary>
public enum FlyingState
{
    Patrol,     // Flying between patrol points
    Tracking,   // Moving toward player
    Orbit       // Circling around player
}