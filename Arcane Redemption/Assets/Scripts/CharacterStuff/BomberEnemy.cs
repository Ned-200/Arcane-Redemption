using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Flying enemy that transitions between aerial states based on player distance.
/// Drops to the ground and switches to melee combat when health drops to 10 or below.
/// 
/// Phase 1 (Health 20): Flying State Machine
///   - Patrol: Fly between two patrol points
///   - Tracking: Move toward player when within ~15m
/// 
/// Phase 2 : Falling
///   - Falls to ground 
/// </summary>

// [RequireComponent(typeof(EnemyAIController))]
public class BomberEnemy : EnemyCharacter
{
    #region Serialized Fields

    [Header("Flying Phase Settings")]
    [SerializeField] private Transform patrolPointA;
    [SerializeField] private Transform patrolPointB;
    [SerializeField] private float flyingMoveSpeed = 5f;
    [SerializeField] private float flyingRotationSpeed = 3f;
    [SerializeField] private float dropRange = 5.0f;
    [SerializeField] private float splashDamage = 10.0f;
    [SerializeField] private float splashDamageRadius = 10.0f;
    private float flyingHeight;
    
    [Header("State Transition Distances")]
    [SerializeField] private float trackingDistance = 15f;    // Enter Tracking when player is within this range
    [SerializeField] private float patrolReturnDistance = 20f; // Return to Patrol when player exceeds this range
    
    [Header("Drop Transition")]
    [SerializeField] private float groundDropSpeed = 8f;      // How fast to fall when transitioning
    [SerializeField] private Animator bomberEnemyAnim;
    [SerializeField] private GameObject blastEffectPrefab;
    [SerializeField] protected LayerMask groundLayers;
    protected bool hasHit = false;
    
    [Header("Debug")]
    [SerializeField] private bool showFlyingDebugGizmos = true;

    #endregion

    #region Private Fields

    // Flying state machine
    private BomberFlyingState currentBomberFlyingState = BomberFlyingState.Patrol;
    
    // Cached components
    private Transform cachedPlayerTransform;
    private Rigidbody rb;
    private EnemyAIController aiController;
    
    // Patrol state
    private Transform currentPatrolTarget;
    private const float PATROL_WAYPOINT_THRESHOLD_SQR = 1f; // sqrMagnitude threshold for reaching waypoint
    
    // Phase tracking
    private bool isInFlyingPhase = true;
    private bool isTransitioningToGround = false;

    // Optimization: Cache squared distances to avoid sqrt calculations
    private float trackingDistanceSqr;
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
            UpdateBomberFlyingStateMachine();
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
        // aiController = GetComponent<EnemyAIController>();
        
        // Validate required components
        // if (aiController == null)
        // {
        //     Debug.LogError($"[{gameObject.name}] EnemyAIController is required but not found! Add [RequireComponent] or attach manually.", this);
        // }
        
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
        // if (aiController != null)
        // {
        //     aiController.enabled = false;
        //     Debug.Log($"[{gameObject.name}] EnemyAIController disabled for flying phase. Will activate on ground transition.");
        // }
        
        // Validate patrol points
        if (patrolPointA == null || patrolPointB == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Patrol points not assigned! Flying patrol will not work correctly.", this);
        }

