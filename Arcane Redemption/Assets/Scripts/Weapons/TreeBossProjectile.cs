using UnityEngine;

public class TreeBossProjectile : ProjectileBase
{
    [Header("Tracking Settings")]
    [SerializeField] private float trackingDuration = 6f;
    [SerializeField] private float trackingRadius = 999f;
    [SerializeField] private float trackingStrength = 10f;
    [SerializeField] private float maxTurnRate = 720f;

    [Header("Center Mass Tracking")]
    [SerializeField] private Vector3 centerMassOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private bool useBoundsCenter = true;
    [SerializeField] private bool trackImmediately = true;

    private Transform target;
    private Collider targetCollider;
    private float trackingTimer;
    private bool isTracking;
    private float projectileSpeed;
    private bool isInitialized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.None;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    public void Initialize(float projectileDamage, BaseCharacter projectileOwner, float speed, Transform targetTransform)
    {
        target = targetTransform;
        projectileSpeed = speed;
        trackingTimer = 0f;
        isTracking = false;
        isInitialized = false;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (rb == null) return;

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.None;

        if (target != null && useBoundsCenter)
        {
            targetCollider = target.GetComponent<Collider>();
            if (targetCollider == null)
            {
                targetCollider = target.GetComponentInChildren<Collider>();
            }
        }

        if (trackImmediately && target != null && rb != null)
        {
            Vector3 centerMass = GetTargetCenterMass();
            Vector3 directionToCenterMass = (centerMass - transform.position).normalized;

            rb.linearVelocity = directionToCenterMass * speed;
            transform.rotation = Quaternion.LookRotation(directionToCenterMass);
        }

        base.Initialize(projectileDamage, projectileOwner, speed);

        if (trackImmediately && target != null && rb != null)
        {
            Vector3 centerMass = GetTargetCenterMass();
            Vector3 directionToCenterMass = (centerMass - transform.position).normalized;

            rb.linearVelocity = directionToCenterMass * speed;
            transform.rotation = Quaternion.LookRotation(directionToCenterMass);
        }

        isInitialized = true;
    }

    private void Update()
    {
        if (hasHit || target == null || !isInitialized) return;

        UpdateTracking();
    }

    private void FixedUpdate()
    {
        if (hasHit || target == null || !isInitialized) return;

        if (isTracking && trackingTimer < trackingDuration)
        {
            PerformTrackingFixed();
        }
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

    private void UpdateTracking()
    {
        Vector3 centerMass = GetTargetCenterMass();
        float distanceToTarget = Vector3.Distance(transform.position, centerMass);

        if (distanceToTarget <= trackingRadius)
        {
            if (!isTracking)
            {
                StartTracking();
            }

            trackingTimer += Time.deltaTime;

            if (trackingTimer >= trackingDuration)
            {
                StopTracking();
            }
        }
    }

    private void StartTracking()
    {
        isTracking = true;
        trackingTimer = 0f;
    }

    private void StopTracking()
    {
        isTracking = false;
    }

    private void PerformTrackingFixed()
    {
        if (rb == null || target == null) return;

        Vector3 centerMass = GetTargetCenterMass();
        Vector3 directionToTarget = (centerMass - transform.position).normalized;
        Vector3 desiredVelocity = directionToTarget * projectileSpeed;

        rb.linearVelocity = desiredVelocity;

        if (rb.linearVelocity != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
        }
    }

    private void PerformTracking()
    {
        if (rb == null) return;

        Vector3 centerMass = GetTargetCenterMass();
        Vector3 directionToTarget = (centerMass - transform.position).normalized;
        Vector3 desiredVelocity = directionToTarget * projectileSpeed;

        Vector3 newVelocity = Vector3.RotateTowards(
            rb.linearVelocity,
            desiredVelocity,
            maxTurnRate * Mathf.Deg2Rad * Time.deltaTime,
            0f
        );

        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, newVelocity, trackingStrength * Time.deltaTime);
        rb.linearVelocity = rb.linearVelocity.normalized * projectileSpeed;

        if (rb.linearVelocity != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
        }
    }

    protected override void OnTargetHit(BaseCharacter target)
    {
        base.OnTargetHit(target);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = isTracking ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Mathf.Min(trackingRadius, 50f));

        if (target != null)
        {
            Vector3 centerMass = GetTargetCenterMass();

            Gizmos.color = isTracking ? Color.green : Color.gray;
            Gizmos.DrawLine(transform.position, centerMass);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(centerMass, 0.3f);

            if (isTracking)
            {
                Gizmos.color = Color.green;
                Vector3 direction = (centerMass - transform.position).normalized;
                Gizmos.DrawRay(transform.position, direction * 2f);
            }

            if (rb != null && rb.linearVelocity != Vector3.zero)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 3f);
            }
        }
    }
}