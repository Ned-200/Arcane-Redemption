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

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (playerAnim == null)
        {
            playerAnim = owner.transform.GetComponent<Animator>();
        }
    }

    protected override void PlayAttackAnimation()
    {
        if (playerAnim != null)
        {
            playerAnim.Play("Spell Cast");
        } else
        {
            Debug.LogError("Staff could not find Player Animator");
        }
    }

    protected override void OnProjectileFired(ProjectileBase projectile)
    {
        base.OnProjectileFired(projectile);

        Debug.Log($"Staff fired projectile with {damage} damage!");

        // Flash the staff glow
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
            }
            else
            {
                chargingEffect.Stop();
            }
        }

        Debug.Log($"Staff aim mode: {(aiming ? "ENABLED" : "DISABLED")}");
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