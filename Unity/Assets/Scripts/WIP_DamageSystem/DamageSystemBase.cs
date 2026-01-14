using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// ============================================================================
// ENUMS
// ============================================================================

/// <summary>
/// Types of damage that can be dealt.
/// Used by DamageCalculator to apply type-specific bonuses and resistances.
/// </summary>
/// <remarks>
/// NOTE: Do not change the order of existing values - this will break serialized assets.
/// Always append new types to the end.
/// </remarks>
public enum DamageType { Physical, Fire }

/// <summary>
/// Types of stats that entities can have.
/// </summary>
/// <remarks>
/// Stats are divided into two categories:
/// - Attribute stats (Armor, CritChance): Just computed values (base + modifiers)
/// - Resource stats (Health): Track current value separate from max (see ResourceStatConfig)
/// 
/// NOTE: Do not change the order of existing values - this will break serialized assets.
/// </remarks>
public enum StatType { Health, Armor, CritChance, CritDamage, FireDamageBonus, MoveSpeed, ContactDamage, ContactCooldown, FireResistance }

/// <summary>
/// Types of stat modifiers that affect how the modifier value is applied.
/// </summary>
/// <remarks>
/// Application order: Flat → PercentAdd (summed) → PercentMultiply (individual)
/// Example with base 100: +10 Flat, +20% Add, +50% Multiply = (100 + 10) * 1.2 * 1.5 = 198
/// </remarks>
public enum ModifierType { Flat, PercentAdd, PercentMultiply }

// ============================================================================
// DATA CLASSES
// ============================================================================

/// <summary>
/// A single instance of damage with a type and amount.
/// Used in HitContext to accumulate damage from multiple sources.
/// </summary>
[System.Serializable]
public class DamageInstance
{
    /// <summary>The type of damage (Physical, Fire, etc.)</summary>
    public DamageType Type;
    
    /// <summary>The raw damage amount before mitigation</summary>
    public float Amount;
}

/// <summary>
/// Serializable data for creating stat modifiers in the inspector.
/// Used by StatusEffect to define what modifiers to apply.
/// </summary>
[System.Serializable]
public class StatModifierData
{
    public StatType StatToAffect;
    public float Value;
    public ModifierType Type;
}

/// <summary>
/// Pairs a StatusEffect with a duration for application via HitContext.
/// </summary>
[System.Serializable]
public struct StatusEffectApplication
{
    public StatusEffect Effect;
    public float Duration;
}

/// <summary>
/// The result of damage calculation after all mitigation and bonuses.
/// </summary>
[System.Serializable]
public struct FinalDamageResult
{
    /// <summary>Total damage after armor, resistances, and crit multiplier</summary>
    public float TotalDamage;
    
    /// <summary>Whether the hit was a critical strike</summary>
    public bool WasCritical;
}

// ============================================================================
// STAT MODIFIER
// ============================================================================

/// <summary>
/// A runtime modifier applied to a stat (e.g., "+5 Armor for 10s").
/// </summary>
/// <remarks>
/// Modifiers are tracked by Source so they can be removed when the source
/// is removed (e.g., when a StatusEffect expires).
/// Duration of 0 or less means permanent.
/// </remarks>
[System.Serializable]
public class StatModifier
{
    public float Value;
    public ModifierType Type;
    public float Duration;
    
    /// <summary>The object that applied this modifier (for removal tracking)</summary>
    public readonly object Source;

    public StatModifier(float value, ModifierType type, float duration, object source)
    {
        Value = value;
        Type = type;
        Duration = duration;
        Source = source;
    }
}

// ============================================================================
// STAT
// ============================================================================

/// <summary>
/// A single stat with a base value and a list of active modifiers.
/// The computed value is: (Base + Flat) * (1 + SumOfPercentAdd) * (1 + PercentMult1) * ...
/// </summary>
[System.Serializable]
public class Stat
{
    /// <summary>The base value before any modifiers</summary>
    public float BaseValue;
    
    /// <summary>Active modifiers affecting this stat</summary>
    public List<StatModifier> m_modifiers = new List<StatModifier>();

    /// <summary>
    /// Calculates the final value after applying all modifiers.
    /// </summary>
    /// <returns>The computed stat value</returns>
    public float GetValue()
    {
        float finalValue = BaseValue;
        float percentAdd = 0;
        
        // Sort by type to ensure consistent application order
        m_modifiers.Sort((a, b) => a.Type.CompareTo(b.Type));

        foreach (var mod in m_modifiers) {
            if (mod.Type == ModifierType.Flat) {
                finalValue += mod.Value;
            } else if (mod.Type == ModifierType.PercentAdd) {
                percentAdd += mod.Value;
            } else if (mod.Type == ModifierType.PercentMultiply) {
                finalValue *= (1f + mod.Value);
            }
        }

        finalValue *= (1f + percentAdd);
        return (float)System.Math.Round(finalValue, 4);
    }

