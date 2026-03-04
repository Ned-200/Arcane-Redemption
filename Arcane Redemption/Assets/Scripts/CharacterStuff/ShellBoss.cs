using UnityEngine;
using System.Collections;

public class ShellBoss : EnemyCharacter
{
    #region Serialized Fields

    [Header("Boss Phases")]
    [SerializeField] private string bossDisplayName = "Armored Sentinel";

    [Header("Shell System")]
    [SerializeField] private ShellProtection shellProtection;

    [Header("Phase 1: Shell Phase")]
    [SerializeField] private float phase1AdvanceDistance = 25f;
    [SerializeField] private float phase1MoveSpeed = 3f;
    [SerializeField] private float rockPositionCheckRadius = 2f;
    [SerializeField] private float rockPositioningChance = 0.7f;
    [SerializeField] private float repositionWaitTime = 4f;

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

    [Header("Rock Spawn System")]
    [SerializeField] private RockSpawnPoint[] rockSpawnPoints;

    [Header("Animation")]
    [SerializeField] private Animator bossAnimator;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    #endregion

    #region Private Fields

    private BossPhase currentPhase = BossPhase.ShellPhase;
    private ShellBossState currentState = ShellBossState.Advancing;

    private Transform playerTransform;
    private float lastComboTime;
    private bool isPerformingCombo;
    private Coroutine comboCoroutine;
    private Coroutine repositionCoroutine;

    private RockSpawnPoint targetRockSpawnPoint;
    private bool isPositioningUnderRock;
    private bool isBackingUp;

