using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Configuration constraints for random spell generation.
/// Use presets or customize for specific use cases.
/// </summary>
[System.Serializable]
public struct SpellGenerationConstraints
{
    // Projectile stats
    public float minSpeed, maxSpeed;
    public float minLifetime, maxLifetime;
    public float minSize, maxSize;
    
    // Damage
    public float minDamage, maxDamage;
    public int maxDamageInstances;
    
    // Behavior
    [Range(0f, 1f)] public float homingChance;
    public float minHomingSpeed, maxHomingSpeed;
    public float minHomingRadius, maxHomingRadius;
    public LayerMask homingTargetLayer;
    
    // Misc effects
    [Range(0f, 1f)] public float convertDamageChance;
    [Range(0f, 1f)] public float hitTwiceChance;
    [Range(0f, 1f)] public float damagePercentChance;

    /// <summary>
    /// Preset for player debug usage - more powerful spells allowed
    /// </summary>
    public static SpellGenerationConstraints PlayerPreset => new SpellGenerationConstraints
    {
        minSpeed = 15f, maxSpeed = 80f,
        minLifetime = 2f, maxLifetime = 8f,
        minSize = 0.08f, maxSize = 0.4f,
        
        minDamage = 10f, maxDamage = 50f,
        maxDamageInstances = 3,
        
        homingChance = 0.4f,
        minHomingSpeed = 3f, maxHomingSpeed = 8f,
        minHomingRadius = 3f, maxHomingRadius = 10f,
        homingTargetLayer = LayerMask.GetMask("Enemy"),
        
        convertDamageChance = 0.3f,
        hitTwiceChance = 0.2f,
        damagePercentChance = 0.25f
    };

    /// <summary>
    /// Preset for enemy usage - constrained to prevent one-shots
    /// </summary>
    public static SpellGenerationConstraints EnemyPreset => new SpellGenerationConstraints
    {
        minSpeed = 10f, maxSpeed = 25f,
        minLifetime = 1.5f, maxLifetime = 4f,
        minSize = 0.05f, maxSize = 0.15f,
        
        minDamage = 5f, maxDamage = 15f,
        maxDamageInstances = 2,
        
        homingChance = 0.2f,
        minHomingSpeed = 2f, maxHomingSpeed = 4f,
        minHomingRadius = 2f, maxHomingRadius = 5f,
        homingTargetLayer = LayerMask.GetMask("Player"),
        
        convertDamageChance = 0.2f,
        hitTwiceChance = 0.1f,
        damagePercentChance = 0.15f
    };
}

/// <summary>
/// Static utility class for generating random SpellDefinitions at runtime.
/// Creates sensible spells by enforcing constraints (single base stats, max one homing, etc).
/// </summary>
public static class RandomSpellGenerator
{
    /// <summary>
    /// Generate a random spell using player preset (more powerful).
    /// </summary>
    public static SpellDefinition GenerateRandomSpell(GameObject projectilePrefab)
    {
        return Generate(projectilePrefab, SpellGenerationConstraints.PlayerPreset);
    }

    /// <summary>
    /// Generate a random spell using enemy preset (constrained).
    /// </summary>
    public static SpellDefinition GenerateEnemySpell(GameObject projectilePrefab)
    {
        return Generate(projectilePrefab, SpellGenerationConstraints.EnemyPreset);
    }

