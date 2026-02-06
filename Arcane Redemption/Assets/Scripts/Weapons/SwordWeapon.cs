using UnityEngine;

/// <summary>
/// Sword weapon implementation
/// Close-range melee weapon with slashing attacks
/// </summary>
public class SwordWeapon : MeleeWeapon
{
    [Header("Sword Specific")]
    [SerializeField] private Animator swordAnimator;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (swordAnimator == null)
        {
            swordAnimator = GetComponent<Animator>();
        }
    }

    protected override void PlayAttackAnimation()
    {
        if (swordAnimator != null)
        {
            swordAnimator.SetTrigger("Attack");
        }
    }

    protected override void OnTargetHit(BaseCharacter target)
    {
        base.OnTargetHit(target);
        
        Debug.Log($"Sword hit {target.gameObject.name} for {damage} damage!");
        
        // TODO: Add screen shake
        // TODO: Add hit VFX
    }
}