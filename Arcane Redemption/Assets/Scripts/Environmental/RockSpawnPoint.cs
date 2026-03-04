using UnityEngine;

public class RockSpawnPoint : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private float spawnHeight = 20f;
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private bool spawnRockOnStart = true;

    [Header("Visual")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.red;

    private GameObject currentRock;

    public Vector3 SpawnPosition => transform.position + Vector3.up * spawnHeight;
    public bool HasActiveRock => currentRock != null;
    public GameObject CurrentRock => currentRock;

    private void Start()
    {
        if (spawnRockOnStart)
        {
            SpawnRock();
        }
    }

    public bool IsPositionUnderRock(Vector3 position, float tolerance = 2f)
    {
        float horizontalDistance = Vector3.Distance(
            new Vector3(position.x, 0f, position.z),
            new Vector3(transform.position.x, 0f, transform.position.z)
        );

        return horizontalDistance <= tolerance;
    }

    public GameObject SpawnRock()
    {
        if (rockPrefab == null)
        {
            Debug.LogError($"[RockSpawnPoint] {gameObject.name} has no rock prefab assigned!");
            return null;
        }

        if (currentRock != null)
        {
            Debug.LogWarning($"[RockSpawnPoint] {gameObject.name} already has an active rock - destroying old one");
            Destroy(currentRock);
        }

        currentRock = Instantiate(rockPrefab, SpawnPosition, Quaternion.identity);
        currentRock.name = $"{rockPrefab.name}_{gameObject.name}";

        FallingRock rockScript = currentRock.GetComponent<FallingRock>();
        if (rockScript != null)
        {
            Debug.Log($"[RockSpawnPoint] Spawned rock at {gameObject.name} - awaiting player trigger");
        }

        return currentRock;
    }

    public void RespawnRock(float delay = 0f)
    {
        if (delay > 0f)
        {
            Invoke(nameof(SpawnRock), delay);
        }
        else
        {
            SpawnRock();
        }
    }

    public FallingRock GetRockScript()
    {
        if (currentRock != null)
        {
            return currentRock.GetComponent<FallingRock>();
        }
        return null;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = gizmoColor;
        Vector3 spawnPos = transform.position + Vector3.up * spawnHeight;
        
        Gizmos.DrawWireSphere(transform.position, 2f);
        Gizmos.DrawLine(transform.position, spawnPos);
        Gizmos.DrawWireCube(spawnPos, Vector3.one * 2f);

        if (Application.isPlaying && currentRock != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentRock.transform.position);
        }
    }
}