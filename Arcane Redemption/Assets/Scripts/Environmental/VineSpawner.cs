using UnityEngine;

/// <summary>
/// Manages vine spawning logic - spawn position calculation, rotation, and instantiation.
/// Used by TreeBoss to spawn vine attacks at strategic locations.
/// </summary>
public class VineSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject vineAttackPrefab;
    [SerializeField] private Transform spawnOrigin;

    [Header("Position Settings")]
    [SerializeField] private VineSpawnMode spawnMode = VineSpawnMode.AtOrigin;
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;
    [SerializeField] private bool alignToGround = true;
    [SerializeField] private LayerMask groundLayer;

    [Header("Pattern Settings")]
    [SerializeField] private VineSpawnPattern pattern = VineSpawnPattern.Single;
    [SerializeField] private int patternCount = 3;
    [SerializeField] private float patternRadius = 2f;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    public GameObject VineAttackPrefab => vineAttackPrefab;

    private void Awake()
    {
        if (spawnOrigin == null)
        {
            spawnOrigin = transform;
        }

        if (vineAttackPrefab == null)
        {
            Debug.LogError($"[VineSpawner] {gameObject.name} - Vine attack prefab not assigned!");
        }
    }

    public GameObject SpawnVine(Vector3 targetPosition)
    {
        if (vineAttackPrefab == null)
        {
            Debug.LogWarning($"[VineSpawner] Cannot spawn vine - prefab not assigned!");
            return null;
        }

        Vector3 spawnPosition = CalculateSpawnPosition(targetPosition);
        Quaternion spawnRotation = CalculateSpawnRotation();

        GameObject vineInstance = Instantiate(vineAttackPrefab, spawnPosition, spawnRotation);

        Debug.Log($"[VineSpawner] 🌿 Spawned vine at {spawnPosition}");

        return vineInstance;
    }

    public GameObject[] SpawnVinePattern(Vector3 centerPosition)
    {
        if (vineAttackPrefab == null) return new GameObject[0];

        switch (pattern)
        {
            case VineSpawnPattern.Single:
                return new GameObject[] { SpawnVine(centerPosition) };

            case VineSpawnPattern.Circle:
                return SpawnCirclePattern(centerPosition);

            case VineSpawnPattern.Line:
                return SpawnLinePattern(centerPosition);

            default:
                return new GameObject[] { SpawnVine(centerPosition) };
        }
    }

    private GameObject[] SpawnCirclePattern(Vector3 center)
    {
        GameObject[] vines = new GameObject[patternCount];
        float angleStep = 360f / patternCount;

        for (int i = 0; i < patternCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * patternRadius;
            Vector3 spawnPos = center + offset;

            vines[i] = SpawnVine(spawnPos);
        }

        return vines;
    }

    private GameObject[] SpawnLinePattern(Vector3 center)
    {
        GameObject[] vines = new GameObject[patternCount];
        Vector3 direction = spawnOrigin.forward;

        for (int i = 0; i < patternCount; i++)
        {
            float offset = (i - (patternCount - 1) / 2f) * patternRadius;
            Vector3 spawnPos = center + direction * offset;

            vines[i] = SpawnVine(spawnPos);
        }

        return vines;
    }

    private Vector3 CalculateSpawnPosition(Vector3 targetPosition)
    {
        Vector3 basePosition;

        switch (spawnMode)
        {
            case VineSpawnMode.AtOrigin:
                basePosition = spawnOrigin.position;
                break;

            case VineSpawnMode.AtTarget:
                basePosition = targetPosition;
                break;

            case VineSpawnMode.BetweenOriginAndTarget:
                basePosition = Vector3.Lerp(spawnOrigin.position, targetPosition, 0.5f);
                break;

            default:
                basePosition = spawnOrigin.position;
                break;
        }

        basePosition += spawnOffset;

        if (alignToGround)
        {
            basePosition.y = GetGroundHeight(basePosition);
        }

        return basePosition;
    }

    private float GetGroundHeight(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out hit, 20f, groundLayer))
        {
            return hit.point.y;
        }

        return 0f;
    }

    private Quaternion CalculateSpawnRotation()
    {
        return Quaternion.identity;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        if (spawnOrigin == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spawnOrigin.position, 0.5f);

        if (pattern == VineSpawnPattern.Circle)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(spawnOrigin.position, patternRadius);
        }
    }
}

#region Enums

public enum VineSpawnMode
{
    AtOrigin,
    AtTarget,
    BetweenOriginAndTarget
}

public enum VineSpawnPattern
{
    Single,
    Circle,
    Line
}

#endregion