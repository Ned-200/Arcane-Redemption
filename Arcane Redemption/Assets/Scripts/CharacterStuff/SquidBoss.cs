using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Squid Boss with two-phase combat:
/// Phase 1 (0-20s): Invulnerable shell phase with ranged attacks and minion spawning
/// Phase 2 (20s+): Shell breaks, vulnerable melee tentacle slam attacks
/// </summary>
public class SquidBoss : EnemyCharacter
{
    #region Serialized Fields

    [Header("Boss Identity")]
    [SerializeField] private string bossDisplayName = "Deep Sea Kraken";

    [Header("Cutscene")]
    private PlayerData playerData;
    [SerializeField] private float cutsceneDelay = 17f;
    [SerializeField] private GameObject WaterGhostPrefab;
    private bool ghostSpawned = false;

    [Header("Shell Phase (Phase 1)")]
    [SerializeField] private ShellProtection shellProtection;
    [SerializeField] private float shellDuration = 20f;
    [SerializeField] private GameObject shellVisual;

    [Header("Minion Spawning (Phase 1)")]
    [SerializeField] private float minionSpawnInterval = 5f;
    [SerializeField] private BomberEnemy[] dormantBombers;
    [SerializeField] private int maxActiveBombers = 3;

    [Header("Projectile Attack (Phase 1)")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float projectileDamage = 15f;
    [SerializeField] private float projectileCooldown = 3f;

    [Header("Suck Attack (Phase 1)")]
    [SerializeField] private float suckDuration = 3f;
    [SerializeField] private float suckForce = 10f;
    [SerializeField] private float suckCooldown = 8f;
    [SerializeField] private float suckRadius = 15f;
    [SerializeField] private Transform suckSpawnPoint;
    [SerializeField] private GameObject suckVFXPrefab;

    [Header("Tentacle Slam (Phase 2)")]
    [SerializeField] private GameObject movingPlatformParent;
    private Transform[] movingPlatforms;
    [SerializeField] private GameObject tentaclePrefab;
    [SerializeField] private float tentacleSlamDamage = 20f;
    [SerializeField] private float tentacleSlamCooldown = 2.5f;
    [SerializeField] private float phase2MoveSpeed = 4f;
    [SerializeField] private float phase2AttackRange = 7f;

    [Header("Animation")]
    [SerializeField] private Animator bossAnimator;

    [Header("Audio")]
    [SerializeField] private AudioClip[] projectileSounds;
    [SerializeField] private AudioClip suckSound;
    [SerializeField] private AudioClip shellBreakSound;

    [Header("Boss UI")]
    private BossHealthBarUI healthBarUI;

    #endregion

    #region Private Fields

    private SquidBossPhase currentPhase = SquidBossPhase.ShellPhase;
    private float shellTimer;
    private float lastMinionSpawnTime;
    private float lastProjectileTime;
    private float lastSuckTime;
    private float lastTentacleSlamTime;

    private bool isPerformingSuck;
    private Coroutine suckCoroutine;
    private GameObject suckVFXInstance;

    private bool isPerformingTentacleSlam;
    private Coroutine tentacleSlamCoroutine;

    private Transform playerTransform;
    private Rigidbody playerRigidbody;

    private List<BomberEnemy> activeBombers = new List<BomberEnemy>();

    private bool cutsceneComplete = false;
    private float cutsceneTimer = 0f;

    #endregion

    #region Properties

    public SquidBossPhase CurrentPhase => currentPhase;
    public bool IsShellActive => currentPhase == SquidBossPhase.ShellPhase;
    public float ShellTimeRemaining => Mathf.Max(0f, shellDuration - shellTimer);

    #endregion

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();
        InitializeBoss();
    }

    private void Start()
    {
        FindPlayer();
        InitializeDormantBombers();

        // Fetch Player Data (This must be done at start, not awake.)
        playerData = GameObject.FindWithTag("PlayerData").GetComponent<PlayerData>();
        if (playerData == null)
        {
            Debug.LogError("TreeBoss cannot find PlayerData!");
        }
    }

