using UnityEngine;

/// <summary>
/// Represents a single piece of boss armor that can be destroyed at specific health thresholds.
/// Attach this component to each armor GameObject in the boss hierarchy.
/// </summary>
public class BossArmorPiece : MonoBehaviour
{
    [Header("Armor Configuration")]
    [SerializeField] private string armorName = "Armor Piece";
    [SerializeField] [Tooltip("Health percentage (0-1) at which this armor piece should be removed")]
    private float removalThreshold = 0.8f;
    [SerializeField] private ArmorRemovalType removalType = ArmorRemovalType.Disable;
    
    [Header("Destruction Settings")]
    [SerializeField] private GameObject destructionVFX;
    [SerializeField] private AudioClip destructionSound;
    [SerializeField] private float destructionDelay = 0f;
    
    private bool isRemoved = false;
    private Renderer[] renderers;

    public string ArmorName => armorName;
    public float RemovalThreshold => removalThreshold;
    public bool IsRemoved => isRemoved;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    /// <summary>
    /// Removes this armor piece using the configured removal type.
    /// </summary>
    public void RemoveArmor()
    {
        if (isRemoved) return;

        isRemoved = true;

        Debug.Log($"[{name}] Armor piece '{armorName}' removed at threshold {removalThreshold * 100}%");

        PlayDestructionEffects();

        if (destructionDelay > 0)
        {
            Invoke(nameof(ExecuteRemoval), destructionDelay);
        }
        else
        {
            ExecuteRemoval();
        }
    }

    private void ExecuteRemoval()
    {
        switch (removalType)
        {
            case ArmorRemovalType.Disable:
                DisableArmor();
                break;
            case ArmorRemovalType.DisableRenderers:
                DisableRenderers();
                break;
            case ArmorRemovalType.Destroy:
                DestroyArmor();
                break;
        }
    }

    private void DisableArmor()
    {
        gameObject.SetActive(false);
    }

    private void DisableRenderers()
    {
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
    }

    private void DestroyArmor()
    {
        Destroy(gameObject);
    }

    private void PlayDestructionEffects()
    {
        if (destructionVFX != null)
        {
            Instantiate(destructionVFX, transform.position, transform.rotation);
        }

        if (destructionSound != null)
        {
            AudioSource.PlayClipAtPoint(destructionSound, transform.position);
        }
    }

    /// <summary>
    /// Restores the armor piece to its original state (useful for boss reset).
    /// </summary>
    public void RestoreArmor()
    {
        if (!isRemoved) return;

        isRemoved = false;
        gameObject.SetActive(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }

        Debug.Log($"[{name}] Armor piece '{armorName}' restored");
    }
}

public enum ArmorRemovalType
{
    Disable,           // Deactivates the GameObject
    DisableRenderers,  // Only disables renderers (keeps colliders active)
    Destroy            // Destroys the GameObject
}