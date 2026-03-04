using UnityEngine;

public class RockSpawnerManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private RockSpawnPoint[] spawnPoints;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private float randomSpawnChance = 0.3f;
    [SerializeField] private bool autoSpawn = true;

    private float lastSpawnTime;

    private void Start()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            spawnPoints = FindObjectsByType<RockSpawnPoint>(FindObjectsSortMode.None);
        }
    }

    private void Update()
    {
        if (!autoSpawn) return;
        if (Time.time - lastSpawnTime < spawnInterval) return;

        if (Random.value <= randomSpawnChance)
        {
            SpawnRandomRock();
        }

        lastSpawnTime = Time.time;
    }

    public void SpawnRandomRock()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        RockSpawnPoint randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        randomPoint.SpawnRock();
    }

    public void SpawnRockAt(int index)
    {
        if (spawnPoints == null || index < 0 || index >= spawnPoints.Length) return;

        spawnPoints[index].SpawnRock();
    }
}