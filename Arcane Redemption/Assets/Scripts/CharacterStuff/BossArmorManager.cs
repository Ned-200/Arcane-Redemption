using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages all armor pieces on a boss character.
/// Automatically detects and removes armor based on health thresholds.
/// Attach this to the same GameObject as your boss script.
/// </summary>
[RequireComponent(typeof(EnemyCharacter))]
public class BossArmorManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private bool autoDetectArmorPieces = true;
    [SerializeField] private List<BossArmorPiece> armorPieces = new List<BossArmorPiece>();

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private EnemyCharacter bossCharacter;
    private Dictionary<float, List<BossArmorPiece>> armorThresholdMap;
    private HashSet<float> triggeredThresholds = new HashSet<float>();

    private void Awake()
    {
        bossCharacter = GetComponent<EnemyCharacter>();
        if (bossCharacter == null)
        {
            Debug.LogError($"[{gameObject.name}] BossArmorManager requires an EnemyCharacter component!");
            enabled = false;
            return;
        }

        if (autoDetectArmorPieces)
        {
            DetectArmorPieces();
        }

        BuildArmorThresholdMap();
        ValidateConfiguration();
    }

    private void Update()
    {
        if (bossCharacter == null || bossCharacter.IsDead) return;

        CheckArmorRemoval();
    }

    /// <summary>
    /// Automatically finds all BossArmorPiece components in children.
    /// </summary>
    private void DetectArmorPieces()
    {
        armorPieces.Clear();
        armorPieces.AddRange(GetComponentsInChildren<BossArmorPiece>(true));

        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Auto-detected {armorPieces.Count} armor pieces");
        }
    }

    /// <summary>
    /// Builds a dictionary mapping health thresholds to armor pieces for efficient lookup.
    /// </summary>
    private void BuildArmorThresholdMap()
    {
        armorThresholdMap = new Dictionary<float, List<BossArmorPiece>>();

        foreach (BossArmorPiece piece in armorPieces)
        {
            if (piece == null) continue;

            float threshold = piece.RemovalThreshold;
            if (!armorThresholdMap.ContainsKey(threshold))
            {
                armorThresholdMap[threshold] = new List<BossArmorPiece>();
            }

            armorThresholdMap[threshold].Add(piece);
        }

        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Armor system initialized with {armorThresholdMap.Count} unique thresholds");
            foreach (var kvp in armorThresholdMap.OrderByDescending(x => x.Key))
            {
                Debug.Log($"  - {kvp.Key * 100}%: {kvp.Value.Count} piece(s)");
            }
        }
    }

    /// <summary>
    /// Validates the armor configuration and warns about potential issues.
    /// </summary>
    private void ValidateConfiguration()
    {
        if (armorPieces.Count == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] No armor pieces configured!");
            return;
        }

        // Check for duplicate thresholds (not an error, just informational)
        var duplicates = armorPieces
            .GroupBy(p => p.RemovalThreshold)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (float threshold in duplicates)
        {
            int count = armorPieces.Count(p => p.RemovalThreshold == threshold);
            if (showDebugLogs)
            {
                Debug.Log($"[{gameObject.name}] {count} armor pieces share threshold {threshold * 100}%");
            }
        }

        // Check for invalid thresholds
        var invalidPieces = armorPieces.Where(p => p.RemovalThreshold < 0 || p.RemovalThreshold > 1);
        foreach (var piece in invalidPieces)
        {
            Debug.LogWarning($"[{gameObject.name}] Armor piece '{piece.ArmorName}' has invalid threshold: {piece.RemovalThreshold}");
        }
    }

    /// <summary>
    /// Checks if any armor pieces should be removed based on current health.
    /// </summary>
    private void CheckArmorRemoval()
    {
        float currentHealthPercent = bossCharacter.HealthPercent;

        // Check each threshold (from highest to lowest for logical ordering)
        foreach (var kvp in armorThresholdMap.OrderByDescending(x => x.Key))
        {
            float threshold = kvp.Key;

            // Skip if already triggered
            if (triggeredThresholds.Contains(threshold)) continue;

            // Check if health has dropped below this threshold
            if (currentHealthPercent <= threshold)
            {
                RemoveArmorAtThreshold(threshold, kvp.Value);
                triggeredThresholds.Add(threshold);
            }
        }
    }

    /// <summary>
    /// Removes all armor pieces at the specified threshold.
    /// </summary>
    private void RemoveArmorAtThreshold(float threshold, List<BossArmorPiece> pieces)
    {
        if (showDebugLogs)
        {
            Debug.LogWarning($"[{gameObject.name}] 💥 Health at {bossCharacter.HealthPercent * 100:F1}% - Removing armor at threshold {threshold * 100}%");
        }

        foreach (BossArmorPiece piece in pieces)
        {
            if (piece != null && !piece.IsRemoved)
            {
                piece.RemoveArmor();
            }
        }

        OnArmorRemoved(threshold);
    }

    /// <summary>
    /// Override this to add custom behavior when armor is removed.
    /// </summary>
    protected virtual void OnArmorRemoved(float threshold)
    {
        // Can be overridden by derived classes for custom behavior
    }

    /// <summary>
    /// Manually trigger armor removal at a specific threshold (useful for testing).
    /// </summary>
    public void ForceRemoveArmorAtThreshold(float threshold)
    {
        if (armorThresholdMap.TryGetValue(threshold, out List<BossArmorPiece> pieces))
        {
            RemoveArmorAtThreshold(threshold, pieces);
            triggeredThresholds.Add(threshold);
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] No armor pieces configured for threshold {threshold}");
        }
    }

    /// <summary>
    /// Restores all armor pieces to their original state.
    /// </summary>
    public void RestoreAllArmor()
    {
        foreach (BossArmorPiece piece in armorPieces)
        {
            if (piece != null)
            {
                piece.RestoreArmor();
            }
        }

        triggeredThresholds.Clear();

        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] All armor restored");
        }
    }

    /// <summary>
    /// Gets all armor pieces at a specific threshold.
    /// </summary>
    public List<BossArmorPiece> GetArmorAtThreshold(float threshold)
    {
        return armorThresholdMap.TryGetValue(threshold, out List<BossArmorPiece> pieces)
            ? new List<BossArmorPiece>(pieces)
            : new List<BossArmorPiece>();
    }

    #region Context Menu Debug Tools
    [ContextMenu("Detect Armor Pieces")]
    private void DebugDetectArmorPieces()
    {
        DetectArmorPieces();
        BuildArmorThresholdMap();
        ValidateConfiguration();
    }

    [ContextMenu("Force Remove 80% Armor")]
    private void DebugRemove80()
    {
        ForceRemoveArmorAtThreshold(0.8f);
    }

    [ContextMenu("Force Remove 60% Armor")]
    private void DebugRemove60()
    {
        ForceRemoveArmorAtThreshold(0.6f);
    }

    [ContextMenu("Force Remove 20% Armor (Face Shield)")]
    private void DebugRemove20()
    {
        ForceRemoveArmorAtThreshold(0.2f);
    }

    [ContextMenu("Restore All Armor")]
    private void DebugRestoreAll()
    {
        RestoreAllArmor();
    }
    #endregion
}