    /// <summary>
    /// Adds a modifier to this stat.
    /// </summary>
    public void AddModifier(StatModifier mod)
    {
        m_modifiers.Add(mod);
    }

    /// <summary>
    /// Removes all modifiers that came from the specified source.
    /// </summary>
    public void RemoveModifiersFromSource(object source)
    {
        m_modifiers.RemoveAll(mod => mod.Source == source);
    }

    /// <summary>
    /// Ticks down modifier durations and removes expired modifiers.
    /// Called by StatController.Update().
    /// </summary>
    public void UpdateTimers(float deltaTime)
    {
        for (int i = m_modifiers.Count - 1; i >= 0; i--) {
            if (m_modifiers[i].Duration > 0) {
                m_modifiers[i].Duration -= deltaTime;
                if (m_modifiers[i].Duration <= 0) {
                    m_modifiers.RemoveAt(i);
                }
            }
        }
    }
}

// ============================================================================
// RESOURCE STATS
// ============================================================================

/// <summary>
/// Static configuration for which stat types are resources.
/// Resources track current/max separately (like Health, Mana).
/// </summary>
public static class ResourceStatConfig
{
    /// <summary>
    /// Set of stat types that should be treated as resources.
    /// Add types here to enable current/max tracking.
    /// </summary>
    public static readonly HashSet<StatType> ResourceTypes = new HashSet<StatType>
    {
        StatType.Health,
        // Add more as needed: StatType.Mana, StatType.Energy, etc.
    };

    /// <summary>
    /// Checks if a stat type is a resource.
    /// </summary>
    public static bool IsResource(StatType type) => ResourceTypes.Contains(type);
}

/// <summary>
/// Event arguments for resource changes, used for UI updates.
/// </summary>
public class ResourceChangedEventArgs : System.EventArgs
{
    public StatType StatType { get; }
    public float OldValue { get; }
    public float NewValue { get; }
    public float MaxValue { get; }
    
    /// <summary>The change amount (positive for gain, negative for loss)</summary>
    public float Delta => NewValue - OldValue;
    
    /// <summary>Current/Max ratio (0-1), useful for health bars</summary>
    public float Ratio => MaxValue > 0 ? NewValue / MaxValue : 0;

    public ResourceChangedEventArgs(StatType type, float oldValue, float newValue, float maxValue)
    {
        StatType = type;
        OldValue = oldValue;
        NewValue = newValue;
        MaxValue = maxValue;
    }
}

/// <summary>
/// A resource stat that tracks current value separately from max.
/// The max value is computed from the underlying Stat (base + modifiers).
/// </summary>
[System.Serializable]
public class ResourceStat
{
    /// <summary>The underlying Stat that computes the max value</summary>
    public Stat MaxStat;
    
    [SerializeField]
    private float m_current;
    
    /// <summary>The current value (runtime, mutable)</summary>
    public float Current => m_current;
    
    /// <summary>The max value (computed from MaxStat)</summary>
    public float Max => MaxStat.GetValue();
    
    /// <summary>Current/Max ratio (0-1)</summary>
    public float Ratio => Max > 0 ? m_current / Max : 0;
    
    /// <summary>Fired when current value changes</summary>
    public event System.EventHandler<ResourceChangedEventArgs> OnChanged;
    
    private StatType m_type;
    
    public ResourceStat(StatType type, Stat maxStat)
    {
        m_type = type;
        MaxStat = maxStat;
        m_current = maxStat.GetValue();
    }
    
    /// <summary>
    /// Modifies the current value by a delta (clamped to [0, max]).
    /// Use negative for damage, positive for healing.
    /// </summary>
    public void Modify(float delta)
    {
        float oldValue = m_current;
        m_current = Mathf.Clamp(m_current + delta, 0, Max);
        
        if (oldValue != m_current) {
            OnChanged?.Invoke(this, new ResourceChangedEventArgs(m_type, oldValue, m_current, Max));
        }
    }
    
    /// <summary>
    /// Sets the current value directly (clamped to [0, max]).
    /// </summary>
    public void SetCurrent(float value)
    {
        float oldValue = m_current;
        m_current = Mathf.Clamp(value, 0, Max);
        
        if (oldValue != m_current) {
            OnChanged?.Invoke(this, new ResourceChangedEventArgs(m_type, oldValue, m_current, Max));
        }
    }
    
