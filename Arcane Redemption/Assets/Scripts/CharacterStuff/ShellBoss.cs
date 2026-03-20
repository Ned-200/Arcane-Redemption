using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShellBoss : EnemyCharacter
{
    #region Serialized Fields

    [Header("Boss Phases")]
    [SerializeField] private string bossDisplayName = "Armored Sentinel";

    [Header("Shell System")]
    [SerializeField] private ShellProtection shellProtection;

    [Header("Phase 1: Shell Phase")]
    [SerializeField] private float phase1MoveSpeed = 3f;
    [SerializeField] private float rockPositionCheckRadius = 2f;
    [SerializeField] private float timeUnderRock = 3f;
    [SerializeField] private float rockCycleDelay = 1f;

    [Header("Phase 2: Enraged Phase")]
    [SerializeField] private float phase2MoveSpeedMultiplier = 1.5f;
    [SerializeField] private float phase2DamageMultiplier = 1.3f;
    [SerializeField] private float phase2KeepAwayDistance = 10f;
    [SerializeField] private float phase2BackupSpeed = 4f;
    [SerializeField] private float ringAttackOnBackupChance = 0.5f;

    [Header("Ring Attack Settings")]
    [SerializeField] private float ringAttackRadius = 4f;
    [SerializeField] private float ringAttackDamage = 30f;
    [SerializeField] private float ringAttackTriggerRange = 10f;
    [SerializeField] private int minComboAttacks = 1;
    [SerializeField] private int maxComboAttacks = 3;

    [Header("Phase 1 Attack Cooldowns")]
    [SerializeField] private float phase1ComboAttackCooldownMin = 1f;
    [SerializeField] private float phase1ComboAttackCooldownMax = 3f;
    [SerializeField] private float phase1ComboCooldownMin = 1f;
    [SerializeField] private float phase1ComboCooldownMax = 3f;

    [Header("Phase 2 Attack Cooldowns")]
    [SerializeField] private float phase2ComboAttackCooldownMin = 0.5f;
    [SerializeField] private float phase2ComboAttackCooldownMax = 1.5f;
    [SerializeField] private float phase2ComboCooldownMin = 0.5f;
    [SerializeField] private float phase2ComboCooldownMax = 1.5f;

    [Header("Projectile Volley Attack (Phase 2)")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float projectileVolleyCooldown = 6f;
    [SerializeField] private int minProjectilesPerVolley = 3;
    [SerializeField] private int maxProjectilesPerVolley = 6;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float projectileDamage = 15f;
    [SerializeField] private float projectileTrackingDuration = 4f;
    [SerializeField] private float projectileSpawnDelay = 0.2f;
    [SerializeField] private float volleySpreadAngle = 30f;

    [Header("Rock Spawn System")]
    [SerializeField] private RockSpawnPoint[] rockSpawnPoints;

    [Header("Animation")]
    [SerializeField] private Animator bossAnimator;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    [Header("Arena Boundaries")]
    [SerializeField] private ArenaBounds arenaBounds;
    [SerializeField] private float edgeAvoidanceStrength = 3f;

    #endregion

    #region Private Fields

    private BossPhase currentPhase = BossPhase.ShellPhase;
    private ShellBossState currentState = ShellBossState.MovingToRock;

    private Transform playerTransform;
    private float lastComboTime;
    private bool isPerformingCombo;
    private Coroutine comboCoroutine;
    private Coroutine repositionCoroutine;

    private RockSpawnPoint targetRockSpawnPoint;
    private bool isMovingToRock;
    private bool isBackingUp;
    
    private float timeArrivedAtRock;
    private int currentRockIndex;

    private BossHealthBarUI healthBarUI;

    private float lastProjectileVolleyTime;
    private bool isFiringVolley;

    #endregion

    #region Properties

    public BossPhase CurrentPhase => currentPhase;
    public ShellBossState CurrentState => currentState;
    public bool IsShellActive => shellProtection != null && shellProtection.IsShellActive;
    public bool IsPerformingCombo => isPerformingCombo;

    private float CurrentMoveSpeed => currentPhase == BossPhase.EnragedPhase 
        ? phase1MoveSpeed * phase2MoveSpeedMultiplier 
        : phase1MoveSpeed;

    private float CurrentDamageMultiplier => currentPhase == BossPhase.EnragedPhase 
        ? phase2DamageMultiplier 
        : 1f;

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

        UpdateBossStateMachine();
    }

    #endregion

    #region Initialization

    private void InitializeBoss()
    {
        InitializeShellProtection();
        InitializeHealthBarUI();
        InitializeRockSpawnPoints();
        ValidateComponents();
        
        if (currentPhase == BossPhase.ShellPhase)
        {
            StartRockCycle();
        }

        lastProjectileVolleyTime = -projectileVolleyCooldown;
    }

    private void InitializeShellProtection()
    {
        if (shellProtection == null)
        {
            shellProtection = GetComponent<ShellProtection>();
        }

        if (shellProtection != null)
        {
            shellProtection.OnShellBroken += OnShellBroken;
            shellProtection.OnShellHit += OnShellHit;
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] ShellProtection component not found!");
        }
    }

    private void InitializeHealthBarUI()
    {
        healthBarUI = FindFirstObjectByType<BossHealthBarUI>();

        if (healthBarUI == null)
        {
            Debug.LogWarning($"[{gameObject.name}] BossHealthBarUI not found in scene!");
        }
    }

    private void InitializeRockSpawnPoints()
    {
        if (rockSpawnPoints == null || rockSpawnPoints.Length == 0)
        {
            rockSpawnPoints = FindObjectsByType<RockSpawnPoint>(FindObjectsSortMode.None);
            Debug.Log($"[{gameObject.name}] Auto-found {rockSpawnPoints.Length} rock spawn points");
        }
        
        if (rockSpawnPoints == null || rockSpawnPoints.Length == 0)
        {
            Debug.LogError($"[{gameObject.name}] No rock spawn points found! Boss cannot position under rocks.");
        }
        else
        {
            ShuffleRockSpawnPoints();
            currentRockIndex = 0;
        }
    }

    private void ShuffleRockSpawnPoints()
    {
        for (int i = rockSpawnPoints.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            RockSpawnPoint temp = rockSpawnPoints[i];
            rockSpawnPoints[i] = rockSpawnPoints[randomIndex];
            rockSpawnPoints[randomIndex] = temp;
        }
    }

    private void ValidateComponents()
    {
        if (bossAnimator == null)
        {
            bossAnimator = GetComponent<Animator>();
        }

        if (rockSpawnPoints == null || rockSpawnPoints.Length == 0)
        {
            Debug.LogError($"[{gameObject.name}] No rock spawn points found! Boss cannot position under rocks.");
        }

        if (projectilePrefab == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No projectile prefab assigned! Projectile volley attack will not work.");
        }

        if (projectileSpawnPoint == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No projectile spawn point assigned! Using boss position instead.");
        }

        // NEW: Validate or find arena bounds
        if (arenaBounds == null)
        {
            arenaBounds = FindFirstObjectByType<ArenaBounds>();
            if (arenaBounds == null)
            {
                Debug.LogWarning($"[{gameObject.name}] No ArenaBounds found! Boss may fall off platform.");
            }
        }
    }

    #endregion

    #region Shell System

    public void OnRockHit(FallingRock rock)
    {
        if (shellProtection == null) return;

        bool shellBroken = shellProtection.TryDamageShell();

        if (shellBroken)
        {
            TransitionToEnragedPhase();
        }
        else
        {
            StartCoroutine(MoveToNextRockAfterDelay(rockCycleDelay));
        }
    }

    private void OnShellHit()
    {
        Debug.Log($"[{gameObject.name}] Shell hit! {shellProtection.RemainingHits} hits remaining");

        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("ShellHit");
        }
    }

    private void OnShellBroken()
    {
        Debug.LogWarning($"[{gameObject.name}] 💥 SHELL BROKEN! Entering Phase 2!");

        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("ShellBreak");
        }
    }       

    public override void TakeDamage(float damage)
    {
        if (IsShellActive)
        {
            Debug.Log($"[{gameObject.name}] Blocked {damage} damage - shell is active! Must break shell first.");
            OnDamageBlocked(damage);
            return;
        }

        base.TakeDamage(damage);
    }

    #endregion

    #region Phase Management

    private void TransitionToEnragedPhase()
    {
        if (currentPhase == BossPhase.EnragedPhase) return;

        currentPhase = BossPhase.EnragedPhase;
        currentState = ShellBossState.Fighting;

        StopAllCoroutines();
        isMovingToRock = false;

        if (TryFindPlayer())
        {
            if (healthBarUI != null)
            {
                healthBarUI.ShowBossHealthBar(null, bossDisplayName);
            }
        }

        lastProjectileVolleyTime = Time.time;

        Debug.LogWarning($"[{gameObject.name}] ⚡ PHASE 2: ENRAGED! Speed x{phase2MoveSpeedMultiplier}, Damage x{phase2DamageMultiplier}");
    }

    #endregion

    #region Rock Cycling System

    private void StartRockCycle()
    {
        if (rockSpawnPoints == null || rockSpawnPoints.Length == 0)
        {
            Debug.LogError($"[{gameObject.name}] Cannot start rock cycle - no spawn points!");
            return;
        }

        Debug.Log($"[{gameObject.name}] Starting rock cycle with {rockSpawnPoints.Length} points");
        MoveToNextRock();
    }

    private void MoveToNextRock()
    {
        if (currentPhase != BossPhase.ShellPhase) return;

        RockSpawnPoint[] availableRocks = GetAvailableRockSpawnPoints();

        if (availableRocks.Length == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] No available rocks - waiting for reset");
            StartCoroutine(RetryRockCycle());
            return;
        }

        targetRockSpawnPoint = availableRocks[currentRockIndex % availableRocks.Length];
        currentRockIndex++;

        Vector3 targetPosition = targetRockSpawnPoint.transform.position;
        targetPosition.y = transform.position.y;

        isMovingToRock = true;
        currentState = ShellBossState.MovingToRock;

        Debug.Log($"[{gameObject.name}] Moving to rock: {targetRockSpawnPoint.name}");

        if (repositionCoroutine != null)
        {
            StopCoroutine(repositionCoroutine);
        }
        repositionCoroutine = StartCoroutine(MoveToRockPosition(targetPosition));
    }

    private IEnumerator RetryRockCycle()
    {
        yield return new WaitForSeconds(2f);
        MoveToNextRock();
    }

    private IEnumerator MoveToNextRockAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        MoveToNextRock();
    }

    private IEnumerator MoveToRockPosition(Vector3 targetPosition)
    {
        while (isMovingToRock)
        {
            if (TargetPlayer != null && !isPerformingCombo)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, TargetPlayer.position);
                TryPerformRingAttack(distanceToPlayer);
            }

            float distance = Vector3.Distance(transform.position, targetPosition);

            if (distance < rockPositionCheckRadius)
            {
                isMovingToRock = false;
                currentState = ShellBossState.WaitingUnderRock;
                timeArrivedAtRock = Time.time;
                Debug.Log($"[{gameObject.name}] ✓ Arrived at rock - waiting");
                yield break;
            }

            MoveTowards(targetPosition, CurrentMoveSpeed);
            RotateTowards(targetPosition);

            yield return null;
        }
    }

    #endregion

    #region State Machine

    private void UpdateBossStateMachine()
    {
        TryFindPlayer();

        switch (currentPhase)
        {
            case BossPhase.ShellPhase:
                UpdateShellPhase();
                break;

            case BossPhase.EnragedPhase:
                UpdateEnragedPhase();
                break;
        }
    }

    private void UpdateShellPhase()
    {
        if (!isPerformingCombo && TargetPlayer != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, TargetPlayer.position);
            TryPerformRingAttack(distanceToPlayer);
        }

        switch (currentState)
        {
            case ShellBossState.MovingToRock:
                break;

            case ShellBossState.WaitingUnderRock:
                HandleWaitingUnderRock();
                break;
        }
    }

    private void HandleWaitingUnderRock()
    {
        if (Time.time - timeArrivedAtRock >= timeUnderRock)
        {
            Debug.Log($"[{gameObject.name}] Waited {timeUnderRock}s under rock - moving to next");
            MoveToNextRock();
            return;
        }

        if (targetRockSpawnPoint == null)
        {
            Debug.Log($"[{gameObject.name}] Rock destroyed - finding new one");
            MoveToNextRock();
            return;
        }

        FallingRock rock = targetRockSpawnPoint.GetRockScript();
        if (rock == null || rock.HasBeenTriggered)
        {
            Debug.Log($"[{gameObject.name}] Rock triggered - moving to next");
            MoveToNextRock();
        }
    }

    private void UpdateEnragedPhase()
    {
        if (TargetPlayer == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, TargetPlayer.position);

        if (isPerformingCombo || isFiringVolley) return;

        if (distanceToPlayer < phase2KeepAwayDistance)
        {
            HandleBackingUp();
        }
        else
        {
            isBackingUp = false;
        }

        TryPerformRingAttack(distanceToPlayer);
        TryPerformProjectileVolley();
    }

    #endregion

    #region Phase 2: Enraged Phase Logic

    private void HandleBackingUp()
    {
        if (playerTransform == null) return;

        Vector3 directionAwayFromPlayer = (transform.position - playerTransform.position).normalized;
        
        // NEW: Blend with edge avoidance
        if (arenaBounds != null)
        {
            Vector3 edgePush = arenaBounds.GetSafePushDirection(transform.position);
            if (edgePush != Vector3.zero)
            {
                // Blend backing away with staying in bounds
                float edgeProximity = 1f - arenaBounds.GetDistanceFromEdgeNormalized(transform.position);
                directionAwayFromPlayer = Vector3.Lerp(
                    directionAwayFromPlayer,
                    edgePush,
                    edgeProximity * edgeAvoidanceStrength
                ).normalized;
            }
        }

        Vector3 backupPosition = transform.position + directionAwayFromPlayer * 2f;

        MoveTowards(backupPosition, phase2BackupSpeed);
        RotateTowardsPlayer();

        if (!isBackingUp)
        {
            isBackingUp = true;

            if (Random.value <= ringAttackOnBackupChance && !isPerformingCombo)
            {
                PerformRingAttackCombo();
            }
        }
    }

    #endregion

    #region Ring Attack System

    private void TryPerformRingAttack(float distanceToPlayer)
    {
        if (isPerformingCombo) return;
        if (distanceToPlayer > ringAttackTriggerRange) return;

        float comboCooldown = GetComboCooldown();

        if (Time.time - lastComboTime < comboCooldown) return;

        PerformRingAttackCombo();
    }

    private void PerformRingAttackCombo()
    {
        if (comboCoroutine != null)
        {
            StopCoroutine(comboCoroutine);
        }

        int comboLength = Random.Range(minComboAttacks, maxComboAttacks + 1);
        comboCoroutine = StartCoroutine(RingAttackComboSequence(comboLength));
    }

    private IEnumerator RingAttackComboSequence(int attackCount)
    {
        isPerformingCombo = true;

        string phaseText = currentPhase == BossPhase.ShellPhase ? "[SHELL PHASE]" : "[ENRAGED]";
        Debug.Log($"[{gameObject.name}] {phaseText} Starting Ring Attack Combo x{attackCount}");

        for (int i = 0; i < attackCount; i++)
        {
            ExecuteSingleRingAttack();

            if (i < attackCount - 1)
            {
                float attackDelay = GetComboAttackCooldown();
                yield return new WaitForSeconds(attackDelay);
            }
        }

        lastComboTime = Time.time;
        isPerformingCombo = false;
        comboCoroutine = null;
    }

    private void ExecuteSingleRingAttack()
    {
        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("RingAttack");
        }

        DamagePlayersInRingRadius();
    }

    private void DamagePlayersInRingRadius()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, ringAttackRadius);

        foreach (Collider hit in hits)
        {
            PlayerCharacter player = hit.GetComponent<PlayerCharacter>();
            if (player != null)
            {
                BaseCharacter baseChar = player.GetComponent<BaseCharacter>();
                if (baseChar != null)
                {
                    float finalDamage = ringAttackDamage * CurrentDamageMultiplier;
                    baseChar.TakeDamage(finalDamage);
                    Debug.Log($"[{gameObject.name}] Ring Attack hit {player.name} for {finalDamage} damage!");
                }
            }
        }
    }

    private float GetComboAttackCooldown()
    {
        if (currentPhase == BossPhase.EnragedPhase)
        {
            return Random.Range(phase2ComboAttackCooldownMin, phase2ComboAttackCooldownMax);
        }

        return Random.Range(phase1ComboAttackCooldownMin, phase1ComboAttackCooldownMax);
    }

    private float GetComboCooldown()
    {
        if (currentPhase == BossPhase.EnragedPhase)
        {
            return Random.Range(phase2ComboCooldownMin, phase2ComboCooldownMax);
        }

        return Random.Range(phase1ComboCooldownMin, phase1ComboCooldownMax);
    }

    #endregion

    #region Projectile Volley System

    private void TryPerformProjectileVolley()
    {
        if (currentPhase != BossPhase.EnragedPhase) return;
        if (isFiringVolley) return;
        if (playerTransform == null) return;
        if (projectilePrefab == null) return;

        if (Time.time - lastProjectileVolleyTime < projectileVolleyCooldown) return;

        StartCoroutine(FireProjectileVolley());
    }

    private IEnumerator FireProjectileVolley()
    {
        isFiringVolley = true;
        lastProjectileVolleyTime = Time.time;

        int projectileCount = Random.Range(minProjectilesPerVolley, maxProjectilesPerVolley + 1);

        Debug.Log($"[{gameObject.name}] 🎯 Firing projectile volley x{projectileCount}!");

        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("ProjectileVolley");
        }

        for (int i = 0; i < projectileCount; i++)
        {
            FireSingleTrackingProjectile(i, projectileCount);

            if (i < projectileCount - 1)
            {
                yield return new WaitForSeconds(projectileSpawnDelay);
            }
        }

        isFiringVolley = false;
    }

    private void FireSingleTrackingProjectile(int projectileIndex, int totalProjectiles)
    {
        if (playerTransform == null || projectilePrefab == null) return;

        Vector3 spawnPosition = projectileSpawnPoint != null 
            ? projectileSpawnPoint.position 
            : transform.position + Vector3.up * 2f;

        Vector3 directionToPlayer = (playerTransform.position - spawnPosition).normalized;

        float spreadOffset = 0f;
        if (totalProjectiles > 1)
        {
            float step = volleySpreadAngle / (totalProjectiles - 1);
            spreadOffset = -volleySpreadAngle / 2f + (step * projectileIndex);
        }

        Quaternion rotation = Quaternion.LookRotation(directionToPlayer);
        rotation *= Quaternion.Euler(0f, spreadOffset, 0f);

        GameObject projectileObj = Instantiate(projectilePrefab, spawnPosition, rotation);

        TreeBossProjectile projectile = projectileObj.GetComponent<TreeBossProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(projectileDamage, this, projectileSpeed, playerTransform);

            Debug.Log($"[{gameObject.name}] Fired tracking projectile {projectileIndex + 1}/{totalProjectiles}");
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Projectile prefab missing TreeBossProjectile component!");
            Destroy(projectileObj);
        }
    }

    #endregion

    #region Movement System

    private bool TryFindPlayer()
    {
        if (playerTransform != null) return true;

        PlayerCharacter player = FindFirstObjectByType<PlayerCharacter>();
        if (player != null)
        {
            playerTransform = player.transform;
            SetTarget(playerTransform);
            return true;
        }

        return false;
    }

    private RockSpawnPoint[] GetAvailableRockSpawnPoints()
    {
        System.Collections.Generic.List<RockSpawnPoint> available = new System.Collections.Generic.List<RockSpawnPoint>();

        foreach (RockSpawnPoint spawnPoint in rockSpawnPoints)
        {
            if (spawnPoint == null) continue;

            FallingRock rock = spawnPoint.GetRockScript();
            if (rock != null && rock.IsAvailable)
            {
                available.Add(spawnPoint);
            }
        }

        return available.ToArray();
    }

    private void MoveTowards(Vector3 targetPosition, float speed)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f;

        // Calculate new position
        Vector3 newPosition = transform.position + direction * speed * Time.deltaTime;

        // Apply arena boundary constraints
        if (arenaBounds != null)
        {
            newPosition = arenaBounds.ClampPosition(newPosition);
        }

        transform.position = newPosition;
    }

    private void RotateTowardsPlayer()
    {
        if (playerTransform == null) return;

        RotateTowards(playerTransform.position);
    }

    private void RotateTowards(Vector3 targetPosition)
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

    #region Debug Context Menu

    [ContextMenu("Force Break Shell")]
    private void ForceBreakShell()
    {
        if (shellProtection != null)
        {
            while (shellProtection.IsShellActive)
            {
                shellProtection.TryDamageShell();
            }
        }
    }

    [ContextMenu("Force Ring Attack")]
    private void ForceRingAttack()
    {
        PerformRingAttackCombo();
    }

    [ContextMenu("Force Move to Next Rock")]
    private void ForceMoveToNextRock()
    {
        MoveToNextRock();
    }

    [ContextMenu("Force Projectile Volley")]
    private void ForceProjectileVolley()
    {
        if (currentPhase == BossPhase.EnragedPhase)
        {
            StartCoroutine(FireProjectileVolley());
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] Projectile volley only available in Phase 2!");
        }
    }

    #endregion

    #region Override Methods

    protected override void OnDeath()
    {
        base.OnDeath();

        if (healthBarUI != null)
        {
            healthBarUI.HideBossHealthBar();
        }

        if (shellProtection != null)
        {
            shellProtection.OnShellBroken -= OnShellBroken;
            shellProtection.OnShellHit -= OnShellHit;
        }

        StopAllCoroutines();
    }

    #endregion

    #region Debug Gizmos

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, ringAttackRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, ringAttackTriggerRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, phase2KeepAwayDistance);

        if (Application.isPlaying && targetRockSpawnPoint != null)
        {
            Gizmos.color = currentState == ShellBossState.MovingToRock ? Color.yellow : Color.green;
            Gizmos.DrawLine(transform.position, targetRockSpawnPoint.transform.position);
            Gizmos.DrawWireSphere(targetRockSpawnPoint.transform.position, rockPositionCheckRadius);
        }

        if (Application.isPlaying && playerTransform != null)
        {
            Gizmos.color = currentPhase == BossPhase.EnragedPhase ? Color.red : Color.blue;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }

        if (projectileSpawnPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(projectileSpawnPoint.position, 0.3f);
            Gizmos.DrawLine(transform.position, projectileSpawnPoint.position);
        }
    }

    #endregion
}

#region Enums

public enum BossPhase
{
    ShellPhase
    , EnragedPhase
}

public enum ShellBossState
{
    MovingToRock,
    WaitingUnderRock,
    Fighting
}

#endregion