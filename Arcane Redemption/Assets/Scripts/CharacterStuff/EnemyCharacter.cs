using UnityEngine;

/// <summary>
/// Enemy character that inherits stats and weapon slot from BaseCharacter
/// </summary>
public class EnemyCharacter : BaseCharacter
{
    [Header("Enemy Settings")]
    [SerializeField] private GameObject defaultWeapon;

    [Header("AI Settings")]
    [SerializeField] private float detectionRadius = 15f;
    [SerializeField] private float combatRadius = 3f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Combat")]
    [SerializeField] private float attackDamage = 5f;
    [SerializeField] private float attackCooldown = 2f;

    [Header("Death Settings")]
    [SerializeField] private float deathDelay = 2f;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

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

    protected override void Awake()
    {
        base.Awake(); // Initialize stats and weapon slot

        // Equip default weapon
        EquipDefaultWeapon();
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

        OnAttackPerformed();
    }

    /// <summary>
    /// Takes damage from the player or environment
    /// Overrides base TakeDamage to add enemy-specific logging and behavior
    /// </summary>
    public override void TakeDamage(float damage)
    {
        if (isDead) return;

        // Store health before damage
        float healthBefore = CurrentHealth;

        // Call base class TakeDamage which handles health reduction and events
        base.TakeDamage(damage);

        // Log detailed damage information
        
        Debug.Log($"Damage Taken: {damage}");
        
        
        // Show status indicator
        if (HealthPercent <= 0.2f)
        {
            Debug.LogWarning($"[{gameObject.name}] CRITICAL HEALTH!");
        }
        else if (HealthPercent <= 0.5f)
        {
            Debug.Log($"[{gameObject.name}] Low Health");
        }

        // Check if enemy died from this damage
        if (!IsAlive && !isDead)
        {
            Die();
        }
    }

    /// <summary>
    /// Handles enemy death
    /// </summary>
    private void Die()
    {
        isDead = true;
        currentState = EnemyState.Dead;

        // Drop equipped weapon
        DropEquippedWeapon();

        // Disable components immediately
        DisableComponents();

        // Call death event (animations, sounds, VFX)
        OnDeath();

        // Destroy the GameObject after a delay
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
        // Override for custom behavior (animations, sounds, etc.)
    }

    /// <summary>
    /// Called when the enemy performs an attack
    /// </summary>
    protected virtual void OnAttackPerformed()
    {
        // Override for custom behavior (play attack animation, sound, etc.)
    }

    /// <summary>
    /// Called when the enemy takes damage
    /// </summary>
    protected override void OnDamageTaken(float damage)
    {
        base.OnDamageTaken(damage);
        // Add enemy-specific damage response (play hurt sound, visual effects, etc.)
    }

    /// <summary>
    /// Called when the enemy dies.
    /// GameObject will be destroyed after deathDelay seconds.
    /// </summary>
    protected override void OnDeath()
    {
        base.OnDeath();
        
        Debug.LogError($" [{gameObject.name}] smoked bozo - Destroyed in {deathDelay} seconds");
        
        // TODO: Play death animation 
        // TODO: Play death sound
        // TODO: Spawn particle effects (blood, dissolve effect, etc.)
        // TODO: Spawn loot drops
        // TODO: Add score/experience to player
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
