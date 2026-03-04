using UnityEngine;
using System;

public class ShellProtection : MonoBehaviour
{
    [Header("Shell Settings")]
    [SerializeField] private int maxShellHits = 3;
    [SerializeField] private GameObject shellVisual;

    [Header("Feedback")]
    [SerializeField] private GameObject hitVFX;
    [SerializeField] private AudioClip shellHitSound;
    [SerializeField] private AudioClip shellBreakSound;

    private int currentShellHits;
    private bool isShellActive;

    public bool IsShellActive => isShellActive;
    public int CurrentShellHits => currentShellHits;
    public int RemainingHits => maxShellHits - currentShellHits;
    public float ShellIntegrity => isShellActive ? (float)(maxShellHits - currentShellHits) / maxShellHits : 0f;

    public event Action OnShellHit;
    public event Action OnShellBroken;

    private void Awake()
    {
        ActivateShell();
    }

    public void ActivateShell()
    {
        isShellActive = true;
        currentShellHits = 0;

        if (shellVisual != null)
        {
            shellVisual.SetActive(true);
        }

        Debug.Log($"[ShellProtection] Shell activated - {maxShellHits} hits required to break");
    }

    public bool TryDamageShell()
    {
        if (!isShellActive) return false;

        currentShellHits++;

        PlayHitFeedback();

        Debug.Log($"[ShellProtection] Shell hit! ({currentShellHits}/{maxShellHits})");

        OnShellHit?.Invoke();

        if (currentShellHits >= maxShellHits)
        {
            BreakShell();
            return true;
        }

        return false;
    }

    private void BreakShell()
    {
        isShellActive = false;

        if (shellVisual != null)
        {
            shellVisual.SetActive(false);
        }

        PlayBreakFeedback();

        Debug.LogWarning($"[ShellProtection] ??? SHELL BROKEN! Boss is now vulnerable!");

        OnShellBroken?.Invoke();
    }

    private void PlayHitFeedback()
    {
        if (hitVFX != null)
        {
            Instantiate(hitVFX, transform.position, Quaternion.identity);
        }

        if (shellHitSound != null)
        {
            AudioSource.PlayClipAtPoint(shellHitSound, transform.position);
        }
    }

    private void PlayBreakFeedback()
    {
        if (shellBreakSound != null)
        {
            AudioSource.PlayClipAtPoint(shellBreakSound, transform.position);
        }
    }
}