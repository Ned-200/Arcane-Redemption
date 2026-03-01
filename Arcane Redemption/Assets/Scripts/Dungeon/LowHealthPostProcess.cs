using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LowHealthPostProcess : MonoBehaviour
{
    [Header("Volume Reference")]
    public Volume globalVolume;

    [Header("Health")]
    [Range(0f, 1f)] public float criticalThreshold = 0.25f; // 25%

    [Header("Vignette Pulse")]
    public float baseVignetteIntensity = 0.2f;
    public float maxVignetteIntensity = 0.45f;
    public float pulseSpeed = 2f;

    [Header("Saturation Pulse (only when critical)")]
    [Tooltip("How desaturated you get at the pulse PEAK when just barely critical.")]
    public float peakDesatAtThreshold = -25f;

    [Tooltip("How desaturated you get at the pulse PEAK when at 0 health.")]
    public float peakDesatAtZeroHealth = -90f;

    [Tooltip("How fast saturation/vignette return to normal when not critical.")]
    public float returnSmoothSpeed = 6f;

    private ColorAdjustments colorAdjustments;
    private Vignette vignette;

    private float healthPercent = 1f;
    private float pulseTimer;
    private float pulse01;

    void Start()
    {
        if (globalVolume != null)
        {
            globalVolume.profile.TryGet(out colorAdjustments);
            globalVolume.profile.TryGet(out vignette);
        }
    }

    void Update()
    {
        UpdatePulse();
        UpdateVignette();
        UpdateSaturation();
    }

    // Call this from your health system (0..1)
    public void SetHealthPercent(float newHealthPercent)
    {
        healthPercent = Mathf.Clamp01(newHealthPercent);
    }

    void UpdatePulse()
    {
        if (healthPercent <= criticalThreshold)
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            pulse01 = Mathf.Sin(pulseTimer) * 0.5f + 0.5f; // 0..1
        }
        else
        {
            pulseTimer = 0f;
            pulse01 = Mathf.Lerp(pulse01, 0f, Time.deltaTime * returnSmoothSpeed);
        }
    }

    void UpdateVignette()
    {
        if (vignette == null) return;

        if (healthPercent <= criticalThreshold)
        {
            vignette.intensity.value = Mathf.Lerp(baseVignetteIntensity, maxVignetteIntensity, pulse01);
        }
        else
        {
            vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, baseVignetteIntensity, Time.deltaTime * returnSmoothSpeed);
        }
    }

    void UpdateSaturation()
    {
        if (colorAdjustments == null) return;

        if (healthPercent <= criticalThreshold)
        {
            // 0 when barely critical, 1 when at 0 health
            float lowHealth01 = Mathf.InverseLerp(criticalThreshold, 0f, healthPercent);

            // How strong the desaturation peak should be based on how low you are
            float peakDesat = Mathf.Lerp(peakDesatAtThreshold, peakDesatAtZeroHealth, lowHealth01);

            // Only desaturate during the pulse (0..peak)
            float targetSaturation = Mathf.Lerp(0f, peakDesat, pulse01);

            colorAdjustments.saturation.value =
                Mathf.Lerp(colorAdjustments.saturation.value, targetSaturation, Time.deltaTime * returnSmoothSpeed);
        }
        else
        {
            // Fully normal when not critical
            colorAdjustments.saturation.value =
                Mathf.Lerp(colorAdjustments.saturation.value, 0f, Time.deltaTime * returnSmoothSpeed);
        }
    }
}