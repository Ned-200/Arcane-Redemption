using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Audio;

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

    [Header("Sounds")]
    public AudioClip heartbeatSound;
    public AudioClip breathingSound;
    public AudioClip synthSound;
    private GameObject breathingSoundObject;
    private GameObject synthSoundObject;
    [SerializeField] private AudioSource loopingAudioSource;
    [SerializeField] private float soundVolume = 1f;

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

        // Separate AudioSource for looping sounds
        loopingAudioSource.playOnAwake = false;
        loopingAudioSource.spatialBlend = 0f;
        loopingAudioSource.volume = soundVolume;
        loopingAudioSource.loop = true;
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

        if (healthPercent <= criticalThreshold) {
            PlayLoopingSound(heartbeatSound);

            if (breathingSound != null && breathingSoundObject == null) {
                breathingSoundObject = new GameObject("breathingSoundObject");
                breathingSoundObject.transform.SetParent(transform);
                AudioSource breathingSource = breathingSoundObject.AddComponent<AudioSource>();
                breathingSource.PlayOneShot(breathingSound);
                Destroy(breathingSoundObject, breathingSound.length);
            }
            if (synthSound != null && synthSoundObject == null) {
                synthSoundObject = new GameObject("synthSoundObject");
                synthSoundObject.transform.SetParent(transform);
                AudioSource synthSource = synthSoundObject.AddComponent<AudioSource>();
                synthSource.PlayOneShot(synthSound);
                Destroy(synthSoundObject, synthSound.length);
            }

        } else {
            StopLoopingSound();
            if (synthSoundObject != null) {
                synthSoundObject = null;
                Destroy(synthSoundObject);
            }
            if (breathingSoundObject != null) {
                breathingSoundObject = null;
                Destroy(breathingSoundObject);
            }
        }

        if (newHealthPercent == 0)
        {
            StopLoopingSound();
            if (synthSoundObject != null) {
                Destroy(synthSoundObject);
            }
            if (breathingSoundObject != null) {
                Destroy(breathingSoundObject);
            }
        }
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

    private void PlayLoopingSound(AudioClip clip)
    {
        if (clip != null && loopingAudioSource != null)
        {
            loopingAudioSource.clip = clip;
            loopingAudioSource.volume = soundVolume;
            loopingAudioSource.Play();
            
            Debug.Log($"[StaffWeapon] Playing looping sound: {clip.name}");
        }
    }

    private void StopLoopingSound()
    {
        if (loopingAudioSource != null && loopingAudioSource.isPlaying)
        {
            Debug.Log("[StaffWeapon] Stopping looping sound");
            loopingAudioSource.Stop();
        }
    }
}