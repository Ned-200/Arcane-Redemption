using UnityEngine;

/// <summary>
/// Enemy character that inherits stats and hand slots from BaseCharacter
/// </summary>
public class EnemyCharacter : BaseCharacter
{
    [Header("Enemy Settings")]
    [SerializeField] private GameObject defaultWeapon;
    [SerializeField] private GameObject defaultShield;

    [Header("AI Settings")]
    [SerializeField] private float detectionRadius = 15f;
    [SerializeField] private float combatRadius = 3f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Combat")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 2f;

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
        base.Awake(); // Initialize stats and hand slots

        // Equip default equipment
        EquipDefaultItems();
    }

    protected override void Update()
    {
        base.Update(); // Handle stat regeneration

        if (isDead) return;

        // AI behavior handled by EnemyAIController
    }

    private void EquipDefaultItems()
    {
        if (defaultWeapon != null)
        {
            EquipRightHand(defaultWeapon);
        }

        if (defaultShield != null)
        {
            EquipLeftHand(defaultShield);
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
        float healthPercent = currentHealth / maxHealth;
        return healthPercent <= healthThreshold;
    }

    /// <summary>
    /// Gets the current health as a percentage (0-1)
    /// </summary>
    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }

    /// <summary>
    /// Checks if the enemy is critically wounded
    /// </summary>
    public bool IsCriticallyWounded()
    {
        return currentHealth <= (maxHealth * 0.3f);
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
    /// Performs the attack
    /// </summary>
    private void PerformAttack()
    {
        // Deal damage to player
        PlayerCharacter player = targetPlayer.GetComponent<PlayerCharacter>();
        if (player != null)
        {
            // TODO: Implement damage system
            Debug.Log($"{gameObject.name} attacked player for {attackDamage} damage!");
        }

        OnAttackPerformed();
    }

    /// <summary>
    /// Takes damage from the player or environment
    /// </summary>
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);

        Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f && !isDead)
        {
            Die();
        }

        OnDamageTaken(damage);
    }

    /// <summary>
    /// Handles enemy death
    /// </summary>
    private void Die()
    {
        isDead = true;
        currentState = EnemyState.Dead;

        // Drop equipped items
        DropEquippedItems();

        OnDeath();

        // Disable components
        enabled = false;

        // TODO: Play death animation
        // TODO: Spawn loot
        // TODO: Disable collider after animation
    }

    /// <summary>
    /// Drops items the enemy was holding
    /// </summary>
    private void DropEquippedItems()
    {
        // TODO: Spawn items as pickups in the world
        if (RightHandItem != null)
        {
            Debug.Log($"{gameObject.name} dropped {RightHandItem.name}");
        }

        if (LeftHandItem != null)
        {
            Debug.Log($"{gameObject.name} dropped {LeftHandItem.name}");
        }

        UnequipAll();
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
    protected virtual void OnDamageTaken(float damage)
    {
        // Override for custom behavior (play hurt sound, visual effects, etc.)
    }

    /// <summary>
    /// Called when the enemy dies
    /// </summary>
    protected virtual void OnDeath()
    {
        Debug.Log($"{gameObject.name} has died!");
        // Override for custom behavior (death animation, sound, particle effects, etc.)
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
