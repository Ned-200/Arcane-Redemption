using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TreeBoss : EnemyCharacter
{
    #region Serialized Fields

    [Header("Boss Detection Settings")]
    [SerializeField] private float spawnDetectionDelay = 4f;
    [SerializeField] private float advanceStopDistance = 7f;
    [SerializeField] private float advanceSpeed = 2f;

    [Header("Boss Attack Ranges")]
    [SerializeField] private float meleeAttackRange = 5f;
    [SerializeField] private float projectileMinRange = 16f; // NEW: 16m minimum for projectiles
    [SerializeField] private float projectileMaxRange = 25f; // Maximum range for projectiles

    [Header("Boss Attack Damage")]
    [SerializeField] private float armSlamDamage = 25f;
    [SerializeField] private float projectileDamage = 15f;

    [Header("Boss Attack Cooldowns")]
    [SerializeField] private float meleeAttackCooldown = 1.5f;
    [SerializeField] private float rangedAttackCooldown = 3f;

    [Header("Projectile Settings")]
    [SerializeField] private GameObject trackingProjectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private int maxProjectilesPerVolley = 4;
    [SerializeField] private float projectileVolleyDelay = 0.4f;

    [Header("Projectile Volley Limit")]
    [SerializeField] private int minVolleyLimit = 2;
    [SerializeField] private int maxVolleyLimit = 3;
    [SerializeField] private float volleyLimitResetDelay = 5f;

    [Header("Hit Counter Dash Retreat System - NEW")]
    [SerializeField] private int hitsBeforeDashRetreat = 3; // Hits from player before dash
    [SerializeField] private float dashRetreatDistance = 10f; // Distance to dash away
    [SerializeField] private float dashRetreatSpeed = 12f; // Fast dash speed
    [SerializeField] private float dashRetreatDuration = 0.6f; // Duration of dash
    [SerializeField] private float dashRetreatCooldown = 5f; // Cooldown between dashes

    [Header("Boss AI Behavior")]
    [SerializeField] private float healthThresholdForRetreat = 0.6f; // 60%
    [SerializeField] private float retreatDistance = 10f;
    [SerializeField] private float retreatSpeed = 4f;

    [Header("Enraged Mode (20% HP)")]
    [SerializeField] private float enrageHealthThreshold = 0.2f; // 20%
    [SerializeField] private float enrageChargeSpeed = 6f;
    [SerializeField] private float enragedMeleeCooldown = 0.8f; // Faster attacks when enraged
    [SerializeField] private float enragedDamageMultiplier = 1.3f;

    [Header("Animation")]
    [SerializeField] private Animator bossAnimator;

    [Header("Boss Debug")]
    [SerializeField] private bool showBossGizmos = true;

    #endregion

    #region Private Fields

    // State machine
    private BossState currentBossState = BossState.Spawning;
    private BossAttackState currentAttackState = BossAttackState.Idle;

    // Timers
    private float spawnTimer;
    private float lastMeleeAttackTime;
    private float lastRangedAttackTime;
    private float lastDashRetreatTime;

    // Detection and targeting
    private bool isAdvancingToPlayer;
    private Transform playerTransform;

    // Attack combo system
    private bool isPerformingCombo;
    private Queue<ArmSlamType> currentComboQueue = new Queue<ArmSlamType>();
    private Coroutine currentComboCoroutine;

    // Projectile volley system
    private bool isFiringProjectileVolley;
    private Coroutine projectileVolleyCoroutine;
    private int consecutiveVolleyCount;
    private int currentVolleyLimit;
    private bool mustAdvanceAfterVolley;

    // Tactical movement
    private bool isRetreating;
    private Vector3 retreatTargetPosition;
    private bool isAdvancingAfterVolley;

    // NEW: Hit counter and dash retreat system
    private int hitsTakenInMeleeZone;
    private bool isDashRetreating;
    private Coroutine dashRetreatCoroutine;

    // Enraged mode
    private bool isEnraged;
    private bool hasEnteredEnragedMode;

    #endregion

    #region Properties

    public BossState CurrentBossState => currentBossState;
    public BossAttackState CurrentAttackState => currentAttackState;
    public bool IsPerformingCombo => isPerformingCombo;
    public bool IsFiringProjectileVolley => isFiringProjectileVolley;
    public bool IsRetreating => isRetreating;
    public bool IsEnraged => isEnraged;
    public bool IsAdvancingAfterVolley => isAdvancingAfterVolley;
    public int ConsecutiveVolleyCount => consecutiveVolleyCount;
    public int CurrentVolleyLimit => currentVolleyLimit;
    public int HitsTakenInMeleeZone => hitsTakenInMeleeZone; // NEW

    #endregion

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();
        InitializeBoss();
    }

    protected override void Update()
    {
        base.Update();

        if (IsDead) return;

        // Check for enrage trigger
        CheckEnrageCondition();

        UpdateBossStateMachine();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes boss-specific components and settings
    /// </summary>
    private void InitializeBoss()
    {
        spawnTimer = 0f;
        isAdvancingToPlayer = false;
        isFiringProjectileVolley = false;
        isRetreating = false;
        isAdvancingAfterVolley = false;
        isDashRetreating = false;
        isEnraged = false;
        hasEnteredEnragedMode = false;

        // Initialize volley limit system
        consecutiveVolleyCount = 0;
        currentVolleyLimit = Random.Range(minVolleyLimit, maxVolleyLimit + 1);
        mustAdvanceAfterVolley = false;

        // Initialize hit counter
        hitsTakenInMeleeZone = 0;
        lastDashRetreatTime = -dashRetreatCooldown; // Allow first dash immediately

        if (bossAnimator == null)
        {
            bossAnimator = GetComponent<Animator>();
            if (bossAnimator == null)
            {
                Debug.LogWarning($"[{gameObject.name}] No Animator component found!");
            }
        }

        if (projectileSpawnPoint == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Projectile spawn point not assigned! Using boss position.");
            projectileSpawnPoint = transform;
        }

        if (trackingProjectilePrefab == null)
        {
            Debug.LogError($"[{gameObject.name}] Tracking projectile prefab not assigned!");
        }

        Debug.Log($"[{gameObject.name}] Initialized with volley limit: {currentVolleyLimit}");
    }

    #endregion

    #region Hit Counter System - NEW

    /// <summary>
    /// Called when boss takes damage - increments hit counter in melee zone
    /// </summary>
    protected override void OnDamageTaken(float damage)
    {
        base.OnDamageTaken(damage);

        // Only count hits when player is within melee zone (<16m)
        if (TargetPlayer != null && !isEnraged)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, TargetPlayer.position);
            
            if (distanceToPlayer < projectileMinRange)
            {
                hitsTakenInMeleeZone++;
                Debug.Log($"[{gameObject.name}] Hit #{hitsTakenInMeleeZone} taken in melee zone ({distanceToPlayer:F1}m)");

                // Check if should perform dash retreat
                if (hitsTakenInMeleeZone >= hitsBeforeDashRetreat)
                {
                    TryPerformDashRetreat();
                }
            }
        }
    }

    /// <summary>
    /// Attempts to perform a dash retreat after taking required hits
    /// </summary>
    private void TryPerformDashRetreat()
    {
        // Check cooldown
        if (Time.time - lastDashRetreatTime < dashRetreatCooldown)
        {
            Debug.Log($"[{gameObject.name}] Dash retreat on cooldown ({(dashRetreatCooldown - (Time.time - lastDashRetreatTime)):F1}s remaining)");
            return;
        }

        // Can't dash when already dashing or retreating
        if (isDashRetreating || isRetreating)
        {
            return;
        }

        // Can't dash when enraged
        if (isEnraged)
        {
            Debug.Log($"[{gameObject.name}] Cannot dash retreat - ENRAGED!");
            return;
        }

        PerformDashRetreat();
    }

    /// <summary>
    /// Executes a fast dash retreat to a random position 10m away
    /// </summary>
    private void PerformDashRetreat()
    {
        if (TargetPlayer == null) return;

        // Stop any ongoing actions
        if (currentComboCoroutine != null)
        {
            StopCoroutine(currentComboCoroutine);
            isPerformingCombo = false;
            currentComboCoroutine = null;
        }

        // Reset hit counter
        hitsTakenInMeleeZone = 0;
        lastDashRetreatTime = Time.time;

        // Calculate random dash direction (away from player with some randomness)
        Vector3 directionAwayFromPlayer = (transform.position - TargetPlayer.position).normalized;
        
        // Add random angle variation (-45 to +45 degrees)
        float randomAngle = Random.Range(-45f, 45f);
        Quaternion randomRotation = Quaternion.Euler(0f, randomAngle, 0f);
        Vector3 dashDirection = randomRotation * directionAwayFromPlayer;

        Vector3 dashTargetPosition = transform.position + dashDirection * dashRetreatDistance;
        dashTargetPosition.y = transform.position.y; // Keep on same Y level

        Debug.LogWarning($"[{gameObject.name}] 🏃 DASH RETREAT! After {hitsBeforeDashRetreat} hits - Dashing to {dashDirection * dashRetreatDistance}");

        // Trigger dash animation
        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("Dash");
        }

        // Start dash coroutine
        if (dashRetreatCoroutine != null)
        {
            StopCoroutine(dashRetreatCoroutine);
        }
        dashRetreatCoroutine = StartCoroutine(DashRetreatSequence(dashTargetPosition));
    }

    /// <summary>
    /// Coroutine that handles the dash retreat movement
    /// </summary>
    private IEnumerator DashRetreatSequence(Vector3 targetPosition)
    {
        isDashRetreating = true;
        float dashStartTime = Time.time;

        Debug.Log($"[{gameObject.name}] Dash retreat started - Speed: {dashRetreatSpeed}m/s, Duration: {dashRetreatDuration}s");

        while (isDashRetreating && Time.time - dashStartTime < dashRetreatDuration)
        {
            // Cancel dash if enraged
            if (isEnraged)
            {
                isDashRetreating = false;
                yield break;
            }

            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

            // Stop if reached target
            if (distanceToTarget < 0.5f)
            {
                Debug.Log($"[{gameObject.name}] Dash retreat complete - Reached target");
                break;
            }

            // Fast dash movement
            MoveTowardsTarget(targetPosition, dashRetreatSpeed);

            // Don't rotate during dash (maintains momentum)

            yield return null;
        }

        isDashRetreating = false;
        dashRetreatCoroutine = null;
        
        Debug.Log($"[{gameObject.name}] Dash retreat finished");
    }

    /// <summary>
    /// Resets hit counter (called when exiting melee zone)
    /// </summary>
    private void ResetHitCounter()
    {
        if (hitsTakenInMeleeZone > 0)
        {
            Debug.Log($"[{gameObject.name}] Hit counter reset (was {hitsTakenInMeleeZone})");
            hitsTakenInMeleeZone = 0;
        }
    }

    #endregion

    #region Enrage System

    /// <summary>
    /// Checks if boss should enter enraged mode at 20% health
    /// </summary>
    private void CheckEnrageCondition()
    {
        if (hasEnteredEnragedMode) return;

        if (HealthPercent <= enrageHealthThreshold)
        {
            EnterEnragedMode();
        }
    }

    /// <summary>
    /// Triggers enraged mode - boss charges and switches to melee only
    /// </summary>
    private void EnterEnragedMode()
    {
        if (hasEnteredEnragedMode) return;

        hasEnteredEnragedMode = true;
        isEnraged = true;

        // Stop any ongoing actions
        StopAllBossActions();

        Debug.LogWarning($"[{gameObject.name}] ⚠ ENRAGED MODE ACTIVATED! Health at {HealthPercent * 100:F1}% ⚠");

        // Trigger enrage animation/VFX
        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("Enrage");
        }

        // Visual feedback for enrage
        OnEnragedModeEntered();
    }

    /// <summary>
    /// Stops all current boss actions when entering enraged mode
    /// </summary>
    private void StopAllBossActions()
    {
        // Stop combo
        if (currentComboCoroutine != null)
        {
            StopCoroutine(currentComboCoroutine);
            isPerformingCombo = false;
            currentComboCoroutine = null;
        }

        // Stop projectile volley
        if (projectileVolleyCoroutine != null)
        {
            StopCoroutine(projectileVolleyCoroutine);
            isFiringProjectileVolley = false;
            projectileVolleyCoroutine = null;
        }

        // Stop retreat
        isRetreating = false;
        isAdvancingAfterVolley = false;

        // Stop dash retreat
        if (dashRetreatCoroutine != null)
        {
            StopCoroutine(dashRetreatCoroutine);
            isDashRetreating = false;
            dashRetreatCoroutine = null;
        }

        // Reset counters
        ResetVolleyCounter();
        ResetHitCounter();
    }

    /// <summary>
    /// Called when enraged mode is entered - override for visual effects
    /// </summary>
    protected virtual void OnEnragedModeEntered()
    {
        // TODO: Add visual effects
        // - Red glow/aura
        // - Particle effects
        // - Screen shake
        // - Boss roar sound effect
        
        Debug.Log($"[{gameObject.name}] Boss is now enraged! Melee only mode activated!");
    }

    #endregion

    #region Boss State Machine

    /// <summary>
    /// Updates the boss behavior based on current state
    /// </summary>
    private void UpdateBossStateMachine()
    {
        switch (currentBossState)
        {
            case BossState.Spawning:
                HandleSpawningState();
                break;

            case BossState.Advancing:
                HandleAdvancingState();
                break;

            case BossState.Fighting:
                HandleFightingState();
                break;

            case BossState.BossDefeated:
                // Do nothing, handled by death system
                break;
        }
    }

    /// <summary>
    /// Handles spawning state - waits for player detection or triggers advance
    /// </summary>
    private void HandleSpawningState()
    {
        spawnTimer += Time.deltaTime;

        // Check for player within detection radius
        if (DetectPlayerInRadius(DetectionRadius))
        {
            TransitionToFightingState();
            return;
        }

        // After spawn delay, start advancing towards player if not detected
        if (spawnTimer >= spawnDetectionDelay && !isAdvancingToPlayer)
        {
            InitiateAdvanceTowardsPlayer();
        }
    }

    /// <summary>
    /// Handles advancing state - boss moves towards player until within range
    /// </summary>
    private void HandleAdvancingState()
    {
        if (playerTransform == null)
        {
            TryFindPlayer();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Check if player entered detection radius during advance
        if (distanceToPlayer <= DetectionRadius)
        {
            TransitionToFightingState();
            return;
        }

        // Stop advancing if within stop distance (not in enraged mode)
        if (!isEnraged && distanceToPlayer <= advanceStopDistance)
        {
            StopAdvancing();
            return;
        }

        // Continue moving towards player (faster if enraged)
        float currentSpeed = isEnraged ? enrageChargeSpeed : advanceSpeed;
        MoveTowardsTarget(playerTransform.position, currentSpeed);
        RotateTowardsTarget(playerTransform.position);
    }

    /// <summary>
    /// Handles fighting state - performs attacks based on distance to player
    /// </summary>
    private void HandleFightingState()
    {
        if (TargetPlayer == null)
        {
            TransitionToSpawningState();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, TargetPlayer.position);

        // Always face the player (unless dashing or retreating)
        if (!isRetreating && !isDashRetreating)
        {
            RotateTowardsTarget(TargetPlayer.position);
        }

        // Handle dash retreat (highest priority)
        if (isDashRetreating)
        {
            // Dash movement handled in coroutine
            return;
        }

        // Handle retreat movement (not allowed when enraged)
        if (isRetreating && !isEnraged)
        {
            HandleRetreatMovement();
            return;
        }

        // Handle post-volley advance movement
        if (isAdvancingAfterVolley && !isEnraged)
        {
            HandlePostVolleyAdvance(distanceToPlayer);
            return;
        }

        // If enraged, cancel any retreat or advance
        if (isEnraged && (isRetreating || isAdvancingAfterVolley))
        {
            isRetreating = false;
            isAdvancingAfterVolley = false;
        }

        // Don't make new decisions if performing actions
        if (isPerformingCombo || isFiringProjectileVolley)
        {
            return;
        }

        // ENRAGED MODE: Melee only, aggressive charging
        if (isEnraged)
        {
            HandleEnragedBehavior(distanceToPlayer);
            return;
        }

        // NORMAL MODE: Distance-based combat behavior
        DetermineCombatBehavior(distanceToPlayer);
    }

    /// <summary>
    /// Determines combat behavior based on distance to player
    /// NEW: Projectiles at 16m+, melee only within 15m
    /// </summary>
    private void DetermineCombatBehavior(float distanceToPlayer)
    {
        // MELEE ZONE: Within 15m - Melee only, no projectiles
        if (distanceToPlayer < projectileMinRange)
        {
            // Within melee attack range
            if (distanceToPlayer <= meleeAttackRange)
            {
                Debug.Log($"[{gameObject.name}] In melee range ({distanceToPlayer:F1}m) - Melee attack");
                TryPerformMeleeAttack();
            }
            // Between melee range and projectile min range
            else
            {
                Debug.Log($"[{gameObject.name}] Melee zone ({distanceToPlayer:F1}m < 16m) - Advancing to melee");
                MoveTowardsTarget(TargetPlayer.position, advanceSpeed);
            }
        }
        // PROJECTILE ZONE: 16m to 25m - Use projectile volley system
        else if (distanceToPlayer >= projectileMinRange && distanceToPlayer <= projectileMaxRange)
        {
            Debug.Log($"[{gameObject.name}] Projectile range ({distanceToPlayer:F1}m >= 16m) - Volley system");
            
            // Reset hit counter when in projectile zone
            ResetHitCounter();
            
            HandleProjectileCombatBehavior(distanceToPlayer);
        }
        // VERY LONG RANGE: Beyond 25m - Advance only
        else if (distanceToPlayer > projectileMaxRange && distanceToPlayer <= DetectionRadius)
        {
            Debug.Log($"[{gameObject.name}] Very long range ({distanceToPlayer:F1}m > 25m) - Advancing");
            
            // Reset hit counter when far away
            ResetHitCounter();
            
            MoveTowardsTarget(TargetPlayer.position, advanceSpeed);
        }
        // OUT OF RANGE: Beyond detection radius
        else if (distanceToPlayer > DetectionRadius)
        {
            // Player escaped, return to spawning state
            TransitionToSpawningState();
        }
    }

    /// <summary>
    /// Handles behavior when boss is enraged - melee only, aggressive
    /// </summary>
    private void HandleEnragedBehavior(float distanceToPlayer)
    {
        // If outside melee range, charge towards player
        if (distanceToPlayer > meleeAttackRange)
        {
            MoveTowardsTarget(TargetPlayer.position, enrageChargeSpeed);
            Debug.Log($"[{gameObject.name}] Enraged - Charging at player! Distance: {distanceToPlayer:F1}m");
        }
        else
        {
            // Within melee range - spam melee attacks
            TryPerformEnragedMeleeAttack();
        }
    }

    /// <summary>
    /// Handles post-volley advance movement
    /// </summary>
    private void HandlePostVolleyAdvance(float distanceToPlayer)
    {
        // Stop advancing if within melee range
        if (distanceToPlayer <= meleeAttackRange)
        {
            isAdvancingAfterVolley = false;
            Debug.Log($"[{gameObject.name}] Reached melee range, stopping post-volley advance");
            return;
        }

        // Stop advancing if back within melee zone (< 16m)
        if (distanceToPlayer < projectileMinRange)
        {
            isAdvancingAfterVolley = false;
            Debug.Log($"[{gameObject.name}] Entered melee zone (<16m), stopping post-volley advance");
            return;
        }

        // Stop advancing if within advance stop distance
        if (distanceToPlayer <= advanceStopDistance)
        {
            isAdvancingAfterVolley = false;
            Debug.Log($"[{gameObject.name}] Reached advance stop distance, stopping post-volley advance");
            return;
        }

        // Continue advancing
        MoveTowardsTarget(TargetPlayer.position, advanceSpeed);
        Debug.Log($"[{gameObject.name}] Advancing after volley - Distance: {distanceToPlayer:F1}m");
    }

    #endregion

    #region Combat AI Brain

    /// <summary>
    /// Handles projectile combat behavior (16m-25m) - Volley limit system
    /// </summary>
    private void HandleProjectileCombatBehavior(float distanceToPlayer)
    {
        // Check if must advance due to volley limit
        if (mustAdvanceAfterVolley)
        {
            Debug.Log($"[{gameObject.name}] Volley limit reached ({consecutiveVolleyCount}/{currentVolleyLimit}) - Forcing advance");
            ForceAdvanceAfterVolleyLimit();
            return;
        }

        // Fire projectile volley if cooldown is ready
        if (Time.time - lastRangedAttackTime >= rangedAttackCooldown)
        {
            StartProjectileVolley();
            return;
        }
    }

    /// <summary>
    /// Called after projectile volley completes to decide next tactical move
    /// </summary>
    private void DecidePostVolleyTactics()
    {
        // Don't make tactical decisions if enraged
        if (isEnraged)
        {
            StartAdvancingTowardsPlayer();
            return;
        }

        // Increment volley counter
        IncrementVolleyCounter();

        // Check if volley limit reached
        if (consecutiveVolleyCount >= currentVolleyLimit)
        {
            Debug.Log($"[{gameObject.name}] Reached volley limit ({consecutiveVolleyCount}/{currentVolleyLimit}) - Must advance!");
            mustAdvanceAfterVolley = true;
            ForceAdvanceAfterVolleyLimit();
            return;
        }

        // Still have volleys available - decide based on health
        float currentHealthPercent = HealthPercent;

        if (currentHealthPercent > healthThresholdForRetreat)
        {
            // Above 60% health - 50/50 chance to fire again or advance
            bool shouldFireAgain = Random.value > 0.5f;

            if (shouldFireAgain)
            {
                Debug.Log($"[{gameObject.name}] Health above {healthThresholdForRetreat * 100}% - Decided to fire again ({consecutiveVolleyCount}/{currentVolleyLimit} volleys)");
                // Will fire again on next update cycle
            }
            else
            {
                Debug.Log($"[{gameObject.name}] Health above {healthThresholdForRetreat * 100}% - Decided to advance ({consecutiveVolleyCount}/{currentVolleyLimit} volleys)");
                StartPostVolleyAdvance();
            }
        }
        else
        {
            // Below 60% health - 50/50 chance to advance or retreat
            bool shouldAdvance = Random.value > 0.5f;

            if (shouldAdvance)
            {
                Debug.Log($"[{gameObject.name}] Health below {healthThresholdForRetreat * 100}% - Decided to advance (50/50)");
                StartPostVolleyAdvance();
            }
            else
            {
                Debug.Log($"[{gameObject.name}] Health below {healthThresholdForRetreat * 100}% - Decided to retreat (50/50)");
                StartRetreating();
            }
        }
    }

    /// <summary>
    /// Forces boss to advance after reaching volley limit
    /// </summary>
    private void ForceAdvanceAfterVolleyLimit()
    {
        Debug.Log($"[{gameObject.name}] Forced advance activated - Resetting volley counter");
        StartPostVolleyAdvance();
        StartCoroutine(ResetVolleyCounterDelayed());
    }

    /// <summary>
    /// Increments the consecutive volley counter
    /// </summary>
    private void IncrementVolleyCounter()
    {
        consecutiveVolleyCount++;
        Debug.Log($"[{gameObject.name}] Volley counter: {consecutiveVolleyCount}/{currentVolleyLimit}");
    }

    /// <summary>
    /// Resets the volley counter and generates new limit
    /// </summary>
    private void ResetVolleyCounter()
    {
        consecutiveVolleyCount = 0;
        currentVolleyLimit = Random.Range(minVolleyLimit, maxVolleyLimit + 1);
        mustAdvanceAfterVolley = false;
        Debug.Log($"[{gameObject.name}] Volley counter reset - New limit: {currentVolleyLimit}");
    }

    /// <summary>
    /// Resets volley counter after delay
    /// </summary>
    private IEnumerator ResetVolleyCounterDelayed()
    {
        yield return new WaitForSeconds(volleyLimitResetDelay);
        ResetVolleyCounter();
    }

    /// <summary>
    /// Starts advancing towards player after ranged attack
    /// </summary>
    private void StartAdvancingTowardsPlayer()
    {
        if (TargetPlayer == null) return;

        isRetreating = false;
        isAdvancingAfterVolley = false;
        Debug.Log($"[{gameObject.name}] Starting advance towards player");
    }

    /// <summary>
    /// Starts post-volley advance movement
    /// </summary>
    private void StartPostVolleyAdvance()
    {
        if (TargetPlayer == null) return;

        isRetreating = false;
        isAdvancingAfterVolley = true;
        
        Debug.Log($"[{gameObject.name}] Starting post-volley advance towards player");
    }

    /// <summary>
    /// Starts retreating away from player
    /// </summary>
    private void StartRetreating()
    {
        if (TargetPlayer == null) return;

        // Can't retreat when enraged
        if (isEnraged)
        {
            Debug.Log($"[{gameObject.name}] Cannot retreat - ENRAGED!");
            return;
        }

        // Calculate retreat position (away from player)
        Vector3 directionAwayFromPlayer = (transform.position - TargetPlayer.position).normalized;
        retreatTargetPosition = transform.position + directionAwayFromPlayer * retreatDistance;

        // Keep retreat position on same Y level
        retreatTargetPosition.y = transform.position.y;

        isRetreating = true;
        isAdvancingAfterVolley = false;
        
        Debug.Log($"[{gameObject.name}] Starting retreat {retreatDistance}m away from player");

        // Start retreat coroutine
        StartCoroutine(RetreatSequence());
    }

    /// <summary>
    /// Handles the retreat movement sequence
    /// </summary>
    private IEnumerator RetreatSequence()
    {
        float retreatStartTime = Time.time;
        float maxRetreatTime = 3f;

        while (isRetreating && Time.time - retreatStartTime < maxRetreatTime)
        {
            // Cancel retreat if enraged
            if (isEnraged)
            {
                isRetreating = false;
                yield break;
            }

            if (TargetPlayer == null)
            {
                isRetreating = false;
                yield break;
            }

            float distanceToRetreatTarget = Vector3.Distance(transform.position, retreatTargetPosition);

            // Stop retreating if reached target or close enough
            if (distanceToRetreatTarget < 1f)
            {
                Debug.Log($"[{gameObject.name}] Reached retreat position");
                break;
            }

            // Move towards retreat position
            MoveTowardsTarget(retreatTargetPosition, retreatSpeed);
            
            // Face player while retreating (Dark Souls style)
            RotateTowardsTarget(TargetPlayer.position);

            yield return null;
        }

        isRetreating = false;
        Debug.Log($"[{gameObject.name}] Retreat complete");
    }

    /// <summary>
    /// Handles movement during retreat
    /// </summary>
    private void HandleRetreatMovement()
    {
        // Movement is handled in RetreatSequence coroutine
    }

    #endregion

    #region State Transitions

    /// <summary>
    /// Transitions boss to spawning state
    /// </summary>
    private void TransitionToSpawningState()
    {
        currentBossState = BossState.Spawning;
        SetTarget(null);
        playerTransform = null;
        
        // Don't allow retreat or advance when transitioning
        if (!isEnraged)
        {
            isRetreating = false;
            isAdvancingAfterVolley = false;
        }

        // Reset counters
        ResetVolleyCounter();
        ResetHitCounter();
        
        Debug.Log($"[{gameObject.name}] Transitioned to Spawning state");
    }

    /// <summary>
    /// Transitions boss to fighting state
    /// </summary>
    private void TransitionToFightingState()
    {
        currentBossState = BossState.Fighting;
        isAdvancingToPlayer = false;
        
        if (playerTransform != null)
        {
            SetTarget(playerTransform);
        }
        
        Debug.Log($"[{gameObject.name}] Transitioned to Fighting state");
    }

    /// <summary>
    /// Initiates advancing behavior towards player
    /// </summary>
    private void InitiateAdvanceTowardsPlayer()
    {
        if (TryFindPlayer())
        {
            currentBossState = BossState.Advancing;
            isAdvancingToPlayer = true;
            Debug.Log($"[{gameObject.name}] Starting advance towards player");
        }
    }

    /// <summary>
    /// Stops the advancing behavior
    /// </summary>
    private void StopAdvancing()
    {
        // Don't stop advancing when enraged
        if (isEnraged)
        {
            return;
        }

        isAdvancingToPlayer = false;
        currentBossState = BossState.Spawning;
        Debug.Log($"[{gameObject.name}] Stopped advancing, returned to Spawning state");
    }

    #endregion

    #region Detection System

    /// <summary>
    /// Detects if player is within specified radius
    /// </summary>
    private bool DetectPlayerInRadius(float radius)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider hit in hits)
        {
            if (IsPlayerCharacter(hit))
            {
                playerTransform = hit.transform;
                SetTarget(playerTransform);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Tries to find the player in the scene
    /// </summary>
    private bool TryFindPlayer()
    {
        PlayerCharacter player = FindFirstObjectByType<PlayerCharacter>();
        if (player != null)
        {
            playerTransform = player.transform;
            return true;
        }

        Debug.LogWarning($"[{gameObject.name}] Could not find player in scene!");
        return false;
    }

    /// <summary>
    /// Checks if collider belongs to a player character
    /// </summary>
    private bool IsPlayerCharacter(Collider col)
    {
        return col.GetComponent<PlayerCharacter>() != null;
    }

    #endregion

    #region Movement System

    /// <summary>
    /// Moves boss towards target position at specified speed
    /// </summary>
    private void MoveTowardsTarget(Vector3 targetPosition, float speed)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f; // Keep movement horizontal

        transform.position += direction * speed * Time.deltaTime;
    }

    /// <summary>
    /// Rotates boss to face target position
    /// </summary>
    private void RotateTowardsTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f; // Keep rotation horizontal

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
        }
    }

    #endregion

    #region Melee Attack System

    /// <summary>
    /// Attempts to perform a melee attack combo (normal mode)
    /// </summary>
    private void TryPerformMeleeAttack()
    {
        if (Time.time - lastMeleeAttackTime < meleeAttackCooldown)
        {
            return;
        }

        if (isPerformingCombo)
        {
            return;
        }

        // Stop advancing when starting melee
        isAdvancingAfterVolley = false;

        // Generate random combo
        MeleeCombo combo = GenerateRandomMeleeCombo();
        ExecuteMeleeCombo(combo, false);

        lastMeleeAttackTime = Time.time;
    }

    /// <summary>
    /// Attempts to perform an enraged melee attack (faster, more damage)
    /// </summary>
    private void TryPerformEnragedMeleeAttack()
    {
        if (Time.time - lastMeleeAttackTime < enragedMeleeCooldown)
        {
            return;
        }

        if (isPerformingCombo)
        {
            return;
        }

        // Generate random combo
        MeleeCombo combo = GenerateRandomMeleeCombo();
        ExecuteMeleeCombo(combo, true);

        lastMeleeAttackTime = Time.time;
    }

    /// <summary>
    /// Generates a random melee combo (1, 3, or 5 attacks)
    /// </summary>
    private MeleeCombo GenerateRandomMeleeCombo()
    {
        // Determine combo length: 1, 3, or 5 attacks
        int[] possibleLengths = { 1, 3, 5 };
        int comboLength = possibleLengths[Random.Range(0, possibleLengths.Length)];

        MeleeCombo combo = new MeleeCombo();

        for (int i = 0; i < comboLength; i++)
        {
            ArmSlamType slamType = (ArmSlamType)Random.Range(0, System.Enum.GetValues(typeof(ArmSlamType)).Length);
            combo.AddAttack(slamType);
        }

        return combo;
    }

    /// <summary>
    /// Executes the melee combo sequence
    /// </summary>
    private void ExecuteMeleeCombo(MeleeCombo combo, bool isEnragedAttack)
    {
        if (currentComboCoroutine != null)
        {
            StopCoroutine(currentComboCoroutine);
        }

        currentComboCoroutine = StartCoroutine(PerformMeleeComboSequence(combo, isEnragedAttack));
    }

    /// <summary>
    /// Coroutine that performs the melee combo sequence
    /// </summary>
    private IEnumerator PerformMeleeComboSequence(MeleeCombo combo, bool isEnragedAttack)
    {
        isPerformingCombo = true;
        currentAttackState = BossAttackState.MeleeAttacking;

        string modeText = isEnragedAttack ? "ENRAGED" : "NORMAL";
        Debug.Log($"[{gameObject.name}] Starting {modeText} melee combo with {combo.AttackCount} attacks");

        foreach (ArmSlamType slamType in combo.Attacks)
        {
            yield return StartCoroutine(PerformArmSlam(slamType, isEnragedAttack));
            
            // Shorter delay between attacks when enraged
            float comboDelay = isEnragedAttack ? 0.15f : 0.3f;
            yield return new WaitForSeconds(comboDelay);
        }

        isPerformingCombo = false;
        currentAttackState = BossAttackState.Idle;
        currentComboCoroutine = null;

        Debug.Log($"[{gameObject.name}] Completed {modeText} melee combo");
    }

    /// <summary>
    /// Performs a single arm slam attack
    /// </summary>
    private IEnumerator PerformArmSlam(ArmSlamType slamType, bool isEnragedAttack)
    {
        // Trigger animation
        TriggerArmSlamAnimation(slamType);

        // Faster animation timing when enraged
        float animationWait = isEnragedAttack ? 0.3f : 0.5f;
        yield return new WaitForSeconds(animationWait);

        // Deal damage to player if in range
        if (TargetPlayer != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, TargetPlayer.position);
            if (distanceToPlayer <= meleeAttackRange)
            {
                DealMeleeDamageToPlayer(slamType, isEnragedAttack);
            }
        }

        // Shorter recovery when enraged
        float recoveryTime = isEnragedAttack ? 0.15f : 0.3f;
        yield return new WaitForSeconds(recoveryTime);
    }

    /// <summary>
    /// Triggers the appropriate arm slam animation
    /// </summary>
    private void TriggerArmSlamAnimation(ArmSlamType slamType)
    {
        if (bossAnimator == null) return;

        string animationTrigger = slamType switch
        {
            ArmSlamType.RightArm => "RightArmSlam",
            ArmSlamType.LeftArm => "LeftArmSlam",
            ArmSlamType.BothArms => "BothArmsSlam",
            _ => "RightArmSlam"
        };

        bossAnimator.SetTrigger(animationTrigger);
        Debug.Log($"[{gameObject.name}] Triggered animation: {animationTrigger}");
    }

    /// <summary>
    /// Deals melee damage to the player
    /// </summary>
    private void DealMeleeDamageToPlayer(ArmSlamType slamType, bool isEnragedAttack)
    {
        if (TargetPlayer == null) return;

        BaseCharacter targetCharacter = TargetPlayer.GetComponent<BaseCharacter>();
        if (targetCharacter != null)
        {
            // Both arms deal more damage
            float damageMultiplier = slamType == ArmSlamType.BothArms ? 1.5f : 1f;
            
            // Enraged attacks deal additional damage           
            if (isEnragedAttack)
            {
                damageMultiplier *= enragedDamageMultiplier;
            }

            float totalDamage = armSlamDamage * damageMultiplier;

            targetCharacter.TakeDamage(totalDamage);
            
            string enragedText = isEnragedAttack ? " [ENRAGED]" : "";
            Debug.Log($"[{gameObject.name}] Hit player with {slamType} for {totalDamage} damage!{enragedText}");
        }
    }

    #endregion

    #region Ranged Attack System

    /// <summary>
    /// Starts a projectile volley with randomized count
    /// Only usable at 16m+ range
    /// </summary>
    private void StartProjectileVolley()
    {
        // No ranged attacks when enraged
        if (isEnraged)  
        {
            Debug.Log($"[{gameObject.name}] Cannot use ranged attacks - ENRAGED! Melee only!");
            return;
        }

        if (isFiringProjectileVolley) return;

        if (trackingProjectilePrefab == null)
        {
            Debug.LogError($"[{gameObject.name}] Cannot perform ranged attack: projectile prefab is null!");
            return;
        }

        // Stop advancing when firing projectiles
        isAdvancingAfterVolley = false;

        if (projectileVolleyCoroutine != null)
        {
            StopCoroutine(projectileVolleyCoroutine);
        }

        projectileVolleyCoroutine = StartCoroutine(PerformProjectileVolley());
    }

    /// <summary>
    /// Coroutine that fires a volley of projectiles with delays
    /// </summary>
    private IEnumerator PerformProjectileVolley()
    {
        isFiringProjectileVolley = true;
        currentAttackState = BossAttackState.RangedAttacking;

        // Randomize number of projectiles (1 to maxProjectilesPerVolley)
        int projectileCount = Random.Range(1, maxProjectilesPerVolley + 1);
        
        float initialDistance = TargetPlayer != null ? Vector3.Distance(transform.position, TargetPlayer.position) : 0f;
        string volleyInfo = $"[Volley {consecutiveVolleyCount + 1}/{currentVolleyLimit}]";
        
        Debug.Log($"[{gameObject.name}] Starting projectile volley at {initialDistance:F1}m - Firing {projectileCount} projectiles [STATIONARY] {volleyInfo}");

        for (int i = 0; i < projectileCount; i++)
        {
            // Stop volley if boss becomes enraged mid-volley
            if (isEnraged)
            {
                Debug.Log($"[{gameObject.name}] Projectile volley interrupted - Boss entered enraged mode!");
                break;
            }

            // Fire single projectile (boss is stationary during this)
            FireSingleProjectile();

            Debug.Log($"[{gameObject.name}] Fired projectile {i + 1}/{projectileCount} [STATIONARY]");

            // Wait before firing next projectile (except for last one)
            if (i < projectileCount - 1)
            {
                yield return new WaitForSeconds(projectileVolleyDelay);
            }
        }

        // Volley complete
        Debug.Log($"[{gameObject.name}] Projectile volley complete - Movement decision next");

        lastRangedAttackTime = Time.time;
        isFiringProjectileVolley = false;
        currentAttackState = BossAttackState.Idle;
        projectileVolleyCoroutine = null;

        // Decide next tactical move after volley
        DecidePostVolleyTactics();
    }

    /// <summary>
    /// Fires a single tracking projectile at the player
    /// </summary>
    private void FireSingleProjectile()
    {
        if (TargetPlayer == null) return;

        // Trigger ranged attack animation
        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("RangedAttack");
        }

        // Spawn tracking projectile
        Vector3 spawnPosition = projectileSpawnPoint.position;
        Quaternion spawnRotation = Quaternion.LookRotation(TargetPlayer.position - spawnPosition);

        GameObject projectileObj = Instantiate(trackingProjectilePrefab, spawnPosition, spawnRotation);

        // Initialize projectile with tracking behavior
        TreeBossProjectile trackingProjectile = projectileObj.GetComponent<TreeBossProjectile>();
        if (trackingProjectile != null)
        {
            trackingProjectile.Initialize(projectileDamage, this, projectileSpeed, TargetPlayer);
        }
        else
        {
            // Fallback to base projectile if TreeBossProjectile is not attached
            ProjectileBase baseProjectile = projectileObj.GetComponent<ProjectileBase>();
            if (baseProjectile != null)
            {
                baseProjectile.Initialize(projectileDamage, this, projectileSpeed);
            }
        }
    }

    #endregion

    #region Override Methods

    /// <summary>
    /// Override death to transition to defeated state and stop all coroutines
    /// </summary>
    protected override void OnDeath()
    {
        base.OnDeath();
        currentBossState = BossState.BossDefeated;
        
        // Stop any ongoing combos
        if (currentComboCoroutine != null)
        {
            StopCoroutine(currentComboCoroutine);
            isPerformingCombo = false;
        }

        // Stop any ongoing projectile volleys
        if (projectileVolleyCoroutine != null)
        {
            StopCoroutine(projectileVolleyCoroutine);
            isFiringProjectileVolley = false;
        }

        // Stop retreat and advance
        isRetreating = false;
        isAdvancingAfterVolley = false;

        // Stop dash retreat
        if (dashRetreatCoroutine != null)
        {
            StopCoroutine(dashRetreatCoroutine);
            isDashRetreating = false;
        }

        Debug.Log($"[{gameObject.name}] BOSS DEFEATED!");
        
        // TODO: Trigger boss defeat events (cinematic, loot, achievement, etc.)
    }

    /// <summary>
    /// Override state changed to handle boss-specific state changes
    /// </summary>
    protected override void OnStateChanged(EnemyState newState)
    {
        base.OnStateChanged(newState);
        // Additional boss-specific state change logic
    }

    #endregion

    #region Debug Gizmos

    private void OnDrawGizmosSelected()
    {
        if (!showBossGizmos) return;

        // Detection radius (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, DetectionRadius);

        // Projectile max range (outer boundary - white)
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, projectileMaxRange);

        // Projectile minimum range (inner boundary - light blue) - 16m
        Gizmos.color = new Color(0.5f, 0.8f, 1f);
        Gizmos.DrawWireSphere(transform.position, projectileMinRange);

        // Advance stop distance (cyan)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, advanceStopDistance);

        // Melee attack range (red - brighter when enraged)
        Gizmos.color = isEnraged ? new Color(1f, 0f, 0f, 1f) : Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRange);

        // Dash retreat distance (orange) - NEW
        if (Application.isPlaying && !isEnraged)
        {
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, dashRetreatDistance);
        }

        // Retreat distance (purple - not shown when enraged)
        if (!isEnraged)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, retreatDistance);
        }

        // Line to target during play mode
        if (Application.isPlaying && TargetPlayer != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, TargetPlayer.position);
            
            // Color code the line based on combat mode and range
            if (isEnraged)
            {
                Gizmos.color = Color.red; // Enraged
            }
            else if (distanceToPlayer < projectileMinRange)
            {
                Gizmos.color = Color.green; // Melee zone (<16m)
            }
            else if (distanceToPlayer < projectileMaxRange)
            {
                Gizmos.color = Color.magenta; // Projectile zone (16m-25m)
            }
            else
            {
                Gizmos.color = Color.gray; // Very long range
            }
            
            Gizmos.DrawLine(transform.position, TargetPlayer.position);

            // Show retreat target if retreating
            if (isRetreating && !isEnraged)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(retreatTargetPosition, 1f);
                Gizmos.DrawLine(transform.position, retreatTargetPosition);
            }

            // Show post-volley advance indicator
            if (isAdvancingAfterVolley)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, TargetPlayer.position + Vector3.up * 0.5f);
            }

            // Show dash retreat indicator - NEW
            if (isDashRetreating)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position + Vector3.up * 1f, TargetPlayer.position + Vector3.up * 1f);
            }

            // Show volley limit indicator
            if (consecutiveVolleyCount > 0)
            {
                Gizmos.color = mustAdvanceAfterVolley ? Color.red : Color.yellow;
                Vector3 volleyIndicatorPos = transform.position + Vector3.up * 5f;
                float indicatorSize = 0.3f * consecutiveVolleyCount;
                Gizmos.DrawWireSphere(volleyIndicatorPos, indicatorSize);
            }

            // Show hit counter indicator - NEW
            if (hitsTakenInMeleeZone > 0 && distanceToPlayer < projectileMinRange)
            {
                Gizmos.color = hitsTakenInMeleeZone >= hitsBeforeDashRetreat ? Color.red : new Color(1f, 0.5f, 0f);
                Vector3 hitCounterPos = transform.position + Vector3.up * 6f;
                float hitIndicatorSize = 0.2f * hitsTakenInMeleeZone;
                Gizmos.DrawWireSphere(hitCounterPos, hitIndicatorSize);
            }
        }

        // Health indicator (green to red gradient, flashing red when enraged)
        if (Application.isPlaying)
        {
            Color healthColor = Color.Lerp(Color.red, Color.green, HealthPercent);
            
            // Flash red when enraged
            if (isEnraged && Time.time % 0.5f < 0.25f)
            {
                healthColor = Color.red;
            }

            Gizmos.color = healthColor;
            Vector3 healthBarStart = transform.position + Vector3.up * 4f;
            Vector3 healthBarEnd = healthBarStart + Vector3.right * 2f * HealthPercent;
            Gizmos.DrawLine(healthBarStart, healthBarEnd);

            // Enrage threshold indicator
            Gizmos.color = Color.yellow;
            Vector3 enrageThresholdPos = healthBarStart + Vector3.right * 2f * enrageHealthThreshold;
            Gizmos.DrawWireSphere(enrageThresholdPos, 0.1f);

            // Retreat threshold indicator
            Gizmos.color = Color.cyan;
            Vector3 retreatThresholdPos = healthBarStart + Vector3.right * 2f * healthThresholdForRetreat;
            Gizmos.DrawWireSphere(retreatThresholdPos, 0.1f);
        }
    }

    #endregion
}

#region Boss Enums and Data Structures

/// <summary>
/// Represents the high-level boss states
/// </summary>
public enum BossState
{
    Spawning,       // Initial state, waiting for player
    Advancing,      // Moving towards player after spawn delay
    Fighting,       // Actively engaging player
    BossDefeated    // Boss has been killed
}

/// <summary>
/// Represents the current attack state
/// </summary>
public enum BossAttackState
{
    Idle,           // Not attacking
    MeleeAttacking, // Performing melee combo
    RangedAttacking // Performing ranged attack
}

/// <summary>
/// Types of arm slam attacks
/// </summary>
public enum ArmSlamType
{
    RightArm,   // Right arm slam
    LeftArm,    // Left arm slam
    BothArms    // Both arms slam (higher damage)
}

/// <summary>
/// Represents a melee attack combo sequence
/// </summary>
public class MeleeCombo
{
    private List<ArmSlamType> attacks = new List<ArmSlamType>();

    public IReadOnlyList<ArmSlamType> Attacks => attacks;
    public int AttackCount => attacks.Count;

    public void AddAttack(ArmSlamType attackType)
    {
        attacks.Add(attackType);
    }

    public void Clear()
    {
        attacks.Clear();
    }
}

#endregion