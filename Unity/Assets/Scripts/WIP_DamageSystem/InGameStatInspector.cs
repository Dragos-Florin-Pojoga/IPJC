/////////////////////////////////
// DEBUG / IN-GAME STAT OVERLAY //
/////////////////////////////////

using UnityEngine;
using UnityEngine.InputSystem;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// In-game stat inspector overlay controlled by numpad keys.
/// Uses the new Input System. Attach to a single GameObject in the scene.
/// 
/// Controls:
///   Numpad 1 - Show player stats
///   Numpad 2 - Show equipped weapon bullet stats
///   Numpad 3 - Show stats of object you're looking at (raycast)
///   Numpad 0 - Clear the overlay
/// </summary>
public class InGameStatInspector : MonoBehaviour
{
    private enum InspectMode
    {
        None,
        PlayerStats,
        WeaponBullet,
        LookingAt
    }

    [Header("Settings")]
    [Tooltip("Max raycast distance for Numpad 3")]
    public float inspectRaycastDistance = 100f;

    private InspectMode m_currentMode = InspectMode.None;
    private string m_overlayText = "";
    private bool m_showOverlay = false;
    private GUIStyle m_boxStyle;
    private GUIStyle m_labelStyle;

    void Update()
    {
        // Use Keyboard.current from the new Input System
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.numpad1Key.wasPressedThisFrame) {
            m_currentMode = InspectMode.PlayerStats;
            m_showOverlay = true;
        }
        if (keyboard.numpad2Key.wasPressedThisFrame) {
            m_currentMode = InspectMode.WeaponBullet;
            m_showOverlay = true;
        }
        if (keyboard.numpad3Key.wasPressedThisFrame) {
            m_currentMode = InspectMode.LookingAt;
            m_showOverlay = true;
        }
        if (keyboard.numpad0Key.wasPressedThisFrame) {
            ClearOverlay();
        }

