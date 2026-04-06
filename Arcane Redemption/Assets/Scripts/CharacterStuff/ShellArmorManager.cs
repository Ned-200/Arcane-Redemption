using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages shell armor pieces on the Shell Boss.
/// Removes one armor piece each time a falling rock hits the boss.
/// Attach this to the same GameObject as ShellBoss script.
/// </summary>
[RequireComponent(typeof(ShellBoss))]
public class ShellArmorManager : MonoBehaviour
{
    [Header("Armor Configuration")]
    [SerializeField] private bool autoDetectArmorPieces = true;
    [SerializeField] private List<BossArmorPiece> shellArmorPieces = new List<BossArmorPiece>();
    [SerializeField] private int maxRockHits = 3; // Total number of rock hits needed to break shell

    [Header("Armor Removal Order")]
    [SerializeField] private ArmorRemovalOrder removalOrder = ArmorRemovalOrder.Sequential;
    [SerializeField] private bool shuffleOnStart = false;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject armorBreakVFX;
    private AudioSource audioSource;
    [SerializeField] private AudioClip[] shellCrackSounds;
    [SerializeField] private AudioClip shellBreakSound;
    [SerializeField] private bool playFeedbackOnEachHit = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private ShellBoss shellBoss;
    private int currentRockHits = 0;
    private int armorPiecesRemoved = 0;
    private bool isShellBroken = false;

    public bool IsShellActive => !isShellBroken;
    public int CurrentRockHits => currentRockHits;
    public int RemainingHits => Mathf.Max(0, maxRockHits - currentRockHits);
    public int RemainingArmorPieces => Mathf.Max(0, shellArmorPieces.Count - armorPiecesRemoved);
    public float ShellIntegrity => isShellBroken ? 0f : (float)(maxRockHits - currentRockHits) / maxRockHits;

    public event System.Action OnArmorPieceRemoved;
    public event System.Action OnShellFullyBroken;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        
        shellBoss = GetComponent<ShellBoss>();
        if (shellBoss == null)
        {
            Debug.LogError($"[{gameObject.name}] ShellArmorManager requires a ShellBoss component!");
            enabled = false;
            return;
        }

        if (autoDetectArmorPieces)
        {
            DetectArmorPieces();
        }

