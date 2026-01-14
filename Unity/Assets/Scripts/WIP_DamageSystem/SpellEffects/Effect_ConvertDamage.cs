using UnityEngine;

[CreateAssetMenu(fileName = "Effect_ConvertDamage", menuName = "Spells/Effects/Convert Damage")]
public class Effect_ConvertDamage : SpellEffect
{
    public DamageType From = DamageType.Physical;
    public DamageType To = DamageType.Fire;

    public override void OnCompileHit(IProjectile projectile, HitContext context)
    {
        float convertedAmount = 0;
        
        // TODO: add this should be able to convert a % and with a ratio
        // ex: convert 5% of physical to fire with 1:2 ratio
        context.Damages.RemoveAll(dmg => 
        {
            if (dmg.Type == From)
            {
                convertedAmount += dmg.Amount;
                return true; // Remove it
            }
            return false;
        });

        if (convertedAmount > 0)
        {
            context.Damages.Add(new DamageInstance { Type = To, Amount = convertedAmount });
        }
    }
}