    protected override void Update()
    {
        base.Update();

        if (IsDead) return;

        // Handle cutscene delay
        if (!cutsceneComplete)
        {
            cutsceneTimer += Time.deltaTime;
            if (cutsceneTimer >= cutsceneDelay)
            {
                cutsceneComplete = true;
                OnCutsceneComplete();
            }
            else
            {
                // Boss is idle during cutscene, only rotate to face player
                if (playerTransform != null)
                {
                    RotateTowardsPlayer();
                }
                return; // Don't execute combat logic during cutscene
            }
        }

        UpdatePhaseLogic();
    }

    #endregion

    #region Initialization

    private void InitializeBoss()
    {
        if (bossAnimator == null)
        {
            bossAnimator = GetComponent<Animator>();
        }

        if (shellProtection == null)
        {
            shellProtection = GetComponent<ShellProtection>();
        }

        if (tentaclePrefab == null)
        {
            Debug.LogError("SquidBoss: No tentaclePrefab assigned!");
        }

        movingPlatforms = movingPlatformParent.GetComponentsInChildren<Transform>();

        healthBarUI = FindFirstObjectByType<BossHealthBarUI>();

        shellTimer = 0f;
        lastMinionSpawnTime = -minionSpawnInterval;
        lastProjectileTime = -projectileCooldown;
        lastSuckTime = -suckCooldown;
        lastTentacleSlamTime = -tentacleSlamCooldown;

        cutsceneTimer = 0f;
        cutsceneComplete = false;

        Debug.Log($"[{gameObject.name}] Squid Boss initialized - Cutscene delay: {cutsceneDelay}s, Shell duration: {shellDuration}s");
    }

    private void FindPlayer()
    {
        PlayerCharacter player = FindFirstObjectByType<PlayerCharacter>();
        if (player != null)
        {
            playerTransform = player.transform;
            playerRigidbody = player.GetComponent<Rigidbody>();
            SetTarget(playerTransform);

            if (healthBarUI != null)
            {
                //healthBarUI.ShowBossHealthBar(this, bossDisplayName);
            }
        }
    }

    private void InitializeDormantBombers()
    {
        if (dormantBombers == null || dormantBombers.Length == 0)
        {
            dormantBombers = FindObjectsByType<BomberEnemy>(FindObjectsSortMode.None);
            Debug.Log($"[{gameObject.name}] Auto-found {dormantBombers.Length} bomber enemies in scene");
        }

        foreach (BomberEnemy bomber in dormantBombers)
        {
            if (bomber != null)
            {
                bomber.gameObject.SetActive(false);
            }
        }
    }

    private void DestroyBombers()
    {
        foreach (BomberEnemy bomber in dormantBombers)
        {
            if (bomber != null)
            {
                bomber.gameObject.SetActive(false);
            }
        }
    }

    private void OnCutsceneComplete()
    {
        Debug.Log($"[{gameObject.name}] ⚡ Cutscene complete! Boss fight begins!");
        
        // Show health bar when combat starts
        if (healthBarUI != null)
        {
            //healthBarUI.ShowBossHealthBar(this, bossDisplayName);
        }

        // Play intro animation or roar (optional)
        if (bossAnimator != null)
        {
            // You can trigger an intro animation here if you have one
            // bossAnimator.SetTrigger("BattleStart");
        }
    }

    #endregion

    #region Phase Management

    private void UpdatePhaseLogic()
    {
        switch (currentPhase)
        {
            case SquidBossPhase.ShellPhase:
                UpdateShellPhase();
                break;

            case SquidBossPhase.VulnerablePhase:
                UpdateVulnerablePhase();
                break;
        }
    }