        ValidateConfiguration();
        InitializeArmorOrder();
    }

    /// <summary>
    /// Automatically finds all BossArmorPiece components in children tagged as shell armor.
    /// </summary>
    private void DetectArmorPieces()
    {
        shellArmorPieces.Clear();
        BossArmorPiece[] allArmorPieces = GetComponentsInChildren<BossArmorPiece>(true);
        
        foreach (BossArmorPiece piece in allArmorPieces)
        {
            // Only add pieces that are meant for shell armor (you can filter by name or tag)
            if (piece.ArmorName.ToLower().Contains("shell") || piece.ArmorName.ToLower().Contains("armor"))
            {
                shellArmorPieces.Add(piece);
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Auto-detected {shellArmorPieces.Count} shell armor pieces");
        }
    }

    /// <summary>
    /// Validates the armor configuration and warns about potential issues.
    /// </summary>
    private void ValidateConfiguration()
    {
        if (shellArmorPieces.Count == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] No armor pieces configured! Add BossArmorPiece components to shell meshes.");
            return;
        }

        if (shellArmorPieces.Count < maxRockHits)
        {
            Debug.LogWarning($"[{gameObject.name}] Only {shellArmorPieces.Count} armor pieces but {maxRockHits} hits required. Some hits won't remove armor.");
        }

        // Validate each armor piece
        for (int i = 0; i < shellArmorPieces.Count; i++)
        {
            if (shellArmorPieces[i] == null)
            {
                Debug.LogError($"[{gameObject.name}] Armor piece at index {i} is null!");
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Shell armor system initialized:");
            Debug.Log($"  - Armor Pieces: {shellArmorPieces.Count}");
            Debug.Log($"  - Max Rock Hits: {maxRockHits}");
            Debug.Log($"  - Removal Order: {removalOrder}");
        }
    }

    /// <summary>
    /// Initializes the armor removal order based on settings.
    /// </summary>
    private void InitializeArmorOrder()
    {
        if (shuffleOnStart)
        {
            ShuffleArmorPieces();
        }

        if (removalOrder == ArmorRemovalOrder.Random)
        {
            // Random order will be determined at runtime
            if (showDebugLogs)
            {
                Debug.Log($"[{gameObject.name}] Armor will be removed in random order");
            }
        }
        else if (removalOrder == ArmorRemovalOrder.Sequential)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[{gameObject.name}] Armor removal order:");
                for (int i = 0; i < shellArmorPieces.Count; i++)
                {
                    Debug.Log($"  {i + 1}. {shellArmorPieces[i].ArmorName}");
                }
            }
        }
    }

    /// <summary>
    /// Shuffles the armor pieces list for randomized removal.
    /// </summary>
    private void ShuffleArmorPieces()
    {
        for (int i = shellArmorPieces.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            BossArmorPiece temp = shellArmorPieces[i];
            shellArmorPieces[i] = shellArmorPieces[randomIndex];
            shellArmorPieces[randomIndex] = temp;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Armor pieces shuffled");
        }
    }

    /// <summary>
    /// Called by ShellBoss when a falling rock hits the boss.
    /// Removes one armor piece per hit.
    /// </summary>
    public void OnRockHit(FallingRock rock)
    {
        if (isShellBroken)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[{gameObject.name}] Shell already broken, rock hit ignored");
            }
            return;
        }

        currentRockHits++;

        if (showDebugLogs)
        {
            Debug.LogWarning($"[{gameObject.name}] ?? Rock hit #{currentRockHits}/{maxRockHits}!");
        }

        // Remove one armor piece
        RemoveNextArmorPiece();

        // Play feedback
        if (playFeedbackOnEachHit)
        {
            PlayArmorBreakFeedback();
        }

        // Check if shell is fully broken
        if (currentRockHits >= maxRockHits)
        {
            BreakShell();
        }

        // Invoke event
        OnArmorPieceRemoved?.Invoke();
    }

    /// <summary>
    /// Removes the next armor piece based on the removal order.
    /// </summary>
    private void RemoveNextArmorPiece()
    {
        if (armorPiecesRemoved >= shellArmorPieces.Count)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[{gameObject.name}] No more armor pieces to remove!");
            }
            return;
        }

        BossArmorPiece pieceToRemove = null;

        switch (removalOrder)
        {
            case ArmorRemovalOrder.Sequential:
                pieceToRemove = GetNextSequentialPiece();
                break;

            case ArmorRemovalOrder.Random:
                pieceToRemove = GetRandomUnremovedPiece();
                break;

            case ArmorRemovalOrder.Reverse:
                pieceToRemove = GetNextReversePiece();
                break;
        }

        if (pieceToRemove != null && !pieceToRemove.IsRemoved)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[{gameObject.name}] Removing armor piece: '{pieceToRemove.ArmorName}'");
            }

            pieceToRemove.RemoveArmor();
            armorPiecesRemoved++;
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] Failed to find armor piece to remove!");
        }
    }

    /// <summary>
    /// Gets the next armor piece in sequential order.
    /// </summary>
    private BossArmorPiece GetNextSequentialPiece()
    {
        for (int i = 0; i < shellArmorPieces.Count; i++)
        {
            if (shellArmorPieces[i] != null && !shellArmorPieces[i].IsRemoved)
            {
                return shellArmorPieces[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Gets a random unremoved armor piece.
    /// </summary>
    private BossArmorPiece GetRandomUnremovedPiece()
    {
        List<BossArmorPiece> availablePieces = new List<BossArmorPiece>();

        foreach (BossArmorPiece piece in shellArmorPieces)
        {
            if (piece != null && !piece.IsRemoved)
            {
                availablePieces.Add(piece);
            }
        }

        if (availablePieces.Count == 0) return null;

        int randomIndex = Random.Range(0, availablePieces.Count);
        return availablePieces[randomIndex];
    }

    /// <summary>
    /// Gets the next armor piece in reverse order.
    /// </summary>
    private BossArmorPiece GetNextReversePiece()
    {
        for (int i = shellArmorPieces.Count - 1; i >= 0; i--)
        {
            if (shellArmorPieces[i] != null && !shellArmorPieces[i].IsRemoved)
            {
                return shellArmorPieces[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Marks the shell as fully broken and transitions to Phase 2.
    /// </summary>
    private void BreakShell()
    {
        if (isShellBroken) return;

        if (shellBreakSound != null)
        {
            audioSource.PlayOneShot(shellBreakSound);
        }

        isShellBroken = true;

        if (showDebugLogs)
        {
            Debug.LogWarning($"[{gameObject.name}] ?? SHELL FULLY BROKEN! {armorPiecesRemoved} armor pieces removed.");
        }

        // Play final break feedback
        PlayArmorBreakFeedback();

        // Invoke event
        OnShellFullyBroken?.Invoke();
    }

    /// <summary>
    /// Plays visual and audio feedback for armor breaking.
    /// </summary>
    private void PlayArmorBreakFeedback()
    {
        if (armorBreakVFX != null)
        {
            Instantiate(armorBreakVFX, transform.position, Quaternion.identity);
        }

        if (shellCrackSounds != null)
        {
            audioSource.PlayOneShot(shellCrackSounds[Random.Range(0, shellCrackSounds.Length)]);
        }
    }

    /// <summary>
    /// Restores all armor pieces to their original state (useful for boss reset).
    /// </summary>
    public void RestoreAllArmor()
    {
        foreach (BossArmorPiece piece in shellArmorPieces)
        {
            if (piece != null)
            {
                piece.RestoreArmor();
            }
        }

        currentRockHits = 0;
        armorPiecesRemoved = 0;
        isShellBroken = false;

        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] All shell armor restored");
        }
    }

    /// <summary>
    /// Forces removal of the next armor piece (useful for testing).
    /// </summary>
    public void ForceRemoveNextArmor()
    {
        currentRockHits++;
        RemoveNextArmorPiece();

        if (currentRockHits >= maxRockHits)
        {
            BreakShell();
        }
    }

    #region Context Menu Debug Tools
    [ContextMenu("Detect Armor Pieces")]
    private void DebugDetectArmorPieces()
    {
        DetectArmorPieces();
        ValidateConfiguration();
    }

    [ContextMenu("Remove Next Armor Piece")]
    private void DebugRemoveNext()
    {
        ForceRemoveNextArmor();
    }

    [ContextMenu("Remove All Armor")]
    private void DebugRemoveAll()
    {
        while (armorPiecesRemoved < shellArmorPieces.Count)
        {
            ForceRemoveNextArmor();
        }
    }

    [ContextMenu("Restore All Armor")]
    private void DebugRestoreAll()
    {
        RestoreAllArmor();
    }

    [ContextMenu("Shuffle Armor Order")]
    private void DebugShuffle()
    {
        ShuffleArmorPieces();
    }
    #endregion
}

public enum ArmorRemovalOrder
{
    Sequential,  // Remove armor in list order (0, 1, 2...)
    Random,      // Remove random unremoved piece each time
    Reverse      // Remove in reverse order (last to first)
}