using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "Effect_HitTwice", menuName = "Spells/Effects/Hit Twice")]
public class Effect_HitTwice : SpellEffect
{
    public float secondHitMultiplier = 0.1f;
    public float hitDelay = 0.2f;

    public override void OnHit(IProjectile projectile, HitContext originalContext)
    {
        // new context for the second hit
        HitContext secondHit = new HitContext(originalContext.Target, originalContext.AttackerStats);

        // copy & modify the damage from the original context
        foreach (var dmg in originalContext.Damages)
        {
            secondHit.Damages.Add(new DamageInstance 
            {
                Type = dmg.Type,
                Amount = dmg.Amount * secondHitMultiplier
            });
        }
        
        // don't re-apply status effects
        secondHit.StatusEffects.Clear(); 

        IEnumerator DelayedHit()
        {
            yield return new WaitForSeconds(hitDelay);
            
            // new, weaker hit after the delay
            if (originalContext.Target != null) {
                originalContext.Target.TakeHit(secondHit);
            }
        }
        
        // Target is an interface, not a MonoBehaviour so we force it to be one :)
        MonoBehaviour targetMono = originalContext.Target as MonoBehaviour;
        if (targetMono != null) {
            targetMono.StartCoroutine(DelayedHit());
        }
    }
}