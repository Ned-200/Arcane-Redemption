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
    [SerializeField] private float projectileMinRange = 16f;
    [SerializeField] private float projectileMaxRange = 25f;

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

    [Header("Hit Counter Dash Retreat System")]
    [SerializeField] private int hitsBeforeDashRetreat = 3;
    [SerializeField] private float dashRetreatDistance = 10f;
    [SerializeField] private float dashRetreatSpeed = 12f;
    [SerializeField] private float dashRetreatDuration = 0.6f;
    [SerializeField] private float dashRetreatCooldown = 5f;

    [Header("Boss AI Behavior")]
    [SerializeField] private float healthThresholdForRetreat = 0.6f;
    [SerializeField] private float retreatDistance = 10f;
    [SerializeField] private float retreatSpeed = 4f;

    [Header("Enraged Mode (20% HP)")]
    [SerializeField] private float enrageHealthThreshold = 0.2f;
    [SerializeField] private float enrageChargeSpeed = 6f;
    [SerializeField] private float enragedMeleeCooldown = 0.8f;
    [SerializeField] private float enragedDamageMultiplier = 1.3f;

    [Header("Physics Settings")]
    [SerializeField] private float bossMass = 500f;
    [SerializeField] private float linearDamping = 5f;
    [SerializeField] private float angularDamping = 10f;
    [SerializeField] private bool lockPositionDuringAttacks = true;

    [Header("Animation")]
    [SerializeField] private Animator bossAnimator;

    [Header("Boss Debug")]
    [SerializeField] private bool showBossGizmos = true;

    [Header("Change Terrain")]
    [SerializeField] private TerrainSwap terrainSwap;

    [Header("Boss Ghost NPC")]
    [SerializeField] private GameObject PlantGhostPrefab;
    private bool ghostSpawned = false;

    #endregion

    #region Private Fields

    private BossState currentBossState = BossState.Spawning;
    private BossAttackState currentAttackState = BossAttackState.Idle;

    private float spawnTimer;
    private float lastMeleeAttackTime;
    private float lastRangedAttackTime;
    private float lastDashRetreatTime;

    private bool isAdvancingToPlayer;
    private Transform playerTransform;

    private bool isPerformingCombo;
    private Queue<ArmSlamType> currentComboQueue = new Queue<ArmSlamType>();
    private Coroutine currentComboCoroutine;

    private bool isFiringProjectileVolley;
    private Coroutine projectileVolleyCoroutine;
    private int consecutiveVolleyCount;
    private int currentVolleyLimit;
    private bool mustAdvanceAfterVolley;

    private bool isRetreating;
    private Vector3 retreatTargetPosition;
    private bool isAdvancingAfterVolley;

    private int hitsTakenInMeleeZone;
    private bool isDashRetreating;
    private Coroutine dashRetreatCoroutine;

    private bool isEnraged;
    private bool hasEnteredEnragedMode;

    private Rigidbody rb;
    private Vector3 desiredMovementDirection = Vector3.zero;
    private float desiredMovementSpeed = 0f;

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
    public int HitsTakenInMeleeZone => hitsTakenInMeleeZone;

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

        CheckEnrageCondition();
        UpdateBossStateMachine();
    }

    private void FixedUpdate()
    {
        if (IsDead || rb == null) return;

        ApplyPhysicsMovement();
    }

    #endregion

    #region Initialization

    private void InitializeBoss()
    {
        InitializeState();
        InitializeCounters();
        InitializeRigidbody();
        InitializeComponents();
    }

    private void InitializeState()
    {
        spawnTimer = 0f;
        isAdvancingToPlayer = false;
        isFiringProjectileVolley = false;
        isRetreating = false;
        isAdvancingAfterVolley = false;
        isDashRetreating = false;
        isEnraged = false;
        hasEnteredEnragedMode = false;
    }

    private void InitializeCounters()
    {
        consecutiveVolleyCount = 0;
        currentVolleyLimit = Random.Range(minVolleyLimit, maxVolleyLimit + 1);
        mustAdvanceAfterVolley = false;
        hitsTakenInMeleeZone = 0;
        lastDashRetreatTime = -dashRetreatCooldown;
    }

    private void InitializeRigidbody()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"[{gameObject.name}] CRITICAL: TreeBoss requires a Rigidbody component!");
            return;
        }

        ConfigureRigidbody();
    }

    private void ConfigureRigidbody()
    {
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.mass = bossMass;
        rb.linearDamping = linearDamping;
        rb.angularDamping = angularDamping;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        Debug.Log($"[{gameObject.name}] Rigidbody configured - Mass: {rb.mass}, LinearDamping: {rb.linearDamping}, AngularDamping: {rb.angularDamping}");
    }

    private void InitializeComponents()
    {
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

    #region Physics Position Locking

    private void LockBossPosition(bool shouldLock)
    {
        if (rb == null || !lockPositionDuringAttacks) return;

        if (shouldLock)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | 
                             RigidbodyConstraints.FreezeRotationZ | 
                             RigidbodyConstraints.FreezePosition;
        }
        else
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | 
                             RigidbodyConstraints.FreezeRotationZ;
        }
    }

    #endregion

    #region Hit Counter System

    protected override void OnDamageTaken(float damage)
    {
        base.OnDamageTaken(damage);

        if (TargetPlayer != null && !isEnraged)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, TargetPlayer.position);
            
            if (distanceToPlayer < projectileMinRange)
            {
                hitsTakenInMeleeZone++;
                Debug.Log($"[{gameObject.name}] Hit #{hitsTakenInMeleeZone} taken in melee zone ({distanceToPlayer:F1}m)");

                if (hitsTakenInMeleeZone >= hitsBeforeDashRetreat)
                {
                    TryPerformDashRetreat();
                }
            }
        }
    }

    private void TryPerformDashRetreat()
    {
        if (Time.time - lastDashRetreatTime < dashRetreatCooldown) return;
        if (isDashRetreating || isRetreating) return;
        if (isEnraged) return;

        PerformDashRetreat();
    }

    private void PerformDashRetreat()
    {
        if (TargetPlayer == null) return;

        StopCurrentCombo();
        ResetDashRetreat();
        
        Vector3 dashTargetPosition = CalculateDashTargetPosition();
        
        Debug.LogWarning($"[{gameObject.name}] 🏃 DASH RETREAT! After {hitsBeforeDashRetreat} hits");

        TriggerDashAnimation();
        StartDashRetreatCoroutine(dashTargetPosition);
    }

    private void StopCurrentCombo()
    {
        if (currentComboCoroutine != null)
        {
            StopCoroutine(currentComboCoroutine);
            isPerformingCombo = false;
            currentComboCoroutine = null;
            LockBossPosition(false);
        }
    }

    private void ResetDashRetreat()
    {
        hitsTakenInMeleeZone = 0;
        lastDashRetreatTime = Time.time;
    }

    private Vector3 CalculateDashTargetPosition()
    {
        Vector3 directionAwayFromPlayer = (transform.position - TargetPlayer.position).normalized;
        float randomAngle = Random.Range(-45f, 45f);
        Quaternion randomRotation = Quaternion.Euler(0f, randomAngle, 0f);
        Vector3 dashDirection = randomRotation * directionAwayFromPlayer;

        Vector3 targetPosition = transform.position + dashDirection * dashRetreatDistance;
        targetPosition.y = transform.position.y;

        return targetPosition;
    }

    private void TriggerDashAnimation()
    {
        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("Dash");
        }
    }

    private void StartDashRetreatCoroutine(Vector3 targetPosition)
    {
        if (dashRetreatCoroutine != null)
        {
            StopCoroutine(dashRetreatCoroutine);
        }
        dashRetreatCoroutine = StartCoroutine(DashRetreatSequence(targetPosition));
    }

    private IEnumerator DashRetreatSequence(Vector3 targetPosition)
    {
        isDashRetreating = true;
        float dashStartTime = Time.time;

        while (isDashRetreating && Time.time - dashStartTime < dashRetreatDuration)
        {
            if (isEnraged)
            {
                isDashRetreating = false;
                yield break;
            }

            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

            if (distanceToTarget < 0.5f)
            {
                break;
            }

            MoveTowardsTarget(targetPosition, dashRetreatSpeed);

            yield return null;
        }

        isDashRetreating = false;
        dashRetreatCoroutine = null;
    }

    private void ResetHitCounter()
    {
        if (hitsTakenInMeleeZone > 0)
        {
            hitsTakenInMeleeZone = 0;
        }
    }

    #endregion

    #region Enrage System

    private void CheckEnrageCondition()
    {
        if (hasEnteredEnragedMode) return;

        if (HealthPercent <= enrageHealthThreshold)
        {
            EnterEnragedMode();
        }
    }

    private void EnterEnragedMode()
    {
        if (hasEnteredEnragedMode) return;

        hasEnteredEnragedMode = true;
        isEnraged = true;

        StopAllBossActions();

        Debug.LogWarning($"[{gameObject.name}] ⚠ ENRAGED MODE ACTIVATED! Health at {HealthPercent * 100:F1}% ⚠");

        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("Enrage");
        }

        OnEnragedModeEntered();
    }

    private void StopAllBossActions()
    {
        StopCurrentCombo();
        StopProjectileVolley();
        StopMovementActions();
        StopDashRetreat();
        ResetVolleyCounter();
        ResetHitCounter();
    }

    private void StopProjectileVolley()
    {
        if (projectileVolleyCoroutine != null)
        {
            StopCoroutine(projectileVolleyCoroutine);
            isFiringProjectileVolley = false;
            projectileVolleyCoroutine = null;
            LockBossPosition(false);
        }
    }

    private void StopMovementActions()
    {
        isRetreating = false;
        isAdvancingAfterVolley = false;
    }

    private void StopDashRetreat()
    {
        if (dashRetreatCoroutine != null)
        {
            StopCoroutine(dashRetreatCoroutine);
            isDashRetreating = false;
            dashRetreatCoroutine = null;
        }
    }

    protected virtual void OnEnragedModeEntered()
    {
    }

    #endregion

    #region Boss State Machine

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
                break;
        }
    }

    private void HandleSpawningState()
    {
        spawnTimer += Time.deltaTime;

        if (DetectPlayerInRadius(DetectionRadius))
        {
            TransitionToFightingState();
            return;
        }

        if (spawnTimer >= spawnDetectionDelay && !isAdvancingToPlayer)
        {
            InitiateAdvanceTowardsPlayer();
        }
    }

    private void HandleAdvancingState()
    {
        if (playerTransform == null)
        {
            TryFindPlayer();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= DetectionRadius)
        {
            TransitionToFightingState();
            return;
        }

        if (!isEnraged && distanceToPlayer <= advanceStopDistance)
        {
            StopAdvancing();
            return;
        }

        float currentSpeed = isEnraged ? enrageChargeSpeed : advanceSpeed;
        MoveTowardsTarget(playerTransform.position, currentSpeed);
        RotateTowardsTarget(playerTransform.position);
    }

    private void HandleFightingState()
    {
        if (TargetPlayer == null)
        {
            TransitionToSpawningState();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, TargetPlayer.position);

        if (!isRetreating && !isDashRetreating)
        {
            RotateTowardsTarget(TargetPlayer.position);
        }

        if (isDashRetreating) return;

        if (isRetreating && !isEnraged)
        {
            HandleRetreatMovement();
            return;
        }

        if (isAdvancingAfterVolley && !isEnraged)
        {
            HandlePostVolleyAdvance(distanceToPlayer);
            return;
        }

        if (isEnraged && (isRetreating || isAdvancingAfterVolley))
        {
            isRetreating = false;
            isAdvancingAfterVolley = false;
        }

        if (isPerformingCombo || isFiringProjectileVolley) return;

        if (isEnraged)
        {
            HandleEnragedBehavior(distanceToPlayer);
            return;
        }

        DetermineCombatBehavior(distanceToPlayer);
    }

    private void DetermineCombatBehavior(float distanceToPlayer)
    {
        if (distanceToPlayer < projectileMinRange)
        {                                                                               
            if (distanceToPlayer <= meleeAttackRange)
            {
                TryPerformMeleeAttack();
            }
            else
            {
                MoveTowardsTarget(TargetPlayer.position, advanceSpeed);
            }
        }
        else if (distanceToPlayer >= projectileMinRange && distanceToPlayer <= projectileMaxRange)
        {
            ResetHitCounter();
            HandleProjectileCombatBehavior(distanceToPlayer);
        }
        else if (distanceToPlayer > projectileMaxRange && distanceToPlayer <= DetectionRadius)
        {
            ResetHitCounter();
            MoveTowardsTarget(TargetPlayer.position, advanceSpeed);
        }
        else if (distanceToPlayer > DetectionRadius)
        {
            TransitionToSpawningState();
        }
    }

    private void HandleEnragedBehavior(float distanceToPlayer)
    {
        if (distanceToPlayer > meleeAttackRange)
        {
            MoveTowardsTarget(TargetPlayer.position, enrageChargeSpeed);
        }
        else
        {
            TryPerformEnragedMeleeAttack();
        }
    }

    private void HandlePostVolleyAdvance(float distanceToPlayer)
    {
        if (distanceToPlayer <= meleeAttackRange || distanceToPlayer < projectileMinRange || distanceToPlayer <= advanceStopDistance)
        {
            isAdvancingAfterVolley = false;
            return;
        }

        MoveTowardsTarget(TargetPlayer.position, advanceSpeed);
    }

    #endregion

    #region Combat AI Brain

    private void HandleProjectileCombatBehavior(float distanceToPlayer)
    {
        if (mustAdvanceAfterVolley)
        {
            ForceAdvanceAfterVolleyLimit();
            return;
        }

        if (Time.time - lastRangedAttackTime >= rangedAttackCooldown)
        {
            StartProjectileVolley();
        }
    }

    private void DecidePostVolleyTactics()
    {
        if (isEnraged)
        {
            StartAdvancingTowardsPlayer();
            return;
        }

        IncrementVolleyCounter();

        if (consecutiveVolleyCount >= currentVolleyLimit)
        {
            mustAdvanceAfterVolley = true;
            ForceAdvanceAfterVolleyLimit();
            return;
        }

        float currentHealthPercent = HealthPercent;

        if (currentHealthPercent > healthThresholdForRetreat)
        {
            if (Random.value <= 0.5f)
            {
                StartPostVolleyAdvance();
            }
        }
        else
        {
            if (Random.value > 0.5f)
            {
                StartPostVolleyAdvance();
            }
            else
            {
                StartRetreating();
            }
        }
    }

    private void ForceAdvanceAfterVolleyLimit()
    {
        StartPostVolleyAdvance();
        StartCoroutine(ResetVolleyCounterDelayed());
    }

    private void IncrementVolleyCounter()
    {
        consecutiveVolleyCount++;
    }

    private void ResetVolleyCounter()
    {
        consecutiveVolleyCount = 0;
        currentVolleyLimit = Random.Range(minVolleyLimit, maxVolleyLimit + 1);
        mustAdvanceAfterVolley = false;
    }

    private IEnumerator ResetVolleyCounterDelayed()
    {
        yield return new WaitForSeconds(volleyLimitResetDelay);
        ResetVolleyCounter();
    }

    private void StartAdvancingTowardsPlayer()
    {
        if (TargetPlayer == null) return;

        isRetreating = false;
        isAdvancingAfterVolley = false;
    }

    private void StartPostVolleyAdvance()
    {
        if (TargetPlayer == null) return;

        isRetreating = false;
        isAdvancingAfterVolley = true;
    }

    private void StartRetreating()
    {
        if (TargetPlayer == null || isEnraged) return;

        Vector3 directionAwayFromPlayer = (transform.position - TargetPlayer.position).normalized;
        retreatTargetPosition = transform.position + directionAwayFromPlayer * retreatDistance;
        retreatTargetPosition.y = transform.position.y;

        isRetreating = true;
        isAdvancingAfterVolley = false;

        StartCoroutine(RetreatSequence());
    }

    private IEnumerator RetreatSequence()
    {
        float retreatStartTime = Time.time;
        float maxRetreatTime = 3f;

        while (isRetreating && Time.time - retreatStartTime < maxRetreatTime)
        {
            if (isEnraged || TargetPlayer == null)
            {
                isRetreating = false;
                yield break;
            }

            float distanceToRetreatTarget = Vector3.Distance(transform.position, retreatTargetPosition);

            if (distanceToRetreatTarget < 1f)
            {
                break;
            }

            MoveTowardsTarget(retreatTargetPosition, retreatSpeed);
            RotateTowardsTarget(TargetPlayer.position);

            yield return null;
        }

        isRetreating = false;
    }

    private void HandleRetreatMovement()
    {
    }

    #endregion

    #region State Transitions

    private void TransitionToSpawningState()
    {
        currentBossState = BossState.Spawning;
        SetTarget(null);
        playerTransform = null;
        
        if (!isEnraged)
        {
            isRetreating = false;
            isAdvancingAfterVolley = false;
        }

        ResetVolleyCounter();
        ResetHitCounter();
    }

    private void TransitionToFightingState()
    {
        currentBossState = BossState.Fighting;
        isAdvancingToPlayer = false;
        
        if (playerTransform != null)
        {
            SetTarget(playerTransform);
        }
    }

    private void InitiateAdvanceTowardsPlayer()
    {
        if (TryFindPlayer())
        {
            currentBossState = BossState.Advancing;
            isAdvancingToPlayer = true;
        }
    }

    private void StopAdvancing()
    {
        if (isEnraged) return;

        isAdvancingToPlayer = false;
        currentBossState = BossState.Spawning;
    }

    #endregion

    #region Detection System

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

    private bool TryFindPlayer()
    {
        PlayerCharacter player = FindFirstObjectByType<PlayerCharacter>();
        if (player != null)
        {
            playerTransform = player.transform;
            return true;
        }

        return false;
    }

    private bool IsPlayerCharacter(Collider col)
    {
        return col.GetComponent<PlayerCharacter>() != null;
    }

    #endregion

    #region Movement System

    private void MoveTowardsTarget(Vector3 targetPosition, float speed)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f;

        desiredMovementDirection = direction;
        desiredMovementSpeed = speed;
    }

    private void ApplyPhysicsMovement()
    {
        if (desiredMovementDirection == Vector3.zero || desiredMovementSpeed == 0f)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        Vector3 targetVelocity = desiredMovementDirection * desiredMovementSpeed;
        targetVelocity.y = rb.linearVelocity.y;

        rb.linearVelocity = targetVelocity;

        desiredMovementDirection = Vector3.zero;
        desiredMovementSpeed = 0f;
    }

    private void RotateTowardsTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
        }
    }

    #endregion

    #region Melee Attack System

    private void TryPerformMeleeAttack()
    {
        if (Time.time - lastMeleeAttackTime < meleeAttackCooldown) return;
        if (isPerformingCombo) return;

        isAdvancingAfterVolley = false;

        MeleeCombo combo = GenerateRandomMeleeCombo();
        ExecuteMeleeCombo(combo, false);

        lastMeleeAttackTime = Time.time;
    }

    private void TryPerformEnragedMeleeAttack()
    {
        if (Time.time - lastMeleeAttackTime < enragedMeleeCooldown) return;
        if (isPerformingCombo) return;

        MeleeCombo combo = GenerateRandomMeleeCombo();
        ExecuteMeleeCombo(combo, true);

        lastMeleeAttackTime = Time.time;
    }

    private MeleeCombo GenerateRandomMeleeCombo()
    {
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

    private void ExecuteMeleeCombo(MeleeCombo combo, bool isEnragedAttack)
    {
        if (currentComboCoroutine != null)
        {
            StopCoroutine(currentComboCoroutine);
        }

        currentComboCoroutine = StartCoroutine(PerformMeleeComboSequence(combo, isEnragedAttack));
    }

    private IEnumerator PerformMeleeComboSequence(MeleeCombo combo, bool isEnragedAttack)
    {
        isPerformingCombo = true;
        currentAttackState = BossAttackState.MeleeAttacking;
        
        LockBossPosition(true);

        string modeText = isEnragedAttack ? "ENRAGED" : "NORMAL";
        Debug.Log($"[{gameObject.name}] Starting {modeText} melee combo with {combo.AttackCount} attacks");

        foreach (ArmSlamType slamType in combo.Attacks)
        {
            yield return StartCoroutine(PerformArmSlam(slamType, isEnragedAttack));
            
            float comboDelay = isEnragedAttack ? 0.15f : 0.3f;
            yield return new WaitForSeconds(comboDelay);
        }

        isPerformingCombo = false;
        currentAttackState = BossAttackState.Idle;
        currentComboCoroutine = null;
        
        LockBossPosition(false);
    }

    private IEnumerator PerformArmSlam(ArmSlamType slamType, bool isEnragedAttack)
    {
        TriggerArmSlamAnimation(slamType);

        float animationWait = isEnragedAttack ? 0.3f : 0.5f;
        yield return new WaitForSeconds(animationWait);

        if (TargetPlayer != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, TargetPlayer.position);
            if (distanceToPlayer <= meleeAttackRange)
            {
                DealMeleeDamageToPlayer(slamType, isEnragedAttack);
            }
        }

        float recoveryTime = isEnragedAttack ? 0.15f : 0.3f;
        yield return new WaitForSeconds(recoveryTime);
    }

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
    }

    private void DealMeleeDamageToPlayer(ArmSlamType slamType, bool isEnragedAttack)
    {
        if (TargetPlayer == null) return;

        BaseCharacter targetCharacter = TargetPlayer.GetComponent<BaseCharacter>();
        if (targetCharacter != null)
        {
            float damageMultiplier = slamType == ArmSlamType.BothArms ? 1.5f : 1f;
            
            if (isEnragedAttack)
            {
                damageMultiplier *= enragedDamageMultiplier;
            }

            float totalDamage = armSlamDamage * damageMultiplier;
            targetCharacter.TakeDamage(totalDamage);
        }
    }

    #endregion

    #region Ranged Attack System

    private void StartProjectileVolley()
    {
        if (isEnraged || isFiringProjectileVolley) return;

        if (trackingProjectilePrefab == null)
        {
            Debug.LogError($"[{gameObject.name}] Cannot perform ranged attack: projectile prefab is null!");
            return;
        }

        isAdvancingAfterVolley = false;

        if (projectileVolleyCoroutine != null)
        {
            StopCoroutine(projectileVolleyCoroutine);
        }

        projectileVolleyCoroutine = StartCoroutine(PerformProjectileVolley());
    }

    private IEnumerator PerformProjectileVolley()
    {
        isFiringProjectileVolley = true;
        currentAttackState = BossAttackState.RangedAttacking;
        
        LockBossPosition(true);

        int projectileCount = Random.Range(1, maxProjectilesPerVolley + 1);

        for (int i = 0; i < projectileCount; i++)
        {
            if (isEnraged)
            {
                break;
            }

            FireSingleProjectile();

            if (i < projectileCount - 1)
            {
                yield return new WaitForSeconds(projectileVolleyDelay);
            }
        }

        lastRangedAttackTime = Time.time;
        isFiringProjectileVolley = false;
        currentAttackState = BossAttackState.Idle;
        projectileVolleyCoroutine = null;
        
        LockBossPosition(false);

        DecidePostVolleyTactics();
    }

    private void FireSingleProjectile()
    {
        if (TargetPlayer == null) return;

        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("RangedAttack");
        }

        Vector3 spawnPosition = projectileSpawnPoint.position;
        Quaternion spawnRotation = Quaternion.LookRotation(TargetPlayer.position - spawnPosition);

        GameObject projectileObj = Instantiate(trackingProjectilePrefab, spawnPosition, spawnRotation);

        TreeBossProjectile trackingProjectile = projectileObj.GetComponent<TreeBossProjectile>();
        if (trackingProjectile != null)
        {
            trackingProjectile.Initialize(projectileDamage, this, projectileSpeed, TargetPlayer);
        }
        else
        {
            ProjectileBase baseProjectile = projectileObj.GetComponent<ProjectileBase>();
            if (baseProjectile != null)
            {
                baseProjectile.Initialize(projectileDamage, this, projectileSpeed);
            }
        }
    }

    #endregion

    #region Override Methods

    protected override void OnDeath()
    {
        base.OnDeath();
        currentBossState = BossState.BossDefeated;
        
        StopCurrentCombo();
        StopProjectileVolley();
        StopMovementActions();
        StopDashRetreat();

        //change the terrain from sand to grass
        if (terrainSwap != null){
            terrainSwap.SetCheckpointReached(true);
        }

        //spawn boss's ghost NPC for dialogue
        if (!ghostSpawned) {
            ghostSpawned = true;
            Instantiate(PlantGhostPrefab, gameObject.transform.position, gameObject.transform.rotation);
        }

        //disintegration animation
         disintegrate.TriggerDisintegration();
    }

    protected override void OnStateChanged(EnemyState newState)
    {
        base.OnStateChanged(newState);
    }

    #endregion

    #region Debug Gizmos

    private void OnDrawGizmosSelected()
    {
        if (!showBossGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, DetectionRadius);

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, projectileMaxRange);

        Gizmos.color = new Color(0.5f, 0.8f, 1f);
        Gizmos.DrawWireSphere(transform.position, projectileMinRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, advanceStopDistance);

        Gizmos.color = isEnraged ? new Color(1f, 0f, 0f, 1f) : Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRange);

        if (Application.isPlaying && !isEnraged)
        {
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, dashRetreatDistance);
        }

        if (!isEnraged)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, retreatDistance);
        }

        if (Application.isPlaying && TargetPlayer != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, TargetPlayer.position);
            
            if (isEnraged)
            {
                Gizmos.color = Color.red;
            }
            else if (distanceToPlayer < projectileMinRange)
            {
                Gizmos.color = Color.green;
            }
            else if (distanceToPlayer < projectileMaxRange)
            {
                Gizmos.color = Color.magenta;
            }
            else
            {
                Gizmos.color = Color.gray;
            }
            
            Gizmos.DrawLine(transform.position, TargetPlayer.position);

            if (isRetreating && !isEnraged)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(retreatTargetPosition, 1f);
                Gizmos.DrawLine(transform.position, retreatTargetPosition);
            }

            if (isAdvancingAfterVolley)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, TargetPlayer.position + Vector3.up * 0.5f);
            }

            if (isDashRetreating)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position + Vector3.up * 1f, TargetPlayer.position + Vector3.up * 1f);
            }

            if (consecutiveVolleyCount > 0)
            {
                Gizmos.color = mustAdvanceAfterVolley ? Color.red : Color.yellow;
                Vector3 volleyIndicatorPos = transform.position + Vector3.up * 5f;
                float indicatorSize = 0.3f * consecutiveVolleyCount;
                Gizmos.DrawWireSphere(volleyIndicatorPos, indicatorSize);
            }

            if (hitsTakenInMeleeZone > 0 && distanceToPlayer < projectileMinRange)
            {
                Gizmos.color = hitsTakenInMeleeZone >= hitsBeforeDashRetreat ? Color.red : new Color(1f, 0.5f, 0f);
                Vector3 hitCounterPos = transform.position + Vector3.up * 6f;
                float hitIndicatorSize = 0.2f * hitsTakenInMeleeZone;
                Gizmos.DrawWireSphere(hitCounterPos, hitIndicatorSize);
            }
        }

        if (Application.isPlaying)
        {
            Color healthColor = Color.Lerp(Color.red, Color.green, HealthPercent);
            
            if (isEnraged && Time.time % 0.5f < 0.25f)
            {
                healthColor = Color.red;
            }

            Gizmos.color = healthColor;
            Vector3 healthBarStart = transform.position + Vector3.up * 4f;
            Vector3 healthBarEnd = healthBarStart + Vector3.right * 2f * HealthPercent;
            Gizmos.DrawLine(healthBarStart, healthBarEnd);

            Gizmos.color = Color.yellow;
            Vector3 enrageThresholdPos = healthBarStart + Vector3.right * 2f * enrageHealthThreshold;
            Gizmos.DrawWireSphere(enrageThresholdPos, 0.1f);

            Gizmos.color = Color.cyan;
            Vector3 retreatThresholdPos = healthBarStart + Vector3.right * 2f * healthThresholdForRetreat;
            Gizmos.DrawWireSphere(retreatThresholdPos, 0.1f);
        }
    }

    #endregion
}

#region Boss Enums and Data Structures

public enum BossState
{
    Spawning,
    Advancing,
    Fighting,
    BossDefeated
}

public enum BossAttackState
{
    Idle,
    MeleeAttacking,
    RangedAttacking
}

public enum ArmSlamType
{
    RightArm,
    LeftArm,
    BothArms
}

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