    /// <summary>Restores current to max.</summary>
    public void SetToMax() => SetCurrent(Max);
    
    /// <summary>Sets current to zero.</summary>
    public void SetToZero() => SetCurrent(0);
    
    /// <summary>
    /// Clamps current to max (call when max decreases from buff expiry).
    /// </summary>
    public void ClampToMax()
    {
        if (m_current > Max) {
            SetCurrent(Max);
        }
    }
}

/// <summary>
/// Configuration for passive regeneration of a resource stat.
/// </summary>
[System.Serializable]
public class RegenConfig
{
    /// <summary>Which resource to regenerate</summary>
    public StatType resourceType;
    
    /// <summary>Amount regenerated per second</summary>
    public float amountPerSecond;
    
    /// <summary>If true, no regen when current is 0 (dead)</summary>
    [Tooltip("If true, regen only when current > 0 (no regen when dead)")]
    public bool requireAlive = true;
}

// ============================================================================
// HIT CONTEXT & DAMAGEABLE
// ============================================================================

/// <summary>
/// The "packet" of information passed through the damage pipeline.
/// SpellEffects modify this before it reaches the target.
/// </summary>
public class HitContext
{
    /// <summary>The target being hit</summary>
    public IDamageable Target { get; }
    
    /// <summary>The attacker's stats (for crit, damage bonuses)</summary>
    public StatController AttackerStats { get; }
    
    /// <summary>Accumulated damage instances (modified by SpellEffects)</summary>
    public List<DamageInstance> Damages { get; set; }
    
    /// <summary>Status effects to apply on hit</summary>
    public List<StatusEffectApplication> StatusEffects { get; set; }

    public HitContext(IDamageable target, StatController attacker)
    {
        Target = target;
        AttackerStats = attacker;
        Damages = new List<DamageInstance>();
        StatusEffects = new List<StatusEffectApplication>();
    }
}

/// <summary>
/// Interface for any entity that can receive damage.
/// Implemented by Player, Enemy, and potentially destructible objects.
/// </summary>
public interface IDamageable
{
    /// <summary>Returns the entity's StatController for damage calculation</summary>
    StatController GetStatController();

    /// <summary>Processes the hit after SpellEffects have compiled the HitContext</summary>
    void TakeHit(HitContext context);

    /// <summary>Returns the entity's transform for position/distance checks</summary>
    Transform GetTransform();
}

// ============================================================================
// DAMAGE CALCULATOR
// ============================================================================

/// <summary>
/// Static class that performs damage calculation.
/// Applies damage type bonuses, armor mitigation, and critical strikes.
/// </summary>
public static class DamageCalculator
{
    /// <summary>
    /// Calculates final damage from a HitContext.
    /// </summary>
    /// <param name="context">The compiled hit context with damage instances</param>
    /// <returns>Final damage result after all calculations</returns>
    public static FinalDamageResult CalculateHit(HitContext context)
    {
        float totalDamage = 0;
        
        StatController targetStats = context.Target.GetStatController();
        if (targetStats == null) {
            Debug.LogWarning("Hit target has no StatController!");
            return new FinalDamageResult { TotalDamage = 0, WasCritical = false };
        }

        // Roll for crit
        float critChance = context.AttackerStats.GetStatValue(StatType.CritChance);
        bool isCritical = Random.value < (critChance / 100);

        // Process each damage instance
        foreach (var damage in context.Damages) {
            float amount = damage.Amount;
            
            // Apply damage type bonuses
            if (damage.Type == DamageType.Fire) {
                amount *= (1 + context.AttackerStats.GetStatValue(StatType.FireDamageBonus));
            }
            
            // Apply mitigation
            if (damage.Type == DamageType.Physical) {
                amount -= targetStats.GetStatValue(StatType.Armor);
            } else if (damage.Type == DamageType.Fire) {
                float fireResist = targetStats.GetStatValue(StatType.FireResistance);
                amount *= (1f - Mathf.Clamp01(fireResist / 100f));
            }
            
            totalDamage += Mathf.Max(0, amount);
        }
        
        // Apply crit multiplier
        if (isCritical) {
            float critDamage = context.AttackerStats.GetStatValue(StatType.CritDamage);
            totalDamage = totalDamage + totalDamage * (critDamage / 100);
        }

        return new FinalDamageResult {
            TotalDamage = totalDamage,
            WasCritical = isCritical
        };
    }
}
