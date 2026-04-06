using UnityEngine;

/// <summary>
/// Enemy character that inherits stats and weapon slot from BaseCharacter
/// </summary>
public class EnemyCharacter : BaseCharacter
{
    [Header("Enemy Settings")]
    [SerializeField] private GameObject defaultWeapon;

    [Header("Animation")]
    private Animator enemyAnim;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] damagedSounds;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip[] attackSounds;

    [Header("AI Settings")]
    [SerializeField] private float detectionRadius = 15f;
    [SerializeField] private float combatRadius = 3f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Combat")]
    public string element;
    [SerializeField] private float attackDamage = 5f;
    [SerializeField] private float attackCooldown = 2f;

    [Header("Death Settings")]
    [SerializeField] private float deathDelay = 3f;

    [Header("Debug")]
    [SerializeField] protected bool showDebugGizmos = true;

    // State
    private EnemyState currentState = EnemyState.Idle;
    private Transform targetPlayer;
    private float lastAttackTime;
    private bool isDead;

    // Public accessors for AI Controller
    public EnemyState CurrentState => currentState;
    public Transform TargetPlayer => targetPlayer;
    public float DetectionRadius => detectionRadius;
    public float CombatRadius => combatRadius;
    public float AttackRange => attackRange;
    public float MoveSpeed => moveSpeed;
    public float RotationSpeed => rotationSpeed;
    public bool IsDead => isDead;

    //accessing disintegrate script
    public Disintegrate disintegrate;
    public Disintegrate[] partsToDisintegrate;

    protected override void Awake()
    {
        base.Awake(); // Initialize stats and weapon slot

        // FIXED: Safely initialize animator
        InitializeAnimator();
        
        // FIXED: Safely initialize disintegrate
        InitializeDisintegrate();

        // Equip default weapon
        EquipDefaultWeapon();
    }

    /// <summary>
    /// Safely initializes the animator component
    /// </summary>
    private void InitializeAnimator()
    {
        enemyAnim = GetComponent<Animator>();
        
        if (enemyAnim == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No Animator component found - animations disabled");
            return;
        }

        // Only try to play animation if animator has a controller
        if (enemyAnim.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Animator has no controller assigned - animations disabled");
            return;
        }

        // Play idle animation with random start time for variety
        // Trust that the state exists since we've validated the controller
        try
        {
            enemyAnim.Play("Idle", 0, Random.Range(0.0f, 1.0f));
            Debug.Log($"[{gameObject.name}]  Animator initialized successfully - Playing Idle");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[{gameObject.name}] Failed to play Idle state: {e.Message}");
        }
    }

    /// <summary>
    /// Checks if the animator has a specific state
    /// </summary>
    private bool HasAnimationState(string stateName)
    {
        if (enemyAnim == null || enemyAnim.runtimeAnimatorController == null)
            return false;

        foreach (var clip in enemyAnim.runtimeAnimatorController.animationClips)
        {
            if (clip.name == stateName)
                return true;
        }

        return false;
    }

    // / <summary>
    // / Safely initializes the disintegrate component
    // / </summary>
    private void InitializeDisintegrate()
    {
        if (disintegrate != null || partsToDisintegrate.Length > 0)
            return; // Already assigned in Inspector

        // Try to find on this GameObject
        disintegrate = GetComponent<Disintegrate>();
        
        if (disintegrate == null)
        {
            // Try to find on "Body" child
            Transform bodyTransform = transform.Find("Body");
            if (bodyTransform != null)
            {
                disintegrate = bodyTransform.GetComponent<Disintegrate>();
            }
        }

        if (disintegrate == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No Disintegrate component found - death effect will be skipped");
        }
    }

    protected override void Update()
    {
        base.Update(); // Handle stat regeneration

        if (isDead) return;

        // AI behavior handled by EnemyAIController
    }

    private void EquipDefaultWeapon()
    {
        if (defaultWeapon != null)
        {
            EquipWeapon(defaultWeapon);
        }
    }

    /// <summary>
    /// Sets the current enemy state
    /// </summary>
    public void SetState(EnemyState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            OnStateChanged(newState);
        }
    }

    /// <summary>
    /// Sets the target player
    /// </summary>
    public void SetTarget(Transform target)
    {
        targetPlayer = target;
    }

    /// <summary>
    /// Checks if the enemy should retreat based on health percentage
    /// </summary>
    /// <param name="healthThreshold">Health percentage threshold (0-1)</param>
    /// <returns>True if health is below threshold</returns>
    public bool ShouldRetreat(float healthThreshold)
    {
        return HealthPercent <= healthThreshold;
    }

    /// <summary>
    /// Gets the current health as a percentage (0-1)
    /// </summary>
    public float GetHealthPercent()
    {
        return HealthPercent;
    }

    /// <summary>
    /// Checks if the enemy is critically wounded
    /// </summary>
    public bool IsCriticallyWounded()
    {
        return HealthPercent <= 0.3f;
    }

    /// <summary>
    /// Attempts to attack the target
    /// </summary>
    public bool TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown)
        {
            return false;
        }

        if (targetPlayer == null)
        {
            return false;
        }

        float distanceToTarget = Vector3.Distance(transform.position, targetPlayer.position);
        if (distanceToTarget > attackRange)
        {
            return false;
        }

        PerformAttack();
        lastAttackTime = Time.time;
        return true;
    }

    /// <summary>
    /// Performs the attack and deals damage to the player
    /// </summary>
    private void PerformAttack()
    {
        if (targetPlayer == null) return;

        // Try to get BaseCharacter component first (more general)
        BaseCharacter targetCharacter = targetPlayer.GetComponent<BaseCharacter>();
        if (targetCharacter != null)
        {
            targetCharacter.TakeDamage(attackDamage);
            Debug.Log($"{gameObject.name} attacked {targetPlayer.name} for {attackDamage} damage!");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} attacked {targetPlayer.name} but target has no BaseCharacter component!");
        }

        if (attackSounds.Length > 0)
        {
            AudioSource.PlayClipAtPoint(attackSounds[Random.Range(0, attackSounds.Length)], transform.position);
        }

        OnAttackPerformed();
    }

    /// <summary>
    /// Takes damage from the player or environment
    /// Overrides base TakeDamage to add enemy-specific logging and behavior
    /// </summary>
    public override void TakeDamage(float damage)
    {
        // Debug entry point
        Debug.Log($"[{gameObject.name}]  EnemyCharacter.TakeDamage CALLED! Damage: {damage}, CurrentHealth: {CurrentHealth:F1}, IsDead: {isDead}");

        if (isDead)
        {
            Debug.LogWarning($"[{gameObject.name}] Already dead, ignoring {damage} damage");
            return;
        }

        // Store health before damage
        float healthBefore = CurrentHealth;

        // Call base class TakeDamage which handles health reduction and events
        base.TakeDamage(damage);

        // Play damaged sounds
        if (damagedSounds.Length > 0)
        {
            AudioSource audioSource = GetComponent<AudioSource>(); // play sounds from parent audiosource if exists
            if (audioSource != null) {
                audioSource.PlayOneShot(damagedSounds[Random.Range(0, damagedSounds.Length)]);
            } else { // Play clip at parent position otherwise
                AudioSource.PlayClipAtPoint(damagedSounds[Random.Range(0, damagedSounds.Length)], transform.position);
            }
        }

        // Remove physical detail - FIXED: Safely check for children
        Transform detail1 = transform.Find("Detail1");
        if (detail1 != null && HealthPercent <= 0.9f)
        {
            Disintegrate detailDisintegrate = detail1.GetComponent<Disintegrate>();
            if (detailDisintegrate != null)
                detailDisintegrate.TriggerDisintegration();
        }


        Transform detail2 = transform.Find("Detail2");
        if (detail2 != null && HealthPercent <= 0.5f)
        {
            Disintegrate detailDisintegrate = detail2.GetComponent<Disintegrate>();
            if (detailDisintegrate != null)
                detailDisintegrate.TriggerDisintegration();
        }

        Transform detail3 = transform.Find("Detail3");
        if (detail3 != null && HealthPercent <= 0.3f)
        {
            Disintegrate detailDisintegrate = detail3.GetComponent<Disintegrate>();
            if (detailDisintegrate != null)
                detailDisintegrate.TriggerDisintegration();
        }

        // Become Alert if not already when attacked
        if (currentState == EnemyState.Idle || currentState == EnemyState.Patrol) {

            // Find all colliders in detection radius
            Collider[] hits = Physics.OverlapSphere(transform.position, DetectionRadius);

            foreach (Collider hit in hits)
            {
                PlayerCharacter player = hit.GetComponent<PlayerCharacter>();
                if (player != null)
                {
                    EnemyAIController aiController = GetComponent<EnemyAIController>();
                    Vector3 direction = (hit.transform.position - transform.position).normalized;
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }
        }

        // Log detailed damage information
        float healthLost = healthBefore - CurrentHealth;
        Debug.Log($"[{gameObject.name}]  Damage Applied: {damage} | Health: {healthBefore:F1} → {CurrentHealth:F1} (-{healthLost:F1}) | {HealthPercent * 100:F1}%");
        
        // Show status indicator
        if (HealthPercent <= 0.2f)
        {
            Debug.LogWarning($"[{gameObject.name}]  CRITICAL HEALTH! ({HealthPercent * 100:F1}%)");
        }
        else if (HealthPercent <= 0.5f)
        {
            Debug.Log($"[{gameObject.name}]  Low Health ({HealthPercent * 100:F1}%)");
        }

        // Check if enemy died from this damage
        if (!IsAlive && !isDead)
        {
            isDead = true;
            Debug.Log($"[{gameObject.name}]  Triggering death sequence...");
            Die();
        }
    }

    /// <summary>
    /// Handles enemy death
    /// </summary>
    private void Die()
    {
        currentState = EnemyState.Dead;

       
        OnDeath();

        
        DropEquippedWeapon();

        
        DisableComponents();

        
        Destroy(gameObject, deathDelay);
    }

    /// <summary>
    /// Disables enemy components to prevent further actions
    /// </summary>
    private void DisableComponents()
    {
        // Disable this script
        enabled = false;

        // Disable AI controller if present
        EnemyAIController aiController = GetComponent<EnemyAIController>();
        if (aiController != null)
        {
            aiController.enabled = false;
        }

        // Disable character controller to prevent movement
        CharacterController charController = GetComponent<CharacterController>();
        if (charController != null)
        {
            charController.enabled = false;
        }

        // Optionally disable collider to make enemy non-interactive
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
    }

    /// <summary>
    /// Drops the weapon the enemy was holding
    /// </summary>
    private void DropEquippedWeapon()
    {
        // TODO: Spawn weapon as pickup in the world
        if (EquippedWeapon != null)
        {
            Debug.Log($"{gameObject.name} dropped {EquippedWeapon.name}");
        }

        UnequipWeapon();
    }

    #region Virtual Event Methods

    /// <summary>
    /// Called when the enemy state changes
    /// </summary>
    protected virtual void OnStateChanged(EnemyState newState)
    {
        Debug.Log($"{gameObject.name} state changed to: {newState}");
        
        // Safely handle animation state changes
        if (enemyAnim != null && enemyAnim.runtimeAnimatorController != null)
        {
            if (newState == EnemyState.Idle || newState == EnemyState.Combat) 
            {
                if (enemyAnim.parameters != null)
                {
                    foreach (var param in enemyAnim.parameters)
                    {
                        if (param.name == "isWalking")
                        {
                            enemyAnim.SetBool("isWalking", false);
                            break;
                        }
                    }
                }
            } 
            else if (newState == EnemyState.Alert || newState == EnemyState.Patrol) 
            {
                if (enemyAnim.parameters != null)
                {
                    foreach (var param in enemyAnim.parameters)
                    {
                        if (param.name == "isWalking")
                        {
                            enemyAnim.SetBool("isWalking", true);
                            break;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Called when the enemy performs an attack
    /// </summary>
    protected virtual void OnAttackPerformed()
    {
        // Play attack animation
        if (enemyAnim != null && enemyAnim.runtimeAnimatorController != null)
        {
            try
            {
                enemyAnim.Play("Attack");
                Debug.Log($"[{gameObject.name}] 🗡️ Playing Attack animation");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[{gameObject.name}] Failed to play Attack animation: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Called when the enemy dies.
    /// GameObject will be destroyed after deathDelay seconds.
    /// </summary>
    protected override void OnDeath()
    {
        base.OnDeath();
        
        Debug.Log($"[{gameObject.name}] smoked bozo - Destroyed in {deathDelay} seconds");
        
        // Play death animation
        if (enemyAnim != null && enemyAnim.runtimeAnimatorController != null)
        {
            try
            {
                enemyAnim.Play("Death");
                Debug.Log($"[{gameObject.name}]  Playing Death animation");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[{gameObject.name}] Failed to play Death animation: {e.Message}");
            }
        }

        // Safely trigger disintegration
        if (partsToDisintegrate.Length > 0)
        {
            foreach (Disintegrate disintegratePart in partsToDisintegrate)
            {
                disintegratePart.TriggerDisintegration();
            }
        }

        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }

        Debug.Log($"[{gameObject.name}] Animator state: Animator={(enemyAnim != null)}, Controller={(enemyAnim?.runtimeAnimatorController != null)}, HasDeathState={HasAnimationState("Death")}");
    }

    #endregion

    #region Debug Gizmos

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // Detection radius (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Combat radius (orange)
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, combatRadius);

        // Attack range (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Line to target
        if (Application.isPlaying && targetPlayer != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetPlayer.position);
        }
    }

    #endregion
}

/// <summary>
/// Enum representing the different states an enemy can be in
/// </summary>
public enum EnemyState
{
    Idle,       // Standing still, looking around
    Patrol,     // Walking a patrol route
    Alert,      // Detected player, moving to investigate
    Combat,     // Actively fighting the player
    Retreat,    // Low health, running away
    Dead        // Enemy has died
}
