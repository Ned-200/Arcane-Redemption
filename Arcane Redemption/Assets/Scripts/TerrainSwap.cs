using UnityEngine;

public class TerrainSwap : MonoBehaviour
{
    [Header("Drive With This Bool")]
    private bool checkpointReached;

    [Header("Toggle These GameObjects")]
    [SerializeField] private GameObject terrainA; // default / before checkpoint
    [SerializeField] private GameObject terrainB; // after checkpoint

    private bool currentlySwapped;

    private void Awake()
    {
        // Ensure correct state on start
        ApplyBool(force: true);
    }

    private void OnValidate()
    {
        // Allows live toggling in Inspector (Play Mode only)
        if (Application.isPlaying)
            ApplyBool(force: false);
    }

    /// Call this if you change the bool from another script
    public void SetCheckpointReached(bool value)
    {
        checkpointReached = value;
        ApplyBool(force: false);
    }

    private void ApplyBool(bool force)
    {
        bool wantSwapped = checkpointReached;

        if (!force && wantSwapped == currentlySwapped)
            return;

        if (terrainA != null) terrainA.SetActive(!wantSwapped);
        if (terrainB != null) terrainB.SetActive(wantSwapped);

        currentlySwapped = wantSwapped;

        Debug.Log(wantSwapped
            ? "[TerrainSwap] Checkpoint reached Terrain B enabled"
            : "[TerrainSwap] Checkpoint not reached Terrain A enabled");
    }

  }
