using UnityEngine;

public class TerrainSwap : MonoBehaviour
{
    [Header("Drive With This Bool")]
    [SerializeField] private bool checkpointReached;

    [Header("Terrain")]
    [SerializeField] private bool affectAllActiveTerrains = true;
    [SerializeField] private Terrain singleTerrain;

    [Header("Layer Swap")]
    [Tooltip("Layer you originally painted with")]
    [SerializeField] private TerrainLayer originalLayer;

    [Tooltip("Layer to switch to")]
    [SerializeField] private TerrainLayer swappedLayer;

    private Terrain[] terrains;
    private TerrainLayer[][] cachedOriginalLayers;
    private bool currentlySwapped;

    private void Awake()
    {
        CacheOriginalLayers();
        ApplyBool(); // ensure correct state on start
    }

    private void OnValidate()
    {
        // Allows live toggling in Inspector
        if (Application.isPlaying)
            ApplyBool();
    }

    /// Call this if you change the bool from another script
    public void SetCheckpointReached(bool value)
    {
        checkpointReached = value;
        ApplyBool();
    }

    private void ApplyBool()
    {
        if (checkpointReached && !currentlySwapped)
            SwapToNew();
        else if (!checkpointReached && currentlySwapped)
            RevertToOriginal();
    }

    private void SwapToNew()
    {
        EnsureCached();

        foreach (var t in terrains)
        {
            if (t == null) continue;

            var data = t.terrainData;
            var layers = data.terrainLayers;

            int index = System.Array.IndexOf(layers, originalLayer);
            if (index == -1)
            {
                Debug.LogWarning($"[TerrainSwap] '{originalLayer.name}' not found on {t.name}");
                continue;
            }

            layers[index] = swappedLayer;
            data.terrainLayers = layers;
            t.Flush(); // REQUIRED in URP
        }

        currentlySwapped = true;
        Debug.Log("[TerrainSwap] Swapped to new layer");
    }

    private void RevertToOriginal()
    {
        EnsureCached();

        for (int i = 0; i < terrains.Length; i++)
        {
            if (terrains[i] == null) continue;

            terrains[i].terrainData.terrainLayers =
                (TerrainLayer[])cachedOriginalLayers[i].Clone();

            terrains[i].Flush();
        }

        currentlySwapped = false;
        Debug.Log("[TerrainSwap] Reverted to original layers");
    }

    private void CacheOriginalLayers()
    {
        terrains = affectAllActiveTerrains
            ? Terrain.activeTerrains
            : new Terrain[] { singleTerrain };

        cachedOriginalLayers = new TerrainLayer[terrains.Length][];

        for (int i = 0; i < terrains.Length; i++)
        {
            if (terrains[i] == null) continue;
            cachedOriginalLayers[i] =
                (TerrainLayer[])terrains[i].terrainData.terrainLayers.Clone();
        }
    }

    private void EnsureCached()
    {
        if (terrains == null || terrains.Length == 0)
            CacheOriginalLayers();
    }
}
