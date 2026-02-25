using UnityEngine;

/// <summary>
/// Controls AI behavior for EnemyCharacter
/// Handles detection, movement, and combat logic with line-of-sight checking
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

    [Header("Line of Sight Settings")]
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private LayerMask obstructionLayers = -1;
    [SerializeField] private float eyeHeight = 1.5f;
    [SerializeField] private float sightAngle = 90f;
    [SerializeField] private bool showLineOfSightDebug = true;

    // Line of sight tracking
    private bool hasLineOfSight = false;
    private Vector3 lastKnownPlayerPosition;
    private float timeSinceLastSighting = 0f;
    [SerializeField] private float memoryDuration = 3f;

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

        // Update line of sight timer
        if (!hasLineOfSight)
        {
            timeSinceLastSighting += Time.deltaTime;
        }
        else
        {
            timeSinceLastSighting = 0f;
        }
    }

    #region Detection

    /// <summary>
    /// Detects nearby players and updates enemy state with line-of-sight checking
    /// </summary>
    private void DetectPlayer()
    {
        // Reset line of sight flag
        hasLineOfSight = false;

        // Find all colliders in detection radius
        Collider[] hits = Physics.OverlapSphere(transform.position, enemy.DetectionRadius);

        foreach (Collider hit in hits)
        {
            PlayerCharacter player = hit.GetComponent<PlayerCharacter>();
            if (player != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, hit.transform.position);

                // Check line of sight if required
                if (requireLineOfSight)
                {
                    if (!HasLineOfSightToTarget(hit.transform))
                    {
                        // No line of sight - check if we recently saw them
                        if (timeSinceLastSighting < memoryDuration && enemy.CurrentState != EnemyState.Idle)
                        {
                            // Continue tracking last known position
                            if (showLineOfSightDebug)
                            {
                                Debug.Log($"[{gameObject.name}] Lost sight of player, tracking last known position for {memoryDuration - timeSinceLastSighting:F1}s");
                            }
                            continue;
                        }
                        else
                        {
                            // Lost them completely
                            if (enemy.CurrentState == EnemyState.Alert || enemy.CurrentState == EnemyState.Combat)
                            {
                                LoseTarget();
                            }
                            continue;
                        }
                    }
                }

                // We can see the player!
                hasLineOfSight = true;
                lastKnownPlayerPosition = hit.transform.position;

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

        // No player found in radius
        if (enemy.CurrentState == EnemyState.Alert || enemy.CurrentState == EnemyState.Combat)
        {
            // Check if we should lose target based on memory duration
            if (timeSinceLastSighting >= memoryDuration)
            {
                LoseTarget();
            }
        }
    }

    /// <summary>
    /// Checks if enemy has line of sight to target using raycast
    /// </summary>
    private bool HasLineOfSightToTarget(Transform target)
    {
        if (target == null) return false;

        // Calculate eye position
        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Vector3 targetPosition = target.position + Vector3.up * eyeHeight;

        // Calculate direction to target
        Vector3 directionToTarget = (targetPosition - eyePosition).normalized;

        // Check if target is within sight angle
        float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
        if (angleToTarget > sightAngle / 2f)
        {
            if (showLineOfSightDebug)
            {
                Debug.Log($"[{gameObject.name}] Target outside sight angle: {angleToTarget:F1}° (max: {sightAngle / 2f}°)");
            }
            return false;
        }

        // Raycast to check for obstructions
        float distanceToTarget = Vector3.Distance(eyePosition, targetPosition);
        RaycastHit hit;

        if (Physics.Raycast(eyePosition, directionToTarget, out hit, distanceToTarget, obstructionLayers))
        {
            // Check if we hit the target or something else
            if (hit.transform == target || hit.transform.root == target)
            {
                // Hit the target directly
                if (showLineOfSightDebug)
                {
                    Debug.DrawLine(eyePosition, hit.point, Color.green);
                }
                return true;
            }
            else
            {
                // Hit an obstruction
                if (showLineOfSightDebug)
                {
                    Debug.DrawLine(eyePosition, hit.point, Color.red);
                    Debug.Log($"[{gameObject.name}] Line of sight blocked by: {hit.collider.gameObject.name} (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
                }
                return false;
            }
        }

        // No obstruction, clear line of sight
        if (showLineOfSightDebug)
        {
            Debug.DrawLine(eyePosition, targetPosition, Color.green);
        }
        return true;
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
            // Move to last known position if we have one
            if (lastKnownPlayerPosition != Vector3.zero)
            {
                MoveTowards(lastKnownPlayerPosition);
                LookAt(lastKnownPlayerPosition);
                
                // If we reached last known position, give up
                if (Vector3.Distance(transform.position, lastKnownPlayerPosition) < 1f)
                {
                    LoseTarget();
                }
            }
            else
            {
                LoseTarget();
            }
            return;
        }

        // Move towards player or last known position
        Vector3 targetPosition = hasLineOfSight ? targetPlayer.position : lastKnownPlayerPosition;
        MoveTowards(targetPosition);
        LookAt(targetPosition);
    }

    private void HandleCombatState()
    {
        if (targetPlayer == null)
        {
            LoseTarget();
            return;
        }

        // Use last known position if we lost sight
        Vector3 targetPosition = hasLineOfSight ? targetPlayer.position : lastKnownPlayerPosition;
        float distanceToPlayer = Vector3.Distance(transform.position, targetPosition);

        // If within attack range and can see target, attack
        if (distanceToPlayer <= enemy.AttackRange && hasLineOfSight)
        {
            LookAt(targetPlayer.position);
            enemy.TryAttack();
        }
        // If too far or can't see, move closer
        else if (distanceToPlayer <= enemy.CombatRadius)
        {
            MoveTowards(targetPosition);
            LookAt(targetPosition);
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

        // Run away from player (or last known position)
        Vector3 threatPosition = hasLineOfSight ? targetPlayer.position : lastKnownPlayerPosition;
        Vector3 retreatDirection = (transform.position - threatPosition).normalized;
        Vector3 retreatPosition = transform.position + retreatDirection * 10f;
        
        MoveTowards(retreatPosition);
        LookAt(threatPosition); // Keep eyes on threat while retreating
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

    #region Debug Visualization

    private void OnDrawGizmosSelected()
    {
        if (!showLineOfSightDebug) return;

        // Draw sight cone
        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        
        // Draw forward direction
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(eyePosition, eyePosition + transform.forward * enemy.DetectionRadius);

        // Draw sight angle cone
        Vector3 leftBoundary = Quaternion.Euler(0, -sightAngle / 2f, 0) * transform.forward * enemy.DetectionRadius;
        Vector3 rightBoundary = Quaternion.Euler(0, sightAngle / 2f, 0) * transform.forward * enemy.DetectionRadius;

        Gizmos.color = hasLineOfSight ? Color.green : Color.yellow;
        Gizmos.DrawLine(eyePosition, eyePosition + leftBoundary);
        Gizmos.DrawLine(eyePosition, eyePosition + rightBoundary);

        // Draw arc for sight cone
        Vector3 previousPoint = eyePosition + leftBoundary;
        for (int i = 1; i <= 20; i++)
        {
            float angle = -sightAngle / 2f + (sightAngle * i / 20f);
            Vector3 point = eyePosition + Quaternion.Euler(0, angle, 0) * transform.forward * enemy.DetectionRadius;
            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }

        // Draw last known player position
        if (lastKnownPlayerPosition != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.5f);
            Gizmos.DrawLine(transform.position, lastKnownPlayerPosition);
        }

        // Draw eye height indicator
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(eyePosition, 0.2f);
    }

    #endregion
}