        // Set starting "altitude", so enemy doesnt drop early by descening to player
        flyingHeight = transform.position.y;
        
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
        patrolReturnDistanceSqr = patrolReturnDistance * patrolReturnDistance;
    }

    private void InitializePatrolState()
    {
        // Start patrolling toward point A
        currentPatrolTarget = patrolPointA;
        currentBomberFlyingState = BomberFlyingState.Patrol;
    }

    #endregion

    #region Flying State Machine

    private void UpdateBomberFlyingStateMachine()
    {
        if (cachedPlayerTransform == null) return;

        // Calculate squared distance to player (optimization: avoids sqrt)
        float distanceToPlayerSqr = (cachedPlayerTransform.position - transform.position).sqrMagnitude;

        // Determine state transitions based on player distance
        BomberFlyingState newState = DetermineNextBomberFlyingState(distanceToPlayerSqr);
        
        // Transition to new state if changed
        if (newState != currentBomberFlyingState)
        {
            OnBomberFlyingStateChanged(currentBomberFlyingState, newState);
            currentBomberFlyingState = newState;
        }

        // Execute current state behavior
        switch (currentBomberFlyingState)
        {
            case BomberFlyingState.Patrol:
                ExecutePatrolState();
                break;
            
            case BomberFlyingState.Tracking:
                ExecuteTrackingState();
                break;
        }
    }

    /// <summary>
    /// Determines the next flying state based on player distance using sqrMagnitude for optimization
    /// </summary>
    private BomberFlyingState DetermineNextBomberFlyingState(float distanceToPlayerSqr)
    {
        
        // Tracking state: Player is within tracking distance (~15m)
        if (distanceToPlayerSqr <= trackingDistanceSqr)
        {
            return BomberFlyingState.Tracking;
        }
        
        // Patrol state: Player is far away (>20m when already in Patrol, or >15m when in other states)
        // Hysteresis prevents rapid state switching at boundaries
        if (currentBomberFlyingState == BomberFlyingState.Patrol)
        {
            // Stay in patrol if player is beyond tracking distance
            if (distanceToPlayerSqr > trackingDistanceSqr)
            {
                return BomberFlyingState.Patrol;
            }
        }
        else
        {
            // Return to patrol only if player is very far away
            if (distanceToPlayerSqr > patrolReturnDistanceSqr)
            {
                return BomberFlyingState.Patrol;
            }
        }
        
        // Stay in current state if no transition criteria met
        return currentBomberFlyingState;
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

        var distX = Mathf.Abs(targetPosition.x - transform.position.x);
        var distZ = Mathf.Abs(targetPosition.z - transform.position.z);
        
        if ((distX < dropRange && distZ < dropRange) && !isTransitioningToGround)
        {
            TransitionToDropPhase();
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
        newPosition.y = flyingHeight;

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
        
        // Always drop when hit
        TransitionToDropPhase();
    }

    /// <summary>
    /// Transitions from flying phase to ground combat phase
    /// </summary>
    private void TransitionToDropPhase()
    {
        Debug.Log($"[{gameObject.name}] Health dropped to {CurrentHealth:F1}! Transitioning to dropping phase...");
        
        isInFlyingPhase = false;
        isTransitioningToGround = true;
        
        // Enable gravity to make enemy fall
        if (rb != null)
        {
            rb.useGravity = true;
        }
        bomberEnemyAnim.SetBool("isDropping", true);

        OnEnteredGroundPhase();
    }

    /// <summary>
    /// Updates the transition from flying to grounded state
    /// </summary>
    private void UpdateGroundTransition()
    {
        // Don't continue if not correct layer
        if (hasHit) {
            CompleteGroundTransition();
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Collide if touching correct layer
        if ((groundLayers & (1 << other.gameObject.layer)) != 0)
        {
            hasHit = true;
        }
    }

    /// <summary>
    /// Completes the ground transition and enables EnemyAIController for ground combat.
    /// EnemyAIController uses transform.position directly, no CharacterController needed.
    /// </summary>
    private void CompleteGroundTransition()
    {
        Debug.Log($"[{gameObject.name}] Dropped! Damaging all characters in range");
        
        isTransitioningToGround = false;
        
        // Stop Rigidbody physics - EnemyAIController will use transform.position directly
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            Debug.Log($"[{gameObject.name}] Rigidbody set to kinematic. EnemyAIController will handle movement via transform.position.");
        }

        OnGroundPhaseCompleted();

        if (currentHealth <= 0)
        {
            return;
        }

        // Deal damage to surroundings, by finding all colliders in detection radius
        Collider[] hits = Physics.OverlapSphere(transform.position, splashDamageRadius);

        foreach (Collider hit in hits) {
            BaseCharacter character = hit.GetComponent<BaseCharacter>();
            if (character != null && character != this)
            {
                character.TakeDamage(splashDamage);
            }
        }
        TakeDamage(maxHealth); // Deal damage to self once, for death sequence

        Instantiate(blastEffectPrefab, transform.position, blastEffectPrefab.transform.rotation);
    }

    #endregion

    #region Virtual Event Methods

    /// <summary>
    /// Called when the flying state changes
    /// </summary>
    protected virtual void OnBomberFlyingStateChanged(BomberFlyingState oldState, BomberFlyingState newState)
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

        // Tracking distance (cyan)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(position, trackingDistance);

        // Patrol return distance (gray)
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(position, patrolReturnDistance);

        // Show current state
        if (Application.isPlaying && isInFlyingPhase)
        {
            Gizmos.color = GetStateColor(currentBomberFlyingState);
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }

    private Color GetStateColor(BomberFlyingState state)
    {
        switch (state)
        {
            case BomberFlyingState.Patrol: return Color.green;
            case BomberFlyingState.Tracking: return Color.yellow;
            default: return Color.white;
        }
    }

    #endregion
}

/// <summary>
/// Enum representing the flying state machine states
/// </summary>
public enum BomberFlyingState
{
    Patrol,     // Flying between patrol points
    Tracking,   // Moving toward player
}