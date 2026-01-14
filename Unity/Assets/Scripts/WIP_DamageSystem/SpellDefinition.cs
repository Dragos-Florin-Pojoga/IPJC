using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A "recipe" for a spell: combines a projectile prefab with a list of SpellEffects.
/// Create as ScriptableObject asset via Create → Spells → Spell Definition.
/// </summary>
/// <remarks>
/// Used by weapons and EnemyAttackController to fire configured spells.
/// Effects are cloned at runtime to allow per-projectile state.
/// </remarks>
[CreateAssetMenu(fileName = "New Spell", menuName = "Spells/Spell Definition")]
public class SpellDefinition : ScriptableObject
{
    /// <summary>The prefab to instantiate (must have Projectile or EnemyProjectile component)</summary>
    [Tooltip("The basic projectile prefab to spawn.")]
    public GameObject projectilePrefab;
    
    /// <summary>Effects that compose this spell's behavior</summary>
    [Tooltip("The list of effects that compose this spell.")]
    public List<SpellEffect> effects = new List<SpellEffect>();
}