        // Auto-update the current inspection
        if (m_showOverlay && m_currentMode != InspectMode.None) {
            RefreshCurrentInspection();
        }
    }

    private void RefreshCurrentInspection()
    {
        switch (m_currentMode) {
            case InspectMode.PlayerStats:
                InspectPlayerStats();
                break;
            case InspectMode.WeaponBullet:
                InspectEquippedWeaponBullet();
                break;
            case InspectMode.LookingAt:
                InspectLookedAtObject();
                break;
        }
    }

    void OnGUI()
    {
        if (!m_showOverlay || string.IsNullOrEmpty(m_overlayText)) return;

        // Initialize styles once
        if (m_boxStyle == null) {
            m_boxStyle = new GUIStyle(GUI.skin.box);
            m_boxStyle.normal.background = MakeTexture(2, 2, new Color(0f, 0f, 0f, 0.8f));
            m_boxStyle.padding = new RectOffset(10, 10, 10, 10);
        }
        if (m_labelStyle == null) {
            m_labelStyle = new GUIStyle(GUI.skin.label);
            m_labelStyle.fontSize = 12;
            m_labelStyle.normal.textColor = Color.white;
            m_labelStyle.wordWrap = false;
        }

        // Calculate size based on content
        GUIContent content = new GUIContent(m_overlayText);
        Vector2 size = m_labelStyle.CalcSize(content);
        size.x = Mathf.Min(size.x + 20, 450);
        size.y = Mathf.Min(size.y + 20, Screen.height - 40);

        // Position in top-right corner
        Rect boxRect = new Rect(Screen.width - size.x - 20, 20, size.x, size.y);
        
        GUI.Box(boxRect, "", m_boxStyle);
        GUI.Label(new Rect(boxRect.x + 10, boxRect.y + 10, boxRect.width - 20, boxRect.height - 20), m_overlayText, m_labelStyle);
    }

    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) {
            pixels[i] = color;
        }
        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // INSPECTION METHODS
    // =========================================================================
    
    /// <summary>
    /// Numpad 1: Inspect the player's StatController
    /// </summary>
    private void InspectPlayerStats()
    {
        var playerWeaponController = FindFirstObjectByType<PlayerWeaponController>();
        if (playerWeaponController == null) {
            m_overlayText = "No PlayerWeaponController found in scene.";
            return;
        }

        var statController = playerWeaponController.m_statController;
        if (statController == null) {
            statController = playerWeaponController.GetComponent<StatController>();
        }

        if (statController == null) {
            m_overlayText = "Player has no StatController.";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"=== PLAYER STATS ===");
        sb.AppendLine();
        AppendStatController(sb, statController);
        
        m_overlayText = sb.ToString();
    }

    /// <summary>
    /// Numpad 2: Inspect the currently equipped weapon's bullet stats
    /// Shows all spell effects like the projectile inspector does.
    /// </summary>
    private void InspectEquippedWeaponBullet()
    {
        var playerWeaponController = FindFirstObjectByType<PlayerWeaponController>();
        if (playerWeaponController == null) {
            m_overlayText = "No PlayerWeaponController found in scene.";
            return;
        }

        var weapon = playerWeaponController.currentWeapon;
        if (weapon == null) {
            m_overlayText = "No weapon currently equipped.";
            return;
        }

        var spell = weapon.baseSpell;
        if (spell == null) {
            m_overlayText = $"Weapon '{weapon.name}' has no baseSpell assigned.";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("┌─────────────────────────────────┐");
        sb.AppendLine($"│  WEAPON: {weapon.name,-22}│");
        sb.AppendLine("└─────────────────────────────────┘");
        sb.AppendLine();
        sb.AppendLine($"Spell: {spell.name}");
        sb.AppendLine($"Projectile: {(spell.projectilePrefab != null ? spell.projectilePrefab.name : "None")}");

        // Collect all effects from spell + weapon modifiers
        List<SpellEffect> allEffects = new List<SpellEffect>();
        if (spell.effects != null) {
            allEffects.AddRange(spell.effects);
        }
        if (weapon.modifierEffects != null) {
            allEffects.AddRange(weapon.modifierEffects);
        }

        if (allEffects.Count > 0) {
            // Extract base projectile stats if present
            Effect_BaseProjectileStats baseStats = null;
            foreach (var effect in allEffects) {
                if (effect is Effect_BaseProjectileStats stats) {
                    baseStats = stats;
                    break;
                }
            }

            if (baseStats != null) {
                sb.AppendLine();
                sb.AppendLine("──── FLIGHT ────");
                sb.AppendLine($"  Speed: {baseStats.speed:F1}");
                sb.AppendLine($"  Lifetime: {baseStats.lifetime:F1}s");
                sb.AppendLine($"  Size: {baseStats.size:F2}");
                if (baseStats.tickRate < float.MaxValue) {
                    sb.AppendLine($"  Tick Rate: {baseStats.tickRate:F2}s");
                }
            }

            sb.AppendLine();
            sb.AppendLine("──── SPELL EFFECTS ────");
            foreach (var effect in allEffects) {
                sb.AppendLine($"  ▸ {effect.GetType().Name}");
                AppendEffectDetails(sb, effect);
            }
        } else {
            sb.AppendLine();
            sb.AppendLine("No spell effects configured.");
        }

        m_overlayText = sb.ToString();
    }

    /// <summary>
    /// Numpad 3: Inspect whatever object the player is looking at (raycast)
    /// </summary>
    private void InspectLookedAtObject()
    {
        Camera cam = Camera.main;
        if (cam == null) {
            m_overlayText = "No main camera found.";
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (!Physics.Raycast(ray, out RaycastHit hit, inspectRaycastDistance)) {
            m_overlayText = "Not looking at anything.";
            return;
        }

        GameObject target = hit.collider.gameObject;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"=== LOOKING AT: {target.name} ===");
        sb.AppendLine($"Distance: {hit.distance:F1}m");
        sb.AppendLine();

        bool foundAnything = false;

        // Try StatController
        var statController = target.GetComponent<StatController>() ?? target.GetComponentInParent<StatController>();
        if (statController != null) {
            foundAnything = true;
            AppendStatController(sb, statController);
        }

        // Try Projectile
        var projectile = target.GetComponent<Projectile>();
        if (projectile != null) {
            foundAnything = true;
            AppendProjectile(sb, projectile);
        }

        // Try EnemyProjectile
        var enemyProjectile = target.GetComponent<EnemyProjectile>();
        if (enemyProjectile != null) {
            foundAnything = true;
            AppendEnemyProjectile(sb, enemyProjectile);
        }

        if (!foundAnything) {
            sb.AppendLine("No inspectable components found on this object.");
            sb.AppendLine("Supports: StatController, Projectile, EnemyProjectile");
        }

        m_overlayText = sb.ToString();
    }

    /// <summary>
    /// Numpad 0: Clear the overlay
    /// </summary>
    public void ClearOverlay()
    {
        m_overlayText = "";
        m_showOverlay = false;
        m_currentMode = InspectMode.None;
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
