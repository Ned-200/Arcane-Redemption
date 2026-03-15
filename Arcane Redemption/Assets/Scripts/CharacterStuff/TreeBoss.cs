using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TreeBoss : EnemyCharacter
{
    #region Serialized Fields

    [Header("Boss Detection Settings")]
    [SerializeField] private float bossDetectionRadius = 30f;
    [SerializeField] private float spawnDetectionDelay = 4f;
    [SerializeField] private float advanceStopDistance = 7f;
    [SerializeField] private float advanceSpeed = 2f;

    [Header("Boss Attack Ranges")]
    [SerializeField] private float meleeAttackRange = 5f;
    [SerializeField] private float projectileMinRange = 16f;
    [SerializeField] private float projectileMaxRange = 25f;

    [Header("Boss Attack Damage")]
    [SerializeField] private float vineRingDamage = 30f;
    [SerializeField] private float projectileDamage = 15f;

    [Header("Boss Attack Cooldowns")]
    [SerializeField] private float meleeAttackCooldown = 2f;
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
    [SerializeField] private float enragedMeleeCooldown = 1.2f;
    [SerializeField] private float enragedDamageMultiplier = 1.3f;

    [Header("Vine Ring Attack System")]
    [SerializeField] private GameObject vineRingPrefab;
    [SerializeField] private float vineRingSpawnDelay = 0.3f;
    [SerializeField] private float vineRingDamageDelay = 0.5f;
    [SerializeField] private float vineRingLifetime = 2f;

    [Header("Physics Settings")]
    [SerializeField] private float bossMass = 500f;
    [SerializeField] private float linearDamping = 5f;
    [SerializeField] private float angularDamping = 10f;
    [SerializeField] private bool lockPositionDuringAttacks = true;

    [Header("Animation")]
    [SerializeField] private Animator bossAnimator;

    [Header("Boss UI")]
    [SerializeField] private string bossDisplayName = "Ancient Tree Guardian";

    [Header("Boss Debug")]
    [SerializeField] private bool showBossGizmos = true;

    [Header("Change Terrain")]
    [SerializeField] private TerrainSwap terrainSwap;

    [Header("Boss Ghost NPC")]
    [SerializeField] private GameObject PlantGhostPrefab;
    private bool ghostSpawned = false;

    [Header("Mayor NPC")]
    [SerializeField] private GameObject MayorNPC;

    [Header("Eye Colour")]
    [SerializeField] private Renderer eyeRenderer;
    [SerializeField] private Renderer normalEye;
    [SerializeField] private Material enragedEyeMaterial;



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

    private bool isPerformingMeleeAttack;
    private Coroutine meleeAttackCoroutine;

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

    private BossHealthBarUI healthBarUI;

    #endregion

    #region Properties

    public BossState CurrentBossState => currentBossState;
    public BossAttackState CurrentAttackState => currentAttackState;
    public bool IsPerformingMeleeAttack => isPerformingMeleeAttack;
    public bool IsFiringProjectileVolley => isFiringProjectileVolley;
    public bool IsRetreating => isRetreating;
    public bool IsEnraged => isEnraged;
    public bool IsAdvancingAfterVolley => isAdvancingAfterVolley;
    public int ConsecutiveVolleyCount => consecutiveVolleyCount;
    public int CurrentVolleyLimit => currentVolleyLimit;
    public int HitsTakenInMeleeZone => hitsTakenInMeleeZone;

    public new float DetectionRadius => bossDetectionRadius;

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

    private void ChangeEyeColourToRed()
{
    Renderer eyeRenderer = normalEye.GetComponent<Renderer>();
    eyeRenderer.material = enragedEyeMaterial;
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
        InitializeHealthBarUI();
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
        isPerformingMeleeAttack = false;
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
        
        Debug.Log($"[{gameObject.name}] Rigidbody configured - Mass: {rb.mass}, Detection Radius: {bossDetectionRadius}");
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

        if (vineRingPrefab == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Vine ring prefab not assigned! Vine ring attack will not display.");
        }

        Debug.Log($"[{gameObject.name}] TreeBoss initialized - Detection: {bossDetectionRadius}m, Melee: {meleeAttackRange}m, Projectile: {projectileMinRange}-{projectileMaxRange}m");
    }

    private void InitializeHealthBarUI()
    {
        healthBarUI = FindFirstObjectByType<BossHealthBarUI>();
        
        if (healthBarUI == null)
        {
            Debug.LogWarning($"[{gameObject.name}] BossHealthBarUI not found in scene! Boss health bar will not display.");
        }
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

        if (currentBossState == BossState.Spawning)
        {
            Debug.Log($"[{gameObject.name}] Boss took damage while spawning - activating!");
            if (TryFindPlayer())
            {
                TransitionToFightingState();
            }
        }

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

        StopMeleeAttack();
        ResetDashRetreat();
        
        Vector3 dashTargetPosition = CalculateDashTargetPosition();
        
        Debug.LogWarning($"[{gameObject.name}] 🏃 DASH RETREAT! After {hitsBeforeDashRetreat} hits");

        TriggerDashAnimation();
        StartDashRetreatCoroutine(dashTargetPosition);
    }

    private void StopMeleeAttack()
    {
        if (meleeAttackCoroutine != null)
        {
            StopCoroutine(meleeAttackCoroutine);
            isPerformingMeleeAttack = false;
            meleeAttackCoroutine = null;
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

                //change eye colour to red

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
            ChangeEyeColourToRed();
        }

        if (currentBossState == BossState.Spawning && TryFindPlayer())
        {
            TransitionToFightingState();
        }

        OnEnragedModeEntered();
    }

    private void StopAllBossActions()
    {
        StopMeleeAttack();
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
        if (DetectPlayerInRadius(bossDetectionRadius))
        {
            Debug.Log($"[{gameObject.name}] Player entered detection range ({bossDetectionRadius}m) - activating boss!");
            TransitionToFightingState();
            return;
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

        if (distanceToPlayer <= bossDetectionRadius)
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

        if (isPerformingMeleeAttack || isFiringProjectileVolley) return;

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
                TryPerformVineRingAttack();
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
        else if (distanceToPlayer > projectileMaxRange && distanceToPlayer <= bossDetectionRadius)
        {
            ResetHitCounter();
            MoveTowardsTarget(TargetPlayer.position, advanceSpeed);
        }
        else if (distanceToPlayer > bossDetectionRadius)
        {
            Debug.Log($"[{gameObject.name}] Player escaped detection range ({distanceToPlayer:F1}m > {bossDetectionRadius}m)");
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
            TryPerformEnragedVineRingAttack();
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
        
        if (healthBarUI != null)
        {
            healthBarUI.HideBossHealthBar();
        }
        
        Debug.Log($"[{gameObject.name}] Returned to Spawning state (waiting for player)");
    }

    private void TransitionToFightingState()
    {
        currentBossState = BossState.Fighting;
        isAdvancingToPlayer = false;
        
        if (playerTransform != null)
        {
            SetTarget(playerTransform);
        }
        
        if (healthBarUI != null)
        {
            healthBarUI.ShowBossHealthBar(this, bossDisplayName);
        }
        
        Debug.Log($"[{gameObject.name}] ⚔️ Transitioned to Fighting state! Target: {TargetPlayer?.name ?? "NULL"}");
    }

    private void InitiateAdvanceTowardsPlayer()
    {
        if (TryFindPlayer())
        {
            currentBossState = BossState.Advancing;
            isAdvancingToPlayer = true;
            Debug.Log($"[{gameObject.name}] Started advancing towards player");
        }
    }

    private void StopAdvancing()
    {
        if (isEnraged) return;

        isAdvancingToPlayer = false;
        currentBossState = BossState.Spawning;
        Debug.Log($"[{gameObject.name}] Stopped advancing (player within stop distance)");
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

    #region Vine Ring Attack System

    private void TryPerformVineRingAttack()
    {
        if (Time.time - lastMeleeAttackTime < meleeAttackCooldown) return;
        if (isPerformingMeleeAttack) return;

        isAdvancingAfterVolley = false;

        ExecuteVineRingAttack(false);

        lastMeleeAttackTime = Time.time;
    }

    private void TryPerformEnragedVineRingAttack()
    {
        if (Time.time - lastMeleeAttackTime < enragedMeleeCooldown) return;
        if (isPerformingMeleeAttack) return;

        ExecuteVineRingAttack(true);

        lastMeleeAttackTime = Time.time;
    }

    private void ExecuteVineRingAttack(bool isEnragedAttack)
    {
        if (meleeAttackCoroutine != null)
        {
            StopCoroutine(meleeAttackCoroutine);
        }

        meleeAttackCoroutine = StartCoroutine(PerformVineRingAttackSequence(isEnragedAttack));
    }

    private IEnumerator PerformVineRingAttackSequence(bool isEnragedAttack)
    {
        isPerformingMeleeAttack = true;
        currentAttackState = BossAttackState.MeleeAttacking;
        
        // Remove position locking for melee attacks
        // LockBossPosition(true);

        string modeText = isEnragedAttack ? "ENRAGED" : "NORMAL";
        Debug.Log($"[{gameObject.name}] 🌿 Performing {modeText} Vine Ring Attack!");

        TriggerVineRingAnimation();

        yield return new WaitForSeconds(vineRingSpawnDelay);

        SpawnVineRing();

        yield return new WaitForSeconds(vineRingDamageDelay);

        DealVineRingDamage(isEnragedAttack);

        float recoveryTime = isEnragedAttack ? 0.3f : 0.5f;
        yield return new WaitForSeconds(recoveryTime);

        isPerformingMeleeAttack = false;
        currentAttackState = BossAttackState.Idle;
        meleeAttackCoroutine = null;
        
        // No need to unlock since we never locked
        // LockBossPosition(false);
    }

    private void TriggerVineRingAnimation()
    {
        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("VineRingAttack");
        }
    }

    private void SpawnVineRing()
    {
        if (vineRingPrefab == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Vine ring prefab not assigned!");
            return;
        }

        Vector3 spawnPosition = transform.position;
        
        // Raycast downward from boss to find ground
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out hit, 20f))
        {
            // Spawn at exact ground point beneath boss (centered on XZ, grounded on Y)
            spawnPosition = new Vector3(transform.position.x, hit.point.y, transform.position.z);
            Debug.Log($"[{gameObject.name}] Ground found at Y={hit.point.y}");
        }
        else
        {
            // Fallback: use Y=0 if no ground detected
            spawnPosition.y = 0f;
            Debug.LogWarning($"[{gameObject.name}] No ground found beneath boss! Using Y=0 fallback.");
        }

        GameObject vineRingInstance = Instantiate(vineRingPrefab, spawnPosition, Quaternion.identity);

        Destroy(vineRingInstance, vineRingLifetime);

        Debug.Log($"[{gameObject.name}] 🌿 Spawned vine ring centered at {spawnPosition}");
    }

    private void DealVineRingDamage(bool isEnragedAttack)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, meleeAttackRange);

        foreach (Collider hit in hits)
        {
            PlayerCharacter player = hit.GetComponent<PlayerCharacter>();
            if (player != null)
            {
                BaseCharacter targetCharacter = player.GetComponent<BaseCharacter>();
                if (targetCharacter != null)
                {
                    float damageMultiplier = isEnragedAttack ? enragedDamageMultiplier : 1f;
                    float totalDamage = vineRingDamage * damageMultiplier;
                    
                    targetCharacter.TakeDamage(totalDamage);
                    
                    Debug.Log($"[{gameObject.name}] 💥 Vine Ring hit {player.name} for {totalDamage} damage!");
                }
            }
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
        
        StopMeleeAttack();
        StopProjectileVolley();
        StopMovementActions();
        StopDashRetreat();

        if (healthBarUI != null)
        {
            healthBarUI.HideBossHealthBar();
        }

        if (terrainSwap != null)
        {
            terrainSwap.SetCheckpointReached(true);
        }

        if (!ghostSpawned)
        {
            ghostSpawned = true;
            if (PlantGhostPrefab != null)
            {
                Instantiate(PlantGhostPrefab, gameObject.transform.position, gameObject.transform.rotation);
            }
            else
            {
                Debug.LogError("TreeBoss not assigned a Ghost NPC Prefab! Check Fields!");
            }
            
            if (MayorNPC != null)
            {
                MayorNPC.SetActive(true);
            }
            else
            {
                Debug.LogError("TreeBoss not assigned MayorNPC! Check Fields!");
            }
        }

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
        Gizmos.DrawWireSphere(transform.position, bossDetectionRadius);

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

#endregion