    /// <summary>
    /// Generate a random spell with full control over constraints.
    /// </summary>
    public static SpellDefinition Generate(GameObject projectilePrefab, SpellGenerationConstraints constraints)
    {
        // Create a runtime ScriptableObject (won't persist after play mode)
        SpellDefinition spell = ScriptableObject.CreateInstance<SpellDefinition>();
        spell.name = "RandomSpell_" + System.Guid.NewGuid().ToString().Substring(0, 8);
        spell.projectilePrefab = projectilePrefab;
        spell.effects = new List<SpellEffect>();

        // REQUIRED: Exactly one base stats effect
        var baseStats = ScriptableObject.CreateInstance<Effect_BaseProjectileStats>();
        baseStats.speed = Random.Range(constraints.minSpeed, constraints.maxSpeed);
        baseStats.lifetime = Random.Range(constraints.minLifetime, constraints.maxLifetime);
        baseStats.size = Random.Range(constraints.minSize, constraints.maxSize);
        baseStats.tickRate = Random.Range(0.3f, 1f);
        spell.effects.Add(baseStats);

        // Add damage instances
        int damageCount = Random.Range(1, constraints.maxDamageInstances + 1);
        for (int i = 0; i < damageCount; i++)
        {
            var damageEffect = ScriptableObject.CreateInstance<Effect_AddDamage>();
            damageEffect.damage = new DamageInstance
            {
                Type = GetRandomDamageType(),
                Amount = Random.Range(constraints.minDamage, constraints.maxDamage)
            };
            spell.effects.Add(damageEffect);
        }

        // OPTIONAL: At most one homing effect
        if (Random.value < constraints.homingChance)
        {
            var homing = ScriptableObject.CreateInstance<Effect_Homing>();
            homing.rotationSpeed = Random.Range(constraints.minHomingSpeed, constraints.maxHomingSpeed);
            homing.findTargetRadius = Random.Range(constraints.minHomingRadius, constraints.maxHomingRadius);
            homing.targetLayer = constraints.homingTargetLayer;
            spell.effects.Add(homing);
        }

        // OPTIONAL: Damage conversion
        if (Random.value < constraints.convertDamageChance)
        {
            var convert = ScriptableObject.CreateInstance<Effect_ConvertDamage>();
            convert.From = DamageType.Physical;
            convert.To = DamageType.Fire;
            spell.effects.Add(convert);
        }

        // OPTIONAL: Hit twice
        if (Random.value < constraints.hitTwiceChance)
        {
            var hitTwice = ScriptableObject.CreateInstance<Effect_HitTwice>();
            hitTwice.secondHitMultiplier = Random.Range(0.1f, 0.5f);
            hitTwice.hitDelay = Random.Range(0.1f, 0.3f);
            spell.effects.Add(hitTwice);
        }

        // OPTIONAL: Damage percent bonus
        if (Random.value < constraints.damagePercentChance)
        {
            var damagePercent = ScriptableObject.CreateInstance<Effect_AddDamagePercent>();
            damagePercent.percentBonus = Random.Range(5f, 30f);
            damagePercent.type = GetRandomDamageType();
            spell.effects.Add(damagePercent);
        }

        Debug.Log($"[RandomSpellGenerator] Generated spell: speed={baseStats.speed:F1}, " +
                  $"lifetime={baseStats.lifetime:F1}, size={baseStats.size:F2}, " +
                  $"effects={spell.effects.Count}");

        return spell;
    }

    /// <summary>
    /// Returns a random DamageType.
    /// </summary>
    private static DamageType GetRandomDamageType()
    {
        var types = System.Enum.GetValues(typeof(DamageType));
        return (DamageType)types.GetValue(Random.Range(0, types.Length));
    }

    /// <summary>
    /// Get a human-readable summary of the spell's effects.
    /// </summary>
    public static string GetSpellSummary(SpellDefinition spell)
    {
        if (spell == null || spell.effects == null) return "No spell";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        float totalDamage = 0f;
        bool hasHoming = false;

        foreach (var effect in spell.effects)
        {
            if (effect is Effect_BaseProjectileStats stats)
            {
                sb.AppendLine($"Speed: {stats.speed:F1} | Life: {stats.lifetime:F1}s | Size: {stats.size:F2}");
            }
            else if (effect is Effect_AddDamage dmg)
            {
                totalDamage += dmg.damage.Amount;
            }
            else if (effect is Effect_Homing)
            {
                hasHoming = true;
            }
        }

        sb.AppendLine($"Total Base Dmg: {totalDamage:F0}");
        if (hasHoming) sb.AppendLine("+ HOMING");

        return sb.ToString();
    }
}