    private BossHealthBarUI healthBarUI;

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
            StartRepositionAfterRockHit();
        }
    }

    private void StartRepositionAfterRockHit()
    {
        if (currentPhase != BossPhase.ShellPhase) return;

        currentState = ShellBossState.PositioningUnderRock;
        
        if (repositionCoroutine != null)
        {
            StopCoroutine(repositionCoroutine);
        }

        repositionCoroutine = StartCoroutine(DelayedRockPositioning(1f));
    }

    private IEnumerator DelayedRockPositioning(float delay)
    {
        yield return new WaitForSeconds(delay);
        AttemptRockPositioning();
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

    protected override void OnDamageTaken(float damage)
    {
        if (IsShellActive)
        {
            Debug.Log($"[{gameObject.name}] Blocked {damage} damage - shell is active!");
            return;
        }

        base.OnDamageTaken(damage);
    }

    #endregion

    #region Phase Management

    private void TransitionToEnragedPhase()
    {
        if (currentPhase == BossPhase.EnragedPhase) return;

        currentPhase = BossPhase.EnragedPhase;
        currentState = ShellBossState.Fighting;

        StopAllCoroutines();
        isPositioningUnderRock = false;

        if (TryFindPlayer())
        {
            if (healthBarUI != null)
            {
                healthBarUI.ShowBossHealthBar(null, bossDisplayName);
            }
        }

        Debug.LogWarning($"[{gameObject.name}] ⚡ PHASE 2: ENRAGED! Speed x{phase2MoveSpeedMultiplier}, Damage x{phase2DamageMultiplier}");
    }

    #endregion

    #region State Machine

    private void UpdateBossStateMachine()
    {
        if (!TryFindPlayer()) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        switch (currentPhase)
        {
            case BossPhase.ShellPhase:
                UpdateShellPhase(distanceToPlayer);
                break;

            case BossPhase.EnragedPhase:
                UpdateEnragedPhase(distanceToPlayer);
                break;
        }
    }

    private void UpdateShellPhase(float distanceToPlayer)
    {
        // CHANGE: Always try ring attack if player is in range (even while positioning)
        if (!isPerformingCombo)
        {
            TryPerformRingAttack(distanceToPlayer);
        }

        switch (currentState)
        {
            case ShellBossState.Advancing:
                HandleAdvancing(distanceToPlayer);
                break;

            case ShellBossState.PositioningUnderRock:
                HandleRockPositioning();
                break;

            case ShellBossState.WaitingUnderRock:
                HandleWaitingUnderRock();
                break;

            case ShellBossState.WaitingForReposition:
                // Still check for ring attack even while waiting
                break;
        }
    }

    private void UpdateEnragedPhase(float distanceToPlayer)
    {
        if (isPerformingCombo) return;

        if (distanceToPlayer < phase2KeepAwayDistance)
        {
            HandleBackingUp();
        }
        else
        {
            isBackingUp = false;
        }

        TryPerformRingAttack(distanceToPlayer);
    }

    #endregion

    #region Phase 1: Shell Phase Logic

    private void HandleAdvancing(float distanceToPlayer)
    {
        if (distanceToPlayer > phase1AdvanceDistance)
        {
            MoveTowardsPlayer(CurrentMoveSpeed);
            RotateTowardsPlayer();
        }
        else
        {
            TransitionToRockPositioning();
        }
    }

    private void TransitionToRockPositioning()
    {
        currentState = ShellBossState.PositioningUnderRock;
        AttemptRockPositioning();
    }

    private void AttemptRockPositioning()
    {
        if (rockSpawnPoints == null || rockSpawnPoints.Length == 0)
        {
            Debug.LogError($"[{gameObject.name}] Cannot position under rock - no spawn points!");
            return;
        }

        RockSpawnPoint[] availablePoints = GetAvailableRockSpawnPoints();

        if (availablePoints.Length == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] No available rocks to position under - waiting");
            StartRepositionWait();
            return;
        }

        float roll = Random.value;

        if (roll <= rockPositioningChance)
        {
            PositionUnderRandomRock(availablePoints);
        }
        else
        {
            StartRepositionWait();
        }
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

    private void PositionUnderRandomRock(RockSpawnPoint[] availablePoints)
    {
        targetRockSpawnPoint = availablePoints[Random.Range(0, availablePoints.Length)];
        
        Vector3 targetPosition = targetRockSpawnPoint.transform.position;
        targetPosition.y = transform.position.y;

        isPositioningUnderRock = true;
        currentState = ShellBossState.PositioningUnderRock;

        Debug.Log($"[{gameObject.name}] Positioning under rock at {targetRockSpawnPoint.name}");

        if (repositionCoroutine != null)
        {
            StopCoroutine(repositionCoroutine);
        }
        repositionCoroutine = StartCoroutine(MoveToRockPosition(targetPosition));
    }

    private IEnumerator MoveToRockPosition(Vector3 targetPosition)
    {
        while (isPositioningUnderRock)
        {
            // CHANGE: Can still perform ring attack while moving to rock
            if (TargetPlayer != null && !isPerformingCombo)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, TargetPlayer.position);
                TryPerformRingAttack(distanceToPlayer);
            }

            float distance = Vector3.Distance(transform.position, targetPosition);

            if (distance < 1f)
            {
                isPositioningUnderRock = false;
                currentState = ShellBossState.WaitingUnderRock;
                Debug.Log($"[{gameObject.name}] ✓ Positioned under rock - waiting for player to trigger fall");
                yield break;
            }

            MoveTowards(targetPosition, CurrentMoveSpeed);
            RotateTowards(targetPosition);

            yield return null;
        }
    }

    private void HandleWaitingUnderRock()
    {
        if (targetRockSpawnPoint == null) return;

        FallingRock rock = targetRockSpawnPoint.GetRockScript();
        
        if (rock == null || rock.HasBeenTriggered)
        {
            Debug.Log($"[{gameObject.name}] Rock was triggered or destroyed - finding new position");
            TransitionToRockPositioning();
        }
    }

    private void StartRepositionWait()
    {
        currentState = ShellBossState.WaitingForReposition;
        
        Debug.Log($"[{gameObject.name}] Failed rock positioning (30% chance) - waiting {repositionWaitTime}s");

        if (repositionCoroutine != null)
        {
            StopCoroutine(repositionCoroutine);
        }
        repositionCoroutine = StartCoroutine(RepositionWaitRoutine());
    }

    private IEnumerator RepositionWaitRoutine()
    {
        yield return new WaitForSeconds(repositionWaitTime);

        currentState = ShellBossState.PositioningUnderRock;
        AttemptRockPositioning();
    }

    private void HandleRockPositioning()
    {
        // Movement handled by MoveToRockPosition coroutine
    }

    #endregion

    #region Phase 2: Enraged Phase Logic

    private void HandleBackingUp()
    {
        if (playerTransform == null) return;

        Vector3 directionAwayFromPlayer = (transform.position - playerTransform.position).normalized;
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

    private void MoveTowardsPlayer(float speed)
    {
        if (playerTransform == null) return;

        MoveTowards(playerTransform.position, speed);
    }

    private void MoveTowards(Vector3 targetPosition, float speed)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f;

        transform.position += direction * speed * Time.deltaTime;
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

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, phase1AdvanceDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, phase2KeepAwayDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, ringAttackRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, ringAttackTriggerRange);

        if (Application.isPlaying && playerTransform != null)
        {
            Gizmos.color = currentPhase == BossPhase.EnragedPhase ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }

        if (targetRockSpawnPoint != null && currentState == ShellBossState.PositioningUnderRock)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, targetRockSpawnPoint.transform.position);
            Gizmos.DrawWireSphere(targetRockSpawnPoint.transform.position, rockPositionCheckRadius);
        }

        if (targetRockSpawnPoint != null && currentState == ShellBossState.WaitingUnderRock)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetRockSpawnPoint.transform.position, rockPositionCheckRadius);
        }
    }

    #endregion
}

#region Enums

public enum BossPhase
{
    ShellPhase,
    EnragedPhase
}

public enum ShellBossState
{
    Advancing,
    PositioningUnderRock,
    WaitingUnderRock,
    WaitingForReposition,
    Fighting
}

#endregion