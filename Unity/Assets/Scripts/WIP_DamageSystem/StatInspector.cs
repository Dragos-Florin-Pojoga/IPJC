/////////////////////////////////
// DEBUG / INSPECTOR UTILITY   //
/////////////////////////////////

using UnityEngine;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// Attach to any GameObject to inspect its stats at runtime.
/// Supports: StatController (entities), Projectile, EnemyProjectile
/// Works in both Play mode and Edit mode (via OnValidate).
/// </summary>
[ExecuteAlways]
public class StatInspector : MonoBehaviour
{
    [Header("Auto-Refresh")]
    [Tooltip("Refresh every frame in Play mode")]
    public bool autoRefresh = true;
    
    [Header("Inspection Output")]
    [TextArea(20, 40)]
    public string inspectorDisplay = "Click 'Refresh' or enter Play mode...";
    
    [Header("Actions")]
    [Tooltip("Click to manually refresh")]
    public bool refresh;

    void OnValidate()
    {
        if (refresh) {
            refresh = false;
            RefreshDisplay();
        }
    }

    void Start()
    {
        RefreshDisplay();
    }

    void Update()
    {
        if (autoRefresh && Application.isPlaying) {
            RefreshDisplay();
        }
    }

    [ContextMenu("Refresh Display")]
    public void RefreshDisplay()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"=== INSPECTOR: {gameObject.name} ===");
        sb.AppendLine();

        bool foundAnything = false;

        // Try StatController
        var statController = GetComponent<StatController>() ?? GetComponentInParent<StatController>();
        if (statController != null) {
            foundAnything = true;
            AppendStatController(sb, statController);
        }

        // Try Projectile
        var projectile = GetComponent<Projectile>();
        if (projectile != null) {
            foundAnything = true;
            AppendProjectile(sb, projectile);
        }

        // Try EnemyProjectile
        var enemyProjectile = GetComponent<EnemyProjectile>();
        if (enemyProjectile != null) {
            foundAnything = true;
            AppendEnemyProjectile(sb, enemyProjectile);
        }

        if (!foundAnything) {
            sb.AppendLine("No inspectable components found.");
            sb.AppendLine("Supports: StatController, Projectile, EnemyProjectile");
        }

        inspectorDisplay = sb.ToString();
    }

    // =========================================================================
    // STAT CONTROLLER
    // =========================================================================
    private void AppendStatController(StringBuilder sb, StatController controller)
    {
        sb.AppendLine("┌─────────────────────────────────┐");
        sb.AppendLine("│        STAT CONTROLLER          │");
        sb.AppendLine("└─────────────────────────────────┘");

        var statsField = typeof(StatController).GetField("m_stats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var resourcesField = typeof(StatController).GetField("m_resources", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var stats = statsField?.GetValue(controller) as Dictionary<StatType, Stat>;
        var resources = resourcesField?.GetValue(controller) as Dictionary<StatType, ResourceStat>;

        if (stats == null || stats.Count == 0) {
            sb.AppendLine("  No stats configured.");
            sb.AppendLine();
            return;
        }

        // Resources
        if (resources != null && resources.Count > 0) {
            sb.AppendLine();
            sb.AppendLine("──── RESOURCES ────");
            foreach (var kvp in resources) {
                var type = kvp.Key;
                var resource = kvp.Value;
                var stat = resource.MaxStat;
                
                sb.AppendLine($"  ▸ {type}: {resource.Current:F1} / {resource.Max:F1} ({resource.Ratio * 100:F0}%)");
                sb.AppendLine($"      Base: {stat.BaseValue:F1}");
                AppendModifiers(sb, stat.m_modifiers, "      ");
            }
        }

        // Attributes
        sb.AppendLine();
        sb.AppendLine("──── ATTRIBUTES ────");
        foreach (var kvp in stats) {
            if (resources != null && resources.ContainsKey(kvp.Key)) continue;
            
            var stat = kvp.Value;
            sb.AppendLine($"  ▸ {kvp.Key}: {stat.GetValue():F2} (base: {stat.BaseValue:F1})");
            AppendModifiers(sb, stat.m_modifiers, "      ");
        }

        // Regen
        if (controller.regenConfigs.Count > 0) {
            sb.AppendLine();
            sb.AppendLine("──── REGEN ────");
            foreach (var regen in controller.regenConfigs) {
                sb.AppendLine($"  ▸ {regen.resourceType}: +{regen.amountPerSecond:F1}/s");
            }
        }

        sb.AppendLine();
    }

    private void AppendModifiers(StringBuilder sb, List<StatModifier> modifiers, string indent)
    {
        if (modifiers == null || modifiers.Count == 0) return;
        
        foreach (var mod in modifiers) {
            string typeStr = mod.Type switch {
                ModifierType.Flat => $"+{mod.Value:F1}",
                ModifierType.PercentAdd => $"+{mod.Value * 100:F0}%",
                ModifierType.PercentMultiply => $"×{(1 + mod.Value):F2}",
                _ => mod.Value.ToString()
            };
            string duration = mod.Duration > 0 ? $" ({mod.Duration:F1}s)" : "";
            sb.AppendLine($"{indent}• {typeStr}{duration}");
        }
    }

    // =========================================================================
    // PROJECTILE
    // =========================================================================
    private void AppendProjectile(StringBuilder sb, Projectile projectile)
    {
        sb.AppendLine("┌─────────────────────────────────┐");
        sb.AppendLine("│          PROJECTILE             │");
        sb.AppendLine("└─────────────────────────────────┘");

        // Use reflection to get private fields
        var speedField = typeof(Projectile).GetField("m_speed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var lifetimeField = typeof(Projectile).GetField("m_lifetime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var spawnTimeField = typeof(Projectile).GetField("m_spawnTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var tickRateField = typeof(Projectile).GetField("m_tickRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var effectsField = typeof(Projectile).GetField("m_runtimeEffects", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        float speed = speedField != null ? (float)speedField.GetValue(projectile) : 0;
        float lifetime = lifetimeField != null ? (float)lifetimeField.GetValue(projectile) : 0;
        float spawnTime = spawnTimeField != null ? (float)spawnTimeField.GetValue(projectile) : 0;
        float tickRate = tickRateField != null ? (float)tickRateField.GetValue(projectile) : 0;
        var effects = effectsField?.GetValue(projectile) as List<SpellEffect>;

        float elapsed = Time.time - spawnTime;
        float remaining = lifetime - elapsed;

        sb.AppendLine();
        sb.AppendLine("──── FLIGHT ────");
        sb.AppendLine($"  Speed: {speed:F1}");
        sb.AppendLine($"  Direction: {projectile.Direction}");
        sb.AppendLine($"  Lifetime: {elapsed:F1}s / {lifetime:F1}s (remaining: {remaining:F1}s)");
        if (tickRate < float.MaxValue) {
            sb.AppendLine($"  Tick Rate: {tickRate:F2}s");
        }

        sb.AppendLine();
        sb.AppendLine("──── OWNER ────");
        sb.AppendLine($"  {(projectile.OwnerStats != null ? projectile.OwnerStats.gameObject.name : "None")}");

        if (effects != null && effects.Count > 0) {
            sb.AppendLine();
            sb.AppendLine("──── SPELL EFFECTS ────");
            foreach (var effect in effects) {
                sb.AppendLine($"  ▸ {effect.GetType().Name}");
                AppendEffectDetails(sb, effect);
            }
        }

        sb.AppendLine();
    }

    // =========================================================================
    // ENEMY PROJECTILE
    // =========================================================================
    private void AppendEnemyProjectile(StringBuilder sb, EnemyProjectile projectile)
    {
        sb.AppendLine("┌─────────────────────────────────┐");
        sb.AppendLine("│       ENEMY PROJECTILE          │");
        sb.AppendLine("└─────────────────────────────────┘");

        var speedField = typeof(EnemyProjectile).GetField("m_speed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var lifetimeField = typeof(EnemyProjectile).GetField("m_lifetime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var spawnTimeField = typeof(EnemyProjectile).GetField("m_spawnTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var tickRateField = typeof(EnemyProjectile).GetField("m_tickRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var effectsField = typeof(EnemyProjectile).GetField("m_runtimeEffects", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        float speed = speedField != null ? (float)speedField.GetValue(projectile) : 0;
        float lifetime = lifetimeField != null ? (float)lifetimeField.GetValue(projectile) : 0;
        float spawnTime = spawnTimeField != null ? (float)spawnTimeField.GetValue(projectile) : 0;
        float tickRate = tickRateField != null ? (float)tickRateField.GetValue(projectile) : 0;
        var effects = effectsField?.GetValue(projectile) as List<SpellEffect>;

        float elapsed = Time.time - spawnTime;
        float remaining = lifetime - elapsed;

        sb.AppendLine();
        sb.AppendLine("──── FLIGHT ────");
        sb.AppendLine($"  Speed: {speed:F1}");
        sb.AppendLine($"  Direction: {projectile.Direction}");
        sb.AppendLine($"  Lifetime: {elapsed:F1}s / {lifetime:F1}s (remaining: {remaining:F1}s)");
        if (tickRate < float.MaxValue) {
            sb.AppendLine($"  Tick Rate: {tickRate:F2}s");
        }

        sb.AppendLine();
        sb.AppendLine("──── TARGET TAGS ────");
        foreach (var tag in projectile.damageableTags) {
            sb.AppendLine($"  • {tag}");
        }

        sb.AppendLine();
        sb.AppendLine("──── OWNER ────");
        sb.AppendLine($"  {(projectile.OwnerStats != null ? projectile.OwnerStats.gameObject.name : "None")}");

        if (effects != null && effects.Count > 0) {
            sb.AppendLine();
            sb.AppendLine("──── SPELL EFFECTS ────");
            foreach (var effect in effects) {
                sb.AppendLine($"  ▸ {effect.GetType().Name}");
                AppendEffectDetails(sb, effect);
            }
        }

        sb.AppendLine();
    }

    // =========================================================================
    // SPELL EFFECT DETAILS
    // =========================================================================
    private void AppendEffectDetails(StringBuilder sb, SpellEffect effect)
    {
        // Use reflection to show public fields
        var fields = effect.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        foreach (var field in fields) {
            var value = field.GetValue(effect);
            if (value is DamageInstance dmg) {
                sb.AppendLine($"      {field.Name}: {dmg.Amount:F1} {dmg.Type}");
            } else {
                sb.AppendLine($"      {field.Name}: {value}");
            }
        }
    }
}
