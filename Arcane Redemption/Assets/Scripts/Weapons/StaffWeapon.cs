using UnityEngine;

/// <summary>
/// Staff weapon implementation
/// Long-range magical weapon that fires projectiles
/// </summary>
public class StaffWeapon : RangedWeapon
{
    [Header("Staff Specific")]
    [SerializeField] private ParticleSystem chargingEffect;
    [SerializeField] private Light staffGlow;
    [SerializeField] private Animator playerAnim;

    [Header("Staff Audio")]
    [SerializeField] private AudioClip chargingSound;
    [SerializeField] private AudioClip[] fireAttackSounds;
    [SerializeField] private AudioClip[] waterAttackSounds;
    [SerializeField] private AudioClip[] plantAttackSounds;
    [SerializeField] private AudioClip aimStartSound;
    [SerializeField] private AudioClip aimEndSound;
    [SerializeField] private float soundVolume = 1f;

    private AudioSource oneShotAudioSource;
    private AudioSource loopingAudioSource;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (playerAnim == null)
        {
            playerAnim = owner.transform.GetComponent<Animator>();
        }

        // Dedicated AudioSource for one-shot sounds (fire, aim start/end)
        oneShotAudioSource = gameObject.AddComponent<AudioSource>();
        oneShotAudioSource.playOnAwake = false;
        oneShotAudioSource.spatialBlend = 0f;
        oneShotAudioSource.volume = soundVolume;

        // Separate AudioSource for looping sounds (charging)
        loopingAudioSource = gameObject.AddComponent<AudioSource>();
        loopingAudioSource.playOnAwake = false;
        loopingAudioSource.spatialBlend = 0f;
        loopingAudioSource.volume = soundVolume;
        loopingAudioSource.loop = true;

        Debug.Log($"[StaffWeapon] Audio initialized on {gameObject.name} | waterAttackSounds={waterAttackSounds != null} |  plantAttackSounds={plantAttackSounds != null} |  fireAttackSounds={fireAttackSounds != null} | chargingSound={chargingSound != null} | aimStartSound={aimStartSound != null} | aimEndSound={aimEndSound != null}");
    }

    protected override void PlayAttackAnimation()
    {
        base.PlayAttackAnimation();
        
        if (playerAnim != null)
        {
            playerAnim.Play("SpellCast" + comboStack);
            Debug.Log("Playing animation " + "SpellCast" + comboStack);
        } 
        else
        {
            Debug.LogError("Staff could not find Player Animator");
        }
    }

    protected override void OnProjectileFired(ProjectileBase projectile)
    {
        base.OnProjectileFired(projectile);

        Debug.Log($"[StaffWeapon] Projectile fired!");

        if (projectile.element == "Fire")
        {
            PlaySound(fireAttackSounds[Random.Range(0, fireAttackSounds.Length)]);
        } else if (projectile.element == "Water")
        {
            PlaySound(waterAttackSounds[Random.Range(0, waterAttackSounds.Length)]);
        } else if (projectile.element == "Plant")
        {
            PlaySound(plantAttackSounds[Random.Range(0, plantAttackSounds.Length)]);
        }

        if (staffGlow != null)
        {
            StopAllCoroutines();
            StartCoroutine(FlashGlow());
        }
    }

    protected override void OnAimStateChanged(bool aiming)
    {
        base.OnAimStateChanged(aiming);

        if (chargingEffect != null)
        {
            if (aiming)
            {
                chargingEffect.Play();
                PlaySound(aimStartSound);
                
                if (chargingSound != null)
                {
                    PlayLoopingSound(chargingSound);
                }
            }
            else
            {
                chargingEffect.Stop();
                PlaySound(aimEndSound);
                StopLoopingSound();
            }
        }

        Debug.Log($"Staff aim mode: {(aiming ? "ENABLED" : "DISABLED")}");
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[StaffWeapon] Tried to play null audio clip!");
            return;
        }

        if (oneShotAudioSource == null)
        {
            Debug.LogError("[StaffWeapon] OneShotAudioSource is null!");
            return;
        }

        Debug.Log($"[StaffWeapon] Playing sound: {clip.name} at volume {soundVolume}");
        oneShotAudioSource.PlayOneShot(clip, soundVolume);
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

    private System.Collections.IEnumerator FlashGlow()
    {
        if (staffGlow == null) yield break;

        float originalIntensity = staffGlow.intensity;
        staffGlow.intensity = originalIntensity * 3f;

        yield return new WaitForSeconds(0.1f);

        staffGlow.intensity = originalIntensity;
    }
}