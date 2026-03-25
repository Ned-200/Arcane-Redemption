using UnityEngine;

public class TreeBossProjectile : ProjectileBase
{
    [Header("Tracking Settings")]
    [SerializeField] private float trackingDuration = 6f;
    [SerializeField] private float trackingStrength = 15f;
    [SerializeField] private float maxTurnRate = 720f;
    [SerializeField] private float accelerationRate = 20f;

    [Header("Center Mass Tracking")]
    [SerializeField] private Vector3 centerMassOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private bool useBoundsCenter = true;
    [SerializeField] private bool trackImmediately = true;
    [SerializeField] private bool predictiveTracking = true;

    [Header("Advanced Tracking")]
    [SerializeField] private float predictionMultiplier = 0.3f;
    [SerializeField] private float minDistanceForPrediction = 10f;
    [SerializeField] private bool useAggressiveTracking = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    [SerializeField] private bool showTrackingGizmos = true;

    private Transform target;
    private Collider targetCollider;
    private float trackingTimer;
    private bool isTracking;
    private float projectileSpeed;
    private bool isInitialized;

    private Vector3 lastTargetPosition;
    private Vector3 targetVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.None;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    public void Initialize(float projectileDamage, BaseCharacter projectileOwner, float speed, Transform targetTransform)
    {
        target = targetTransform;
        projectileSpeed = speed;
        trackingTimer = 0f;
        isTracking = trackImmediately;
        isInitialized = false;

        if (target != null)
        {
            lastTargetPosition = target.position;
            targetVelocity = Vector3.zero;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[TreeBossProjectile] Initialized - Damage: {projectileDamage}, Speed: {speed}, Target: {target?.name ?? "NULL"}");
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (rb == null)
        {
            Debug.LogError("[TreeBossProjectile] No Rigidbody found!");
            return;
        }

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.None;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (target != null && useBoundsCenter)
        {
            targetCollider = target.GetComponent<Collider>();
            if (targetCollider == null)
            {
                targetCollider = target.GetComponentInChildren<Collider>();
            }

            if (showDebugLogs)
            {
                Debug.Log($"[TreeBossProjectile] Target collider: {targetCollider?.name ?? "NULL"}");
            }
        }

        base.Initialize(projectileDamage, projectileOwner, speed);

        if (trackImmediately && target != null && rb != null)
        {
            Vector3 aimPosition = GetPredictedTargetPosition();
            Vector3 directionToTarget = (aimPosition - transform.position).normalized;

            rb.linearVelocity = directionToTarget * speed;
            transform.rotation = Quaternion.LookRotation(directionToTarget);
            
            if (showDebugLogs)
            {
                Debug.Log($"[TreeBossProjectile] Initial aim at {target.name} - Position: {aimPosition}");
            }
        }

        isInitialized = true;
    }

    private void FixedUpdate()
    {
        if (hasHit || !isInitialized) return;

        if (target != null)
        {
            UpdateTargetVelocity();
        }

        if (target != null && trackingTimer < trackingDuration)
        {
            if (useAggressiveTracking)
            {
                PerformAggressiveTracking();
            }
            else
            {
                PerformSmoothTracking();
            }

            trackingTimer += Time.fixedDeltaTime;
        }
        else if (trackingTimer >= trackingDuration && isTracking)
        {
            isTracking = false;
            if (showDebugLogs)
            {
                Debug.Log($"[TreeBossProjectile] Tracking expired after {trackingDuration}s");
            }
        }
    }

    private void UpdateTargetVelocity()
    {
        if (target == null) return;

        Vector3 currentPosition = GetTargetCenterMass();
        targetVelocity = (currentPosition - lastTargetPosition) / Time.fixedDeltaTime;
        lastTargetPosition = currentPosition;
    }

    private Vector3 GetPredictedTargetPosition()
    {
        if (target == null) return Vector3.zero;

        Vector3 centerMass = GetTargetCenterMass();

        if (!predictiveTracking)
        {
            return centerMass;
        }

        float distanceToTarget = Vector3.Distance(transform.position, centerMass);
        if (distanceToTarget < minDistanceForPrediction)
        {
            return centerMass;
        }

        float timeToTarget = distanceToTarget / projectileSpeed;

        Vector3 predictedPosition = centerMass + (targetVelocity * timeToTarget * predictionMultiplier);

        return predictedPosition;
    }

    private Vector3 GetTargetCenterMass()
    {
        if (target == null) return Vector3.zero;

        if (useBoundsCenter && targetCollider != null)
        {
            return targetCollider.bounds.center;
        }

        return target.position + centerMassOffset;
    }

    private void PerformAggressiveTracking()
    {
        if (rb == null || target == null) return;

        Vector3 aimPosition = GetPredictedTargetPosition();
        Vector3 directionToTarget = (aimPosition - transform.position).normalized;

        Vector3 desiredVelocity = directionToTarget * projectileSpeed;

        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, desiredVelocity, trackingStrength * Time.fixedDeltaTime);

        rb.linearVelocity = rb.linearVelocity.normalized * projectileSpeed;

        if (rb.linearVelocity != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
        }

        if (showDebugLogs && Time.frameCount % 60 == 0)
        {
            float distanceToTarget = Vector3.Distance(transform.position, aimPosition);
            float velocityMagnitude = rb.linearVelocity.magnitude;
            Debug.Log($"[TreeBossProjectile] AGGRESSIVE Track - Distance: {distanceToTarget:F1}m, Speed: {velocityMagnitude:F1}, Target Velocity: {targetVelocity.magnitude:F1}");
        }
    }