    private void UpdateShellPhase()
    {
        shellTimer += Time.deltaTime;

        if (shellTimer >= shellDuration)
        {
            TransitionToVulnerablePhase();
            return;
        }

        if (!isPerformingSuck)
        {
            TrySpawnMinion();
            TryFireProjectile();
            TrySuckAttack();
        }

        if (playerTransform != null)
        {
            RotateTowardsPlayer();
        }
    }

    private void UpdateVulnerablePhase()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (!isPerformingTentacleSlam)
        {
            TryTentacleSlam();
            RotateTowardsPlayer();
        }
    }

    private void TransitionToVulnerablePhase()
    {
        if (currentPhase == SquidBossPhase.VulnerablePhase) return;

        Debug.LogWarning($"[{gameObject.name}] ⚡ SHELL TIMER EXPIRED! Transitioning to Phase 2...");

        currentPhase = SquidBossPhase.VulnerablePhase;

        BreakShell();

        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("ShellBreak");
        }

        if (shellBreakSound != null)
        {
            AudioSource.PlayClipAtPoint(shellBreakSound, transform.position);
        }

        StopAllPhase1Actions();
    }

    private void BreakShell()
    {
        if (shellProtection != null && shellProtection.IsShellActive)
        {
            while (shellProtection.IsShellActive)
            {
                shellProtection.TryDamageShell();
            }
        }

        if (shellVisual != null)
        {
            shellVisual.SetActive(false);
        }
    }

    private void StopAllPhase1Actions()
    {
        if (suckCoroutine != null)
        {
            StopCoroutine(suckCoroutine);
            suckCoroutine = null;
            isPerformingSuck = false;
            if (bossAnimator != null)
            {
                bossAnimator.SetBool("isSucking", false);
            }
        }

        if (suckVFXInstance != null)
        {
            Destroy(suckVFXInstance);
        }
    }

    #endregion

    #region Phase 1: Minion Spawning

    private void TrySpawnMinion()
    {
        if (Time.time - lastMinionSpawnTime < minionSpawnInterval) return;

        activeBombers.RemoveAll(b => b == null || !b.gameObject.activeSelf);

        if (activeBombers.Count >= maxActiveBombers) return;

        BomberEnemy dormantBomber = FindDormantBomber();
        if (dormantBomber != null)
        {
            ActivateBomber(dormantBomber);
            lastMinionSpawnTime = Time.time;
        }
    }

    private BomberEnemy FindDormantBomber()
    {
        foreach (BomberEnemy bomber in dormantBombers)
        {
            if (bomber != null && !bomber.gameObject.activeSelf)
            {
                return bomber;
            }
        }
        return null;
    }

    private void ActivateBomber(BomberEnemy bomber)
    {
        bomber.gameObject.SetActive(true);
        activeBombers.Add(bomber);
        Debug.Log($"[{gameObject.name}] 💣 Activated bomber minion! Active: {activeBombers.Count}/{maxActiveBombers}");
    }

    #endregion

    #region Phase 1: Projectile Attack

    private void TryFireProjectile()
    {
        if (playerTransform == null) return;
        if (Time.time - lastProjectileTime < projectileCooldown) return;
        if (projectilePrefab == null) return;

        FireProjectile();
        lastProjectileTime = Time.time;
    }

    private void FireProjectile()
    {
        if (bossAnimator != null)
        {
            bossAnimator.Play("Shoot");
        }

        Vector3 spawnPosition = projectileSpawnPoint != null 
            ? projectileSpawnPoint.position 
            : transform.position + Vector3.up * 2f;

        GameObject projectileObj = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

        TreeBossProjectile projectile = projectileObj.GetComponent<TreeBossProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(projectileDamage, this, projectileSpeed, playerTransform);
            Debug.Log($"[{gameObject.name}] 🎯 Fired projectile at player!");
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Projectile prefab missing TreeBossProjectile component!");
            Destroy(projectileObj);
        }

        if (projectileSounds.Length > 0)
        {
            AudioSource.PlayClipAtPoint(projectileSounds[Random.Range(0, projectileSounds.Length)], transform.position);
        }
    }

    #endregion

    #region Phase 1: Suck Attack

    private void TrySuckAttack()
    {
        if (playerTransform == null) return;
        if (Time.time - lastSuckTime < suckCooldown) return;

        PerformSuckAttack();
        lastSuckTime = Time.time;
    }

    private void PerformSuckAttack()
    {
        if (suckCoroutine != null)
        {
            StopCoroutine(suckCoroutine);
        }

        suckCoroutine = StartCoroutine(SuckAttackSequence());
    }

    private IEnumerator SuckAttackSequence()
    {
        isPerformingSuck = true;

        Debug.Log($"[{gameObject.name}] 🌀 SUCK ATTACK! Pulling player for {suckDuration}s");

        if (bossAnimator != null)
        {
            bossAnimator.SetBool("isSucking", true);
        }

        if (suckVFXPrefab != null)
        {
            suckVFXInstance = Instantiate(suckVFXPrefab, suckSpawnPoint.position, suckSpawnPoint.rotation);
        }

        if (suckSound != null)
        {
            AudioSource.PlayClipAtPoint(suckSound, transform.position);
        }

        float suckTimer = 0f;

        while (suckTimer < suckDuration)
        {
            ApplySuckForce();
            suckTimer += Time.deltaTime;
            yield return null;
        }

        if (suckVFXInstance != null)
        {
            Destroy(suckVFXInstance);
        }

        isPerformingSuck = false;
        if (bossAnimator != null)
        {
            bossAnimator.SetBool("isSucking", false);
        }
        suckCoroutine = null;
    }

    private void ApplySuckForce()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer > suckRadius) return;

        Vector3 directionToBoss = (transform.position - playerTransform.position).normalized;
        float forceMagnitude = suckForce * Time.deltaTime;

        if (playerRigidbody != null)
        {
            playerRigidbody.AddForce(directionToBoss * forceMagnitude, ForceMode.VelocityChange);
        }
        else
        {
            CharacterController controller = playerTransform.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.Move(directionToBoss * forceMagnitude);
            }
            else
            {
                playerTransform.position += directionToBoss * forceMagnitude;
            }
        }
    }

    #endregion

    #region Phase 2: Tentacle Slam

    private void TryTentacleSlam()
    {
        if (Time.time - lastTentacleSlamTime < tentacleSlamCooldown) return;
        if (isPerformingTentacleSlam) return;

        PerformTentacleSlam();
        lastTentacleSlamTime = Time.time;
    }

    private void PerformTentacleSlam()
    {
        if (tentacleSlamCoroutine != null)
        {
            StopCoroutine(tentacleSlamCoroutine);
        }

        tentacleSlamCoroutine = StartCoroutine(TentacleSlamSequence());
    }

    private IEnumerator TentacleSlamSequence()
    {
        isPerformingTentacleSlam = true;

        Debug.Log($"[{gameObject.name}] 🦑 TENTACLE SLAM!");

        if (movingPlatforms.Length > 0) {
            if (movingPlatforms.Contains(playerTransform.parent))
            {
                Transform playerPlatform = playerTransform.parent;

                Vector3 lookDirection = playerTransform.localPosition - playerPlatform.position;
                lookDirection = new Vector3(0, lookDirection.y, 0);
                lookDirection.Normalize();
                Quaternion startRotation = Quaternion.EulerAngles(lookDirection);
                Vector3 positionOffset = new Vector3(playerPlatform.position.x,playerPlatform.position.y-8, playerPlatform.position.z);

                GameObject TentacleAttack = Instantiate(tentaclePrefab, positionOffset -= lookDirection, startRotation);
                TentacleAttack.transform.SetParent(playerPlatform);
            }
        } else
        {
            Debug.LogError("SquidBoss: No moving platforms assgined! Check inspector!");
        }

        if (bossAnimator != null)
        {
            bossAnimator.Play("Slam");
        }

        yield return new WaitForSeconds(0.5f);

        isPerformingTentacleSlam = false;
        tentacleSlamCoroutine = null;
    }

    #endregion

    #region Movement

    private void MoveTowardsPlayer()
    {
        if (playerTransform == null) return;

        Vector3 direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0f;

        transform.position += direction * phase2MoveSpeed * Time.deltaTime;

        if (bossAnimator != null)
        {
            bossAnimator.SetBool("isWalking", true);
        }
    }

    private void RotateTowardsPlayer()
    {
        if (playerTransform == null) return;

        Vector3 direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
        }
    }

    #endregion

    #region Damage System

    public override void TakeDamage(float damage)
    {
        // Block damage during cutscene
        if (!cutsceneComplete)
        {
            Debug.Log($"[{gameObject.name}] 🎬 Boss is invulnerable during cutscene!");
            OnDamageBlocked(damage);
            return;
        }

        if (IsShellActive)
        {
            Debug.Log($"[{gameObject.name}] 🛡️ BLOCKED {damage} damage - shell is active!");
            OnDamageBlocked(damage);
            return;
        }

        base.TakeDamage(damage);
    }

    #endregion

    #region Override Methods

    protected override void OnDeath()
    {
        base.OnDeath();

        DestroyBombers();

        if (healthBarUI != null)
        {
            healthBarUI.HideBossHealthBar();
        }

        StopAllPhase1Actions();

        if (tentacleSlamCoroutine != null)
        {
            StopCoroutine(tentacleSlamCoroutine);
        }

        if (!ghostSpawned)
        {
            ghostSpawned = true;
            if (WaterGhostPrefab != null)
            {
                Instantiate(WaterGhostPrefab, transform.position+new Vector3(0,25,0), WaterGhostPrefab.transform.rotation);
            }
            else
            {
                Debug.LogError("SquidBoss not assigned a Ghost NPC Prefab! Check Fields!");
            }
        }

        // Update player data that water boss was defeated
        if (playerData != null)
        {
            playerData.waterBossDefeated = true;
        }

        Debug.Log($"[{gameObject.name}] ☠️ Squid Boss defeated!");
    }

    #endregion

    #region Debug Gizmos

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, suckRadius);

        if (currentPhase == SquidBossPhase.VulnerablePhase)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, phase2AttackRange);
        }

        if (Application.isPlaying && playerTransform != null)
        {
            Gizmos.color = IsShellActive ? Color.blue : Color.red;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }

        if (Application.isPlaying && IsShellActive)
        {
            Gizmos.color = Color.yellow;
            Vector3 timerPos = transform.position + Vector3.up * 5f;
            Gizmos.DrawWireSphere(timerPos, ShellTimeRemaining / shellDuration * 2f);
        }

        // Show cutscene timer
        if (Application.isPlaying && !cutsceneComplete)
        {
            Gizmos.color = Color.magenta;
            Vector3 cutsceneTimerPos = transform.position + Vector3.up * 6f;
            float cutsceneProgress = cutsceneTimer / cutsceneDelay;
            Gizmos.DrawWireSphere(cutsceneTimerPos, cutsceneProgress * 2f);
        }
    }

    #endregion

    #region Context Menu

    [ContextMenu("Force Break Shell")]
    private void ForceBreakShell()
    {
        TransitionToVulnerablePhase();
    }

    [ContextMenu("Spawn Minion")]
    private void ForceSpawnMinion()
    {
        BomberEnemy bomber = FindDormantBomber();
        if (bomber != null)
        {
            ActivateBomber(bomber);
        }
    }

    [ContextMenu("Skip Cutscene")]
    private void SkipCutscene()
    {
        cutsceneComplete = true;
        OnCutsceneComplete();
        Debug.Log($"[{gameObject.name}] Cutscene skipped!");
    }

    #endregion
}

#region Enums

public enum SquidBossPhase
{
    ShellPhase,
    VulnerablePhase
}

#endregion