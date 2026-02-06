using UnityEngine;

/// <summary>
/// Controls AI behavior for EnemyCharacter
/// Handles detection, movement, and combat logic
/// </summary>
[RequireComponent(typeof(EnemyCharacter))]
public class EnemyAIController : MonoBehaviour
{
    private EnemyCharacter enemy;
    private Transform targetPlayer;
    private float stateTimer;

    [Header("Patrol Settings")]
    [SerializeField] private bool usePatrol = false;
    [SerializeField] private Transform[] patrolPoints;
    private int currentPatrolIndex = 0;

    [Header("Retreat Settings")]
    [SerializeField] private float retreatHealthPercent = 0.2f;

    private void Awake()
    {
        enemy = GetComponent<EnemyCharacter>();
    }

    private void Update()
    {
        if (enemy.IsDead) return;

        // Update AI based on current state
        switch (enemy.CurrentState)
        {
            case EnemyState.Idle:
                HandleIdleState();
                break;

            case EnemyState.Patrol:
                HandlePatrolState();
                break;

            case EnemyState.Alert:
                HandleAlertState();
                break;

            case EnemyState.Combat:
                HandleCombatState();
                break;

            case EnemyState.Retreat:
                HandleRetreatState();
                break;
        }

        // Check for player detection
        DetectPlayer();

        // Check for retreat condition
        CheckRetreatCondition();
    }

    #region Detection

    /// <summary>
    /// Detects nearby players and updates enemy state
    /// </summary>
    private void DetectPlayer()
    {
        // Find all colliders in detection radius
        Collider[] hits = Physics.OverlapSphere(transform.position, enemy.DetectionRadius);

        foreach (Collider hit in hits)
        {
            PlayerCharacter player = hit.GetComponent<PlayerCharacter>();
            if (player != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, hit.transform.position);

                // Check if player is within combat radius
                if (distanceToPlayer <= enemy.CombatRadius)
                {
                    if (enemy.CurrentState != EnemyState.Combat && enemy.CurrentState != EnemyState.Retreat)
                    {
                        EnterCombat(hit.transform);
                    }
                }
                // Check if player is within detection radius
                else if (distanceToPlayer <= enemy.DetectionRadius)
                {
                    if (enemy.CurrentState == EnemyState.Idle || enemy.CurrentState == EnemyState.Patrol)
                    {
                        EnterAlert(hit.transform);
                    }
                }

                return; // Found player, no need to check others
            }
        }

        // No player found
        if (enemy.CurrentState == EnemyState.Alert || enemy.CurrentState == EnemyState.Combat)
        {
            LoseTarget();
        }
    }

    private void EnterCombat(Transform player)
    {
        targetPlayer = player;
        enemy.SetTarget(player);
        enemy.SetState(EnemyState.Combat);
        Debug.Log($"{gameObject.name} entered combat with player!");
    }

    private void EnterAlert(Transform player)
    {
        targetPlayer = player;
        enemy.SetTarget(player);
        enemy.SetState(EnemyState.Alert);
        Debug.Log($"{gameObject.name} detected player!");
    }

    private void LoseTarget()
    {
        targetPlayer = null;
        enemy.SetTarget(null);
        
        if (usePatrol && patrolPoints.Length > 0)
        {
            enemy.SetState(EnemyState.Patrol);
        }
        else
        {
            enemy.SetState(EnemyState.Idle);
        }

        Debug.Log($"{gameObject.name} lost sight of player");
    }

    private void CheckRetreatCondition()
    {
        if (enemy.CurrentState == EnemyState.Dead || enemy.CurrentState == EnemyState.Retreat)
        {
            return;
        }

        // Use health percent directly since ShouldRetreat is not defined
        if (enemy.GetHealthPercent() <= retreatHealthPercent)
        {
            enemy.SetState(EnemyState.Retreat);
            Debug.Log($"{gameObject.name} is retreating!");
        }
    }

    #endregion

    #region State Handlers

    private void HandleIdleState()
    {
        // Just stand still and look around
        // TODO: Add random rotation or idle animation
    }

    private void HandlePatrolState()
    {
        if (!usePatrol || patrolPoints.Length == 0)
        {
            enemy.SetState(EnemyState.Idle);
            return;
        }

        Transform targetPoint = patrolPoints[currentPatrolIndex];
        MoveTowards(targetPoint.position);

        // Check if reached patrol point
        if (Vector3.Distance(transform.position, targetPoint.position) < 0.5f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            stateTimer = 0f;
        }
    }

    private void HandleAlertState()
    {
        if (targetPlayer == null)
        {
            LoseTarget();
            return;
        }

        // Move towards last known player position
        MoveTowards(targetPlayer.position);
        LookAt(targetPlayer.position);
    }

    private void HandleCombatState()
    {
        if (targetPlayer == null)
        {
            LoseTarget();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

        // If within attack range, attack
        if (distanceToPlayer <= enemy.AttackRange)
        {
            LookAt(targetPlayer.position);
            enemy.TryAttack();
        }
        // If too far, move closer
        else if (distanceToPlayer <= enemy.CombatRadius)
        {
            MoveTowards(targetPlayer.position);
            LookAt(targetPlayer.position);
        }
        // If player escaped combat radius, switch to alert
        else
        {
            enemy.SetState(EnemyState.Alert);
        }
    }

    private void HandleRetreatState()
    {
        if (targetPlayer == null)
        {
            enemy.SetState(EnemyState.Idle);
            return;
        }

        // Run away from player
        Vector3 retreatDirection = (transform.position - targetPlayer.position).normalized;
        Vector3 retreatPosition = transform.position + retreatDirection * 10f;
        
        MoveTowards(retreatPosition);
        LookAt(targetPlayer.position); // Keep eyes on player while retreating
    }

    #endregion

    #region Movement

    /// <summary>
    /// Moves the enemy towards a target position
    /// </summary>
    private void MoveTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f; // Keep movement horizontal

        transform.position += direction * enemy.MoveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// Rotates the enemy to look at a target position
    /// </summary>
    private void LookAt(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f; // Keep rotation horizontal

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, enemy.RotationSpeed * Time.deltaTime);
        }
    }

    #endregion
}
