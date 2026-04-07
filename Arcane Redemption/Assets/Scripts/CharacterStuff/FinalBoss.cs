using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Final Boss with two-phase combat system:
/// Phase 1 (Health > 50%): Elemental cycling every 10s (Plant/Fire/Water) with element-specific damage filtering
/// Phase 2 (Health <= 50%): Enraged melee/ranged hybrid with 50/50 RNG decision making
/// </summary>
public class FinalBoss : EnemyCharacter
{
    #region Serialized Fields

    [Header("Boss Identity")]
    [SerializeField] private string bossDisplayName = "Arcane Overlord";

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 20f;
    [SerializeField] private bool requiresDetection = true;

    [Header("Phase Transition")]
    [SerializeField] private float phase2HealthThreshold = 0.5f; // 50% health

    [Header("Phase 1: Elemental Cycling")]
    [SerializeField] private float elementSwitchInterval = 10f;
    [SerializeField] private bool showElementIndicator = true;
    [SerializeField] private Material plantMaterial;
    [SerializeField] private Material fireMaterial;
    [SerializeField] private Material waterMaterial;
    [SerializeField] private Renderer bossRenderer;

    [Header("Plant Mode Settings")]
    [SerializeField] private float plantKitingDistance = 10f;
    [SerializeField] private float plantMoveSpeed = 4f;
    [SerializeField] private int plantVolleyMinProjectiles = 3;
    [SerializeField] private int plantVolleyMaxProjectiles = 5;
    [SerializeField] private float plantVolleyCooldown = 5f;
    [SerializeField] private GameObject plantProjectilePrefab;
    [SerializeField] private GameObject[] plantMinionPrefabs;
    [SerializeField] private float plantSummonCooldown = 8f;

    [Header("Fire Mode Settings")]
    [SerializeField] private float fireApproachSpeed = 5f;
    [SerializeField] private int fireHitsBeforeRetreat = 2;
    [SerializeField] private float fireRetreatDistance = 15f;
    [SerializeField] private float fireRetreatSpeed = 6f;
    [SerializeField] private GameObject fireRingPrefab;
    [SerializeField] private int minComboAttacks = 1;
    [SerializeField] private int maxComboAttacks = 4;
    [SerializeField] private float fireRingComboAttackCooldownMin = 1f;
    [SerializeField] private float fireRingComboAttackCooldownMax = 2f;
    [SerializeField] private float fireRingComboCooldownMin = 6f;
    [SerializeField] private float fireRingComboCooldownMax = 10f;
    [SerializeField] private AudioClip[] fireRingSounds;

    [Header("Water Mode Settings")]
    [SerializeField] private float waterFleeSpeed = 5.5f;
    [SerializeField] private float waterFleeDistance = 20f;
    [SerializeField] private GameObject[] waterMinionPrefabs;
    [SerializeField] private float waterSummonCooldown = 7f;

    [Header("Phase 2: Enraged Settings")]
    [SerializeField] private float enragedSpeedMultiplier = 1.5f;
    [SerializeField] private float enragedDamageMultiplier = 1.3f;
    [SerializeField] private float enragedChargeSpeed = 7f;
    [SerializeField] private float enragedRetreatDistance = 20f;
    [SerializeField] private float enragedRetreatSpeed = 8f;

    [Header("Phase 2: Melee Attack Colliders")]
    [SerializeField] private Collider mouthCollider;
    [SerializeField] private Collider handCollider;
    [SerializeField] private float chompDamage = 30f;
    [SerializeField] private float slamDamage = 25f;
    [SerializeField] private float meleeAttackRange = 5f;

    [Header("Phase 2: Ranged Settings")]
    [SerializeField] private GameObject enragedProjectilePrefab;
    [SerializeField] private int enragedVolleyCount = 5;
    [SerializeField] private float enragedProjectileSpeed = 20f;
    [SerializeField] private float enragedProjectileDamage = 20f;

