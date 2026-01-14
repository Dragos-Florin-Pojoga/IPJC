using UnityEngine;

[CreateAssetMenu(fileName = "Effect_AddDamage", menuName = "Spells/Effects/Add Damage")]
public class Effect_AddDamage : SpellEffect
{
    public DamageInstance damage;

    public override void OnCompileHit(IProjectile projectile, HitContext context)
    {
        context.Damages.Add(new DamageInstance { Type = damage.Type, Amount = damage.Amount });
    }
}