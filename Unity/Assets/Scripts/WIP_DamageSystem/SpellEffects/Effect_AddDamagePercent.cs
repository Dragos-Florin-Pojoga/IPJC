using UnityEngine;

[CreateAssetMenu(fileName = "Effect_AddDamagePercent", menuName = "Spells/Effects/Add Damage Percent")]
public class Effect_AddDamagePercent : SpellEffect
{
    public float percentBonus = 5;
    public DamageType type;

    public override void OnCompileHit(IProjectile projectile, HitContext context)
    {
        foreach (var damage in context.Damages)
        {
            if (damage.Type == type)
            {
                damage.Amount = damage.Amount + damage.Amount * (percentBonus / 100);
            }
        }
    }
}