    private void PerformSmoothTracking()
    {
        if (rb == null || target == null) return;

        Vector3 aimPosition = GetPredictedTargetPosition();
        Vector3 directionToTarget = (aimPosition - transform.position).normalized;

        Vector3 desiredVelocity = directionToTarget * projectileSpeed;

        Vector3 newVelocity = Vector3.RotateTowards(
            rb.linearVelocity,
            desiredVelocity,
            maxTurnRate * Mathf.Deg2Rad * Time.fixedDeltaTime,
            0f
        );

        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, newVelocity, trackingStrength * Time.fixedDeltaTime);
        
        rb.linearVelocity = rb.linearVelocity.normalized * projectileSpeed;

        if (rb.linearVelocity != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
        }
    }

    protected override void OnTargetHit(BaseCharacter target)
    {
        base.OnTargetHit(target);
        
        if (showDebugLogs)
        {
            Debug.Log($"[TreeBossProjectile] 💥 Hit {target.gameObject.name} for {damage} damage!");
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !showTrackingGizmos) return;

        Gizmos.color = isTracking ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        if (target != null)
        {
            Vector3 centerMass = GetTargetCenterMass();
            Vector3 predictedPosition = GetPredictedTargetPosition();

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, centerMass);

            if (predictiveTracking)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, predictedPosition);
                
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(predictedPosition, 0.5f);
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(centerMass, 0.3f);

            if (targetVelocity.magnitude > 0.1f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(centerMass, targetVelocity.normalized * 2f);
            }

            if (rb != null && rb.linearVelocity != Vector3.zero)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 3f);
            }

            #if UNITY_EDITOR
            if (isTracking)
            {
                float distanceToTarget = Vector3.Distance(transform.position, centerMass);
                float distanceToPredicted = Vector3.Distance(transform.position, predictedPosition);
                string mode = useAggressiveTracking ? "AGGRESSIVE" : "SMOOTH";
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up, 
                    $"{mode} TRACKING\n" +
                    $"Distance: {distanceToTarget:F1}m\n" +
                    $"Predicted: {distanceToPredicted:F1}m\n" +
                    $"Time: {trackingTimer:F1}/{trackingDuration}s\n" +
                    $"Target Speed: {targetVelocity.magnitude:F1}m/s"
                );
            }
            #endif
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Don't hit the boss that fired this projectile
        BaseCharacter hitCharacter = other.GetComponent<BaseCharacter>();
        if (hitCharacter == owner)
        {
            return;
        }

        // Damage any BaseCharacter (player) regardless of layer
        if (hitCharacter != null)
        {
            hasHit = true;
            hitCharacter.TakeDamage(damage);
            OnTargetHit(hitCharacter);

            SpawnImpactEffect();
            PlayImpactSound();

            if (destroyOnImpact)
            {
                Destroy(gameObject);
            }
            return;
        }

        // Fall back to base for environment hits (rocks, walls, etc.)
        base.OnTriggerEnter(other);
    }
}