using UnityEngine;

/// <summary>
/// A status effect that applies stat modifiers for a duration.
/// Create as ScriptableObject asset via Create → Spells → Status Effect.
/// </summary>
/// <example>
/// Usage: A "Slow" effect could apply -30% MoveSpeed for 5 seconds.
/// A "Burn" effect could apply -5 Health/sec via a periodic damage modifier.
/// </example>
[CreateAssetMenu(fileName = "New Status Effect", menuName = "Spells/Status Effect")]
public class StatusEffect : ScriptableObject
{
    /// <summary>Display name for UI</summary>
    public string EffectName;
    
    /// <summary>Duration in seconds for all modifiers</summary>
    public float Duration;
    
    /// <summary>Array of modifiers to apply to the target</summary>
    public StatModifierData[] Modifiers;

    /// <summary>
    /// Applies all modifiers to the target's stats.
    /// </summary>
    /// <param name="target">The StatController to affect</param>
    public void Apply(StatController target)
    {
        if (target == null) return;
        foreach (var modData in Modifiers) {
            target.AddModifier(modData.StatToAffect, new StatModifier(modData.Value, modData.Type, Duration, this));
        }
    }

    /// <summary>
    /// Removes all modifiers applied by this effect.
    /// </summary>
    /// <param name="target">The StatController to remove modifiers from</param>
    public void Remove(StatController target)
    {
        if (target == null) return;
        target.RemoveModifiersFromSource(this);
    }
}