    [Header("Projectile Common Settings")]
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float projectileSpawnDelay = 0.2f;

    [Header("Animation")]
    [SerializeField] private Animator bossAnimator;

    [Header("Audio")]
    [SerializeField] private AudioClip[] elementSwitchSounds;
    [SerializeField] private AudioClip[] chompSounds;
    [SerializeField] private AudioClip[] slamSounds;
    [SerializeField] private AudioClip[] projectileSounds;
    [SerializeField] private AudioClip enrageSound;
    [SerializeField] private AudioClip detectionSound;

    [Header("Boss UI")]
    private BossHealthBarUI healthBarUI;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool showDetectionRing = true;

    #endregion

    #region Private Fields

    private FinalBossPhase currentPhase = FinalBossPhase.ElementalCycle;
    private ElementType currentElement = ElementType.Plant;
    private float elementTimer = 0f;

    private Transform playerTransform;

    // Detection
    private bool playerDetected = false;

    // Phase 1 tracking
    private int fireHitsTaken = 0;
    private bool isRetreating = false;
    private float lastPlantVolleyTime;
    private float lastPlantSummonTime;
    private float lastFireRingComboTime;
    private float lastWaterSummonTime;
    private bool isPerformingFireRingCombo = false;
    private Coroutine fireRingComboCoroutine;

    // Phase 2 tracking
    private bool isEnraged = false;
    private EnragedBehavior currentEnragedBehavior;
    private bool isPerformingMeleeAttack = false;
    private Coroutine enragedBehaviorCoroutine;

    private Rigidbody bossRigidbody;
    private AudioSource audioSource;

    #endregion

    #region Properties

    public FinalBossPhase CurrentPhase => currentPhase;
    public ElementType CurrentElement => currentElement;
    public bool IsEnraged => isEnraged;
    public bool PlayerDetected => playerDetected;

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
    }

    protected override void Update()
    {
        base.Update();

        if (IsDead) return;

        // Check for player detection
        if (!playerDetected && requiresDetection)
        {
            CheckPlayerDetection();
            
            // Only rotate to face player when not detected
            if (playerTransform != null && !playerDetected)
            {
                RotateTowardsPlayer();
            }
            
            return; // Don't execute combat logic until detected
        }

        CheckPhaseTransition();
        UpdateBossLogic();
    }

    #endregion

    #region Initialization

    private void InitializeBoss()
    {
        if (bossAnimator == null)
        {
            bossAnimator = GetComponent<Animator>();
        }

        if (bossRenderer == null)
        {
            bossRenderer = GetComponentInChildren<Renderer>();
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (mouthCollider != null) mouthCollider.enabled = false;
        if (handCollider != null) handCollider.enabled = false;

        healthBarUI = FindFirstObjectByType<BossHealthBarUI>();

        lastPlantVolleyTime = -plantVolleyCooldown;
        lastPlantSummonTime = -plantSummonCooldown;
        lastFireRingComboTime = -fireRingComboCooldownMax;
        lastWaterSummonTime = -waterSummonCooldown;

        // Set initial element property for ProjectileBase
        element = "Plant";
        ApplyElementalMaterial();

        // Initialize Rigidbody
        bossRigidbody = GetComponent<Rigidbody>();
        if (bossRigidbody == null)
        {
            Debug.LogError($"[{gameObject.name}] FinalBoss requires a Rigidbody component!");
        }
        else
        {
            bossRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        // Initialize detection state
        playerDetected = !requiresDetection; // If detection not required, start active

        Debug.Log($"[{gameObject.name}] Final Boss initialized - Starting element: {currentElement}, Detection required: {requiresDetection}");
    }

    private void FindPlayer()
    {
        PlayerCharacter player = FindFirstObjectByType<PlayerCharacter>();
        if (player != null)
        {
            playerTransform = player.transform;
            SetTarget(playerTransform);
        }
    }

    #endregion

    #region Detection System

    private void CheckPlayerDetection()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= detectionRadius)
        {
            OnPlayerDetected();
        }
    }

    private void OnPlayerDetected()
    {
        playerDetected = true;

        Debug.Log($"[{gameObject.name}] 👁️ PLAYER DETECTED! Combat begins!");

        // Show health bar when combat starts
        if (healthBarUI != null)
        {
            //healthBarUI.ShowBossHealthBar(this, bossDisplayName);
        }

        // Play detection sound
        if (detectionSound != null)
        {
            AudioSource.PlayClipAtPoint(detectionSound, transform.position);
        }

        // Play intro animation if available
        if (bossAnimator != null)
        {
            // Try to trigger intro animation
            // If you have an "Intro" trigger parameter, uncomment:
            // bossAnimator.SetTrigger("Intro");
        }
    }

    #endregion

    #region Phase Management

    private void CheckPhaseTransition()
    {
        if (currentPhase == FinalBossPhase.ElementalCycle && HealthPercent <= phase2HealthThreshold)
        {
            TransitionToEnragedPhase();
        }
    }

    private void TransitionToEnragedPhase()
    {
        Debug.LogWarning($"[{gameObject.name}] ⚡ ENRAGED! Health at {HealthPercent * 100:F1}%");

        currentPhase = FinalBossPhase.Enraged;
        isEnraged = true;

        // Clear element to accept all damage
        element = "";

        if (enrageSound != null)
        {
            AudioSource.PlayClipAtPoint(enrageSound, transform.position);
        }

        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("Enrage");
        }

        StartEnragedBehavior();
    }

    #endregion

    #region Boss Update Logic

    private void UpdateBossLogic()
    {
        switch (currentPhase)
        {
            case FinalBossPhase.ElementalCycle:
                UpdateElementalCyclePhase();
                break;

            case FinalBossPhase.Enraged:
                UpdateEnragedPhase();
                break;
        }
    }

    #endregion

    #region Phase 1: Elemental Cycle

    private void UpdateElementalCyclePhase()
    {
        elementTimer += Time.deltaTime;

        if (elementTimer >= elementSwitchInterval)
        {
            CycleToNextElement();
            elementTimer = 0f;
        }

        ExecuteElementalBehavior();
    }

    private void CycleToNextElement()
    {
        switch (currentElement)
        {
            case ElementType.Plant:
                currentElement = ElementType.Fire;
                element = "Fire";
                break;
            case ElementType.Fire:
                currentElement = ElementType.Water;
                element = "Water";
                break;
            case ElementType.Water:
                currentElement = ElementType.Plant;
                element = "Plant";
                break;
        }

        fireHitsTaken = 0;
        isRetreating = false;

        ApplyElementalMaterial();

        if (elementSwitchSounds.Length > 0)
        {
            AudioSource.PlayClipAtPoint(elementSwitchSounds[Random.Range(0, elementSwitchSounds.Length)], transform.position);
        }

        Debug.Log($"[{gameObject.name}] 🔄 Element switched to: {currentElement} (element property: {element})");
    }

    private void ApplyElementalMaterial()
    {
        if (bossRenderer == null) return;

        Material targetMaterial = currentElement switch
        {
            ElementType.Plant => plantMaterial,
            ElementType.Fire => fireMaterial,
            ElementType.Water => waterMaterial,
            _ => null
        };

        if (targetMaterial != null)
        {
            bossRenderer.material = targetMaterial;
        }
    }

    private void ExecuteElementalBehavior()
    {
        if (playerTransform == null) return;

        switch (currentElement)
        {
            case ElementType.Plant:
                ExecutePlantBehavior();
                break;
            case ElementType.Fire:
                ExecuteFireBehavior();
                break;
            case ElementType.Water:
                ExecuteWaterBehavior();
                break;
        }
    }

    #endregion

    #region Plant Mode (Kiting)

    private void ExecutePlantBehavior()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= plantKitingDistance)
        {
            KiteAwayFromPlayer(plantMoveSpeed);
        }

        RotateTowardsPlayer();

        TryPlantVolley();
        TryPlantSummon();
    }

    private void TryPlantVolley()
    {
        if (Time.time - lastPlantVolleyTime < plantVolleyCooldown) return;
        if (plantProjectilePrefab == null) return;

        StartCoroutine(FireProjectileVolley(plantProjectilePrefab, Random.Range(plantVolleyMinProjectiles, plantVolleyMaxProjectiles + 1), 15f));
        lastPlantVolleyTime = Time.time;
    }

    private void TryPlantSummon()
    {
        if (Time.time - lastPlantSummonTime < plantSummonCooldown) return;
        if (plantMinionPrefabs == null || plantMinionPrefabs.Length == 0) return;

        SummonMinion(plantMinionPrefabs);
        lastPlantSummonTime = Time.time;
    }

    #endregion

    #region Fire Mode (Hit-and-Run)

    private void ExecuteFireBehavior()
    {
        if (isRetreating)
        {
            ExecuteFireRetreat();
        }
        else
        {
            ApproachPlayer(fireApproachSpeed);
            TryFireRingAttack();
        }

        RotateTowardsPlayer();
    }

    private void ExecuteFireRetreat()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer >= fireRetreatDistance)
        {
            isRetreating = false;
            fireHitsTaken = 0;
            Debug.Log($"[{gameObject.name}] Fire mode: Retreat complete, resuming approach");
            return;
        }

        KiteAwayFromPlayer(fireRetreatSpeed);
    }

    private void TryFireRingAttack()
    {
        if (isPerformingFireRingCombo) return;

        float comboCooldown = Random.Range(fireRingComboCooldownMin, fireRingComboCooldownMax);

        if (Time.time - lastFireRingComboTime < comboCooldown) return;

        PerformFireRingAttackCombo();
    }

    private void PerformFireRingAttackCombo()
    {
        if (fireRingComboCoroutine != null)
        {
            StopCoroutine(fireRingComboCoroutine);
        }

        int comboLength = Random.Range(minComboAttacks, maxComboAttacks + 1);
        fireRingComboCoroutine = StartCoroutine(FireRingAttackComboSequence(comboLength));
    }

    private IEnumerator FireRingAttackComboSequence(int attackCount)
    {
        isPerformingFireRingCombo = true;

        Debug.Log($"[{gameObject.name}] 🔥 Starting Fire Ring Combo x{attackCount}");

        for (int i = 0; i < attackCount; i++)
        {
            ExecuteSingleFireRingAttack();

            if (i < attackCount - 1)
            {
                float attackDelay = Random.Range(fireRingComboAttackCooldownMin, fireRingComboAttackCooldownMax);
                yield return new WaitForSeconds(attackDelay);
            }
        }

        lastFireRingComboTime = Time.time;
        isPerformingFireRingCombo = false;
        fireRingComboCoroutine = null;
    }

    private void ExecuteSingleFireRingAttack()
    {
        if (bossAnimator != null)
        {
            bossAnimator.Play("Attack");
        }

        // Play fire ring sound
        if (fireRingSounds != null && fireRingSounds.Length > 0 && audioSource != null)
        {
            audioSource.PlayOneShot(fireRingSounds[Random.Range(0, fireRingSounds.Length)]);
        }

        // Create ring attack 2m above ground level with fixed X rotation
        if (fireRingPrefab != null)
        {
            Vector3 spawnPosition = transform.position;
            
            // Raycast down to find ground
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out hit, 20f))
            {
                spawnPosition = new Vector3(transform.position.x, hit.point.y + 2f, transform.position.z);
            }
            else
            {
                spawnPosition.y = 2f; // Fallback to Y=2 if no ground found
            }

            // Force X rotation to 90 degrees (parallel to ground)
            Quaternion rotation = Quaternion.Euler(90f, 0f, 0f);
            
            Instantiate(fireRingPrefab, spawnPosition, rotation);
        }

        Debug.Log($"[{gameObject.name}] 🔥 Fire Ring spawned!");
    }

    #endregion

    #region Water Mode (Fleeing)

    private void ExecuteWaterBehavior()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer < waterFleeDistance)
        {
            KiteAwayFromPlayer(waterFleeSpeed);
        }

        RotateTowardsPlayer();

        TryWaterSummon();
    }

    private void TryWaterSummon()
    {
        if (Time.time - lastWaterSummonTime < waterSummonCooldown) return;
        if (waterMinionPrefabs == null || waterMinionPrefabs.Length == 0) return;

        SummonMinion(waterMinionPrefabs);
        lastWaterSummonTime = Time.time;
    }

    #endregion

    #region Minion Summoning

    private void SummonMinion(GameObject[] minionPrefabs)
    {
        if (minionPrefabs.Length == 0) return;

        GameObject minionPrefab = minionPrefabs[Random.Range(0, minionPrefabs.Length)];
        
        // Generate random direction on horizontal plane
        Vector2 randomCircle = Random.insideUnitCircle.normalized; // Normalized to get edge of circle
        Vector3 spawnDirection = new Vector3(randomCircle.x, 0f, randomCircle.y);
        
        // Spawn exactly 15m away from boss
        Vector3 spawnPosition = transform.position + (spawnDirection * 15f);

        Instantiate(minionPrefab, spawnPosition, Quaternion.identity);

        Debug.Log($"[{gameObject.name}] 👾 Summoned {currentElement} minion at 15m distance!");
    }

    #endregion

    #region Projectile System

    private IEnumerator FireProjectileVolley(GameObject projectilePrefab, int count, float damage)
    {
        if (bossAnimator != null)
        {
            bossAnimator.Play("Shoot");
        }

        for (int i = 0; i < count; i++)
        {
            FireSingleProjectile(projectilePrefab, damage);

            if (i < count - 1)
            {
                yield return new WaitForSeconds(projectileSpawnDelay);
            }
        }

        Debug.Log($"[{gameObject.name}] 🎯 Fired volley of {count} projectiles!");
    }

    private void FireSingleProjectile(GameObject projectilePrefab, float damage)
    {
        if (playerTransform == null || projectilePrefab == null) return;

        Vector3 spawnPosition = projectileSpawnPoint != null 
            ? projectileSpawnPoint.position 
            : transform.position + Vector3.up * 2f;

        GameObject projectileObj = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

        TreeBossProjectile projectile = projectileObj.GetComponent<TreeBossProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(damage, this, projectileSpeed, playerTransform);
        }

        if (projectileSounds.Length > 0)
        {
            AudioSource.PlayClipAtPoint(projectileSounds[Random.Range(0, projectileSounds.Length)], transform.position);
        }
    }

    #endregion

    #region Phase 2: Enraged

    private void UpdateEnragedPhase()
    {
        // Behavior is handled by coroutine
    }

    private void StartEnragedBehavior()
    {
        if (enragedBehaviorCoroutine != null)
        {
            StopCoroutine(enragedBehaviorCoroutine);
        }

        enragedBehaviorCoroutine = StartCoroutine(EnragedBehaviorLoop());
    }

    private IEnumerator EnragedBehaviorLoop()
    {
        while (isEnraged && !IsDead)
        {
            float rng = Random.value;

            if (rng <= 0.5f)
            {
                currentEnragedBehavior = EnragedBehavior.MeleeCharge;
                yield return StartCoroutine(ExecuteMeleeCharge());
            }
            else
            {
                currentEnragedBehavior = EnragedBehavior.RangedRetreat;
                yield return StartCoroutine(ExecuteRangedRetreat());
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator ExecuteMeleeCharge()
    {
        Debug.Log($"[{gameObject.name}] ⚔️ MELEE CHARGE!");

        while (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (distanceToPlayer <= meleeAttackRange)
            {
                PerformMeleeAttack();
                yield break;
            }

            ApproachPlayer(enragedChargeSpeed);
            RotateTowardsPlayer();

            yield return null;
        }
    }

    private void PerformMeleeAttack()
    {
        float rng = Random.value;

        if (rng <= 0.5f)
        {
            StartCoroutine(ChompAttack());
        }
        else
        {
            StartCoroutine(SlamAttack());
        }
    }

    private IEnumerator ChompAttack()
    {
        isPerformingMeleeAttack = true;

        if (bossAnimator != null)
        {
            bossAnimator.Play("Chomp");
        }

        Debug.Log($"[{gameObject.name}] 🦷 CHOMP ATTACK!");

        yield return new WaitForSeconds(0.8f);

        isPerformingMeleeAttack = false;
    }

    private IEnumerator SlamAttack()
    {
        isPerformingMeleeAttack = true;

        if (bossAnimator != null)
        {
            bossAnimator.Play("HandSlam");
        }

        Debug.Log($"[{gameObject.name}] ✋ HAND SLAM!");

        yield return new WaitForSeconds(0.8f);

        isPerformingMeleeAttack = false;
    }

    public void EnableMouthCollider()
    {
        if (mouthCollider != null) mouthCollider.enabled = true;
    }

    public void DisableMouthCollider()
    {
        if (mouthCollider != null) mouthCollider.enabled = false;
    }

    public void EnableHandCollider()
    {
        if (handCollider != null) handCollider.enabled = true;
    }

    public void DisableHandCollider()
    {
        if (handCollider != null) handCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isEnraged || !isPerformingMeleeAttack) return;

        PlayerCharacter player = other.GetComponent<PlayerCharacter>();
        if (player != null)
        {
            float damage = 0f;
            string attackType = "";

            if (mouthCollider != null && mouthCollider.enabled)
            {
                damage = chompDamage * enragedDamageMultiplier;
                attackType = "Chomp";

                if (chompSounds.Length > 0)
                {
                    AudioSource.PlayClipAtPoint(chompSounds[Random.Range(0, chompSounds.Length)], transform.position);
                }
            }
            else if (handCollider != null && handCollider.enabled)
            {
                damage = slamDamage * enragedDamageMultiplier;
                attackType = "Slam";

                if (slamSounds.Length > 0)
                {
                    AudioSource.PlayClipAtPoint(slamSounds[Random.Range(0, slamSounds.Length)], transform.position);
                }
            }

            if (damage > 0f)
            {
                player.TakeDamage(damage);
                Debug.Log($"[{gameObject.name}] 💥 {attackType} hit {player.name} for {damage:F1} damage!");
            }
        }
    }

    private IEnumerator ExecuteRangedRetreat()
    {
        Debug.Log($"[{gameObject.name}] 🏃 RANGED RETREAT STARTED!");

        float startDistance = Vector3.Distance(transform.position, playerTransform.position);
        float targetDistance = startDistance + enragedRetreatDistance;
        float retreatStartTime = Time.time;
        float maxRetreatTime = 3f; // Add timeout to prevent infinite loop

        Debug.Log($"[{gameObject.name}] Retreat - Start Distance: {startDistance:F1}m, Target Distance: {targetDistance:F1}m");

        while (playerTransform != null)
        {
            float currentDistance = Vector3.Distance(transform.position, playerTransform.position);
            float retreatElapsedTime = Time.time - retreatStartTime;

            // Break if reached target distance OR timeout
            if (currentDistance >= targetDistance || retreatElapsedTime >= maxRetreatTime)
            {
                Debug.Log($"[{gameObject.name}] ✓ Retreat ended - Distance: {currentDistance:F1}m, Time: {retreatElapsedTime:F1}s");
                break;
            }

            KiteAwayFromPlayer(enragedRetreatSpeed);
            RotateTowardsPlayer();

            yield return null;
        }

        // Check if we have required components for projectile volley
        if (enragedProjectilePrefab == null)
        {
            Debug.LogError($"[{gameObject.name}] ❌ Cannot fire projectile volley - enragedProjectilePrefab is NULL!");
            yield break;
        }

        Debug.Log($"[{gameObject.name}] 🎯 Starting projectile volley - Count: {enragedVolleyCount}, Damage: {enragedProjectileDamage}");

        yield return StartCoroutine(FireProjectileVolley(enragedProjectilePrefab, enragedVolleyCount, enragedProjectileDamage));

        Debug.Log($"[{gameObject.name}] ✓ Volley complete, charging back!");
    }

    #endregion

    #region Movement Helpers

    private void ApproachPlayer(float speed)
    {
        if (playerTransform == null || bossRigidbody == null) return;

        Vector3 direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0f;

        // Use Rigidbody velocity instead of transform.position
        Vector3 targetVelocity = direction * speed;
        targetVelocity.y = bossRigidbody.linearVelocity.y; // Preserve gravity

        bossRigidbody.linearVelocity = targetVelocity;

        if (bossAnimator != null)
        {
            bossAnimator.SetBool("isWalking", true);
        }
    }

    private void KiteAwayFromPlayer(float speed)
    {
        if (bossRigidbody == null) return;

        Vector3 directionAway = (transform.position - playerTransform.position).normalized;
        directionAway.y = 0f;

        Vector3 targetVelocity = directionAway * speed;
        targetVelocity.y = bossRigidbody.linearVelocity.y; // Preserve gravity

        bossRigidbody.linearVelocity = targetVelocity;

        if (bossAnimator != null)
        {
            bossAnimator.SetBool("isWalking", true);
        }
    }

    private void RotateTowardsPlayer()
    {
        if (playerTransform == null || bossRigidbody == null) return;

        Vector3 direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Quaternion newRotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
            
            // Use Rigidbody rotation
            bossRigidbody.MoveRotation(newRotation);
        }
    }

    #endregion

    #region Damage System

    public override void TakeDamage(float damage)
    {
        // Add this debug log at the very start
        Debug.Log($"[FinalBoss] TakeDamage CALLED - Damage: {damage}, Current Element: {element}, Phase: {currentPhase}, Enraged: {isEnraged}, IsDead: {IsDead}");
        
        if (IsDead) return;

        // Auto-detect player if hit before entering detection radius
        if (!playerDetected && requiresDetection)
        {
            OnPlayerDetected();
        }

        // Phase 2: Takes all damage
        if (isEnraged)
        {
            Debug.Log($"[FinalBoss] ENRAGED - accepting all damage");
            base.TakeDamage(damage);
            return;
        }

        // Phase 1: Element filtering already handled by ProjectileBase
        // If this method is called, the damage source was already validated
        Debug.Log($"[FinalBoss] Phase 1 - damage passed element check");
        base.TakeDamage(damage);

        // Track hits for Fire mode retreat
        if (currentElement == ElementType.Fire)
        {
            fireHitsTaken++;
            if (fireHitsTaken >= fireHitsBeforeRetreat)
            {
                isRetreating = true;
                Debug.Log($"[{gameObject.name}] Fire mode: Retreating after {fireHitsTaken} hits!");
            }
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

        if (enragedBehaviorCoroutine != null)
        {
            StopCoroutine(enragedBehaviorCoroutine);
        }

        StopAllCoroutines();

        Debug.Log($"[{gameObject.name}] ☠️ Final Boss defeated!");
    }

    #endregion

    #region Debug Gizmos

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // === DETECTION RADIUS (Highest Priority - Always Visible) ===
        if (requiresDetection && showDetectionRing)
        {
            // Draw filled semi-transparent sphere
            Gizmos.color = playerDetected 
                ? new Color(0f, 1f, 0f, 0.1f)  // Green when detected (semi-transparent)
                : new Color(1f, 0f, 0f, 0.1f); // Red when not detected (semi-transparent)
            Gizmos.DrawSphere(transform.position, detectionRadius);

            // Draw bright wireframe ring
            Gizmos.color = playerDetected 
                ? new Color(0f, 1f, 0f, 0.8f)  // Bright green when detected
                : new Color(1f, 0f, 0f, 0.8f); // Bright red when not detected
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            // Draw player line indicator when in play mode
            if (Application.isPlaying && playerTransform != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                bool playerInRange = distanceToPlayer <= detectionRadius;

                // Draw line from boss to player
                Gizmos.color = playerInRange 
                    ? new Color(0f, 1f, 0f, 0.6f)  // Green line when in range
                    : new Color(1f, 1f, 0f, 0.6f); // Yellow line when outside
                Gizmos.DrawLine(transform.position + Vector3.up * 1f, playerTransform.position + Vector3.up * 1f);

                // Draw distance marker
                Vector3 midPoint = (transform.position + playerTransform.position) / 2f + Vector3.up * 2f;
                Gizmos.DrawWireSphere(midPoint, 0.3f);

                #if UNITY_EDITOR
                // Display distance text in editor
                UnityEditor.Handles.Label(
                    midPoint,
                    $"Distance: {distanceToPlayer:F1}m / {detectionRadius:F1}m\n" +
                    $"Status: {(playerDetected ? "DETECTED" : "IDLE")}"
                );
                #endif
            }
        }

        // === ELEMENTAL BEHAVIOR RANGES (Lower Priority) ===
        if (currentPhase == FinalBossPhase.ElementalCycle && playerDetected)
        {
            switch (currentElement)
            {
                case ElementType.Plant:
                    Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                    Gizmos.DrawWireSphere(transform.position, plantKitingDistance);
                    break;
                case ElementType.Fire:
                    Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                    Gizmos.DrawWireSphere(transform.position, fireRetreatDistance);
                    break;
                case ElementType.Water:
                    Gizmos.color = new Color(0f, 0f, 1f, 0.3f);
                    Gizmos.DrawWireSphere(transform.position, waterFleeDistance);
                    break;
            }
        }

        // === ENRAGED MELEE RANGE ===
        if (isEnraged)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, meleeAttackRange);
        }

        // === PLAYER LINE (Combat Active) ===
        if (Application.isPlaying && playerTransform != null && playerDetected)
        {
            Gizmos.color = isEnraged ? Color.red : GetElementColor(currentElement);
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }

        // === ELEMENT TIMER INDICATOR ===
        if (Application.isPlaying && !isEnraged && playerDetected)
        {
            Gizmos.color = Color.yellow;
            Vector3 timerPos = transform.position + Vector3.up * 5f;
            float progress = elementTimer / elementSwitchInterval;
            Gizmos.DrawWireSphere(timerPos, progress * 2f);
        }
    }

    private Color GetElementColor(ElementType element)
    {
        return element switch
        {
            ElementType.Plant => Color.green,
            ElementType.Fire => Color.red,
            ElementType.Water => Color.blue,
            _ => Color.white
        };
    }

    #endregion

    #region Context Menu

    [ContextMenu("Cycle Element")]
    private void ForceCycleElement()
    {
        CycleToNextElement();
    }

    [ContextMenu("Force Enrage")]
    private void ForceEnrage()
    {
        TransitionToEnragedPhase();
    }

    [ContextMenu("Force Detection")]
    private void ForceDetection()
    {
        OnPlayerDetected();
    }

    [ContextMenu("Force Fire Ring Attack")]
    private void ForceFireRingAttack()
    {
        PerformFireRingAttackCombo();
    }

    #endregion
}

#region Enums

public enum FinalBossPhase
{
    ElementalCycle,
    Enraged
}

public enum ElementType
{
    Plant,
    Fire,
    Water
}

public enum EnragedBehavior
{
    MeleeCharge,
    RangedRetreat
}

#endregion