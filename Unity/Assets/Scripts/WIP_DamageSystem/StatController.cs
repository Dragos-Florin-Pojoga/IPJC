using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all stats for an entity (player, enemy, etc.).
/// Supports both attribute stats (Armor, CritChance) and resource stats (Health, Mana).
/// </summary>
/// <remarks>
/// Resource stats are determined by ResourceStatConfig and track current/max separately.
/// Provides events for UI updates and supports passive regeneration.
/// </remarks>
public class StatController : MonoBehaviour
{
    /// <summary>
    /// Entry for configuring a stat in the inspector.
    /// </summary>
    [System.Serializable]
    public class StatEntry {
        public StatType type;
        public Stat stat;
    }

    [Header("Stats Configuration")]
    [Tooltip("All stats for this entity")]
    public List<StatEntry> statEntries = new List<StatEntry>();
    
    [Header("Regeneration")]
    [Tooltip("Configure passive regeneration for resource stats")]
    public List<RegenConfig> regenConfigs = new List<RegenConfig>();

    private Dictionary<StatType, Stat> m_stats = new Dictionary<StatType, Stat>();
    private Dictionary<StatType, ResourceStat> m_resources = new Dictionary<StatType, ResourceStat>();

    /// <summary>
    /// Fired when any resource stat changes. Useful for UI that listens to all resources.
    /// </summary>
    public event System.EventHandler<ResourceChangedEventArgs> OnAnyResourceChanged;

    void Awake()
    {
        InitializeStats();
    }

    private void InitializeStats()
    {
        m_stats.Clear();
        m_resources.Clear();
        
        foreach (var entry in statEntries) {
            m_stats[entry.type] = entry.stat;
            
            if (ResourceStatConfig.IsResource(entry.type)) {
                var resource = new ResourceStat(entry.type, entry.stat);
                m_resources[entry.type] = resource;
                resource.OnChanged += (sender, args) => OnAnyResourceChanged?.Invoke(this, args);
            }
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        
        foreach (var stat in m_stats.Values) {
            stat.UpdateTimers(dt);
        }
        
        foreach (var regen in regenConfigs) {
            if (!m_resources.TryGetValue(regen.resourceType, out var resource)) continue;
            if (regen.requireAlive && resource.Current <= 0) continue;
            resource.Modify(regen.amountPerSecond * dt);
        }
    }

    // =========================================================================
    // ATTRIBUTE STATS API
    // =========================================================================
    
    /// <summary>
    /// Gets the computed value of a stat (base + modifiers).
    /// For resource stats, this returns the MAX value.
    /// </summary>
    public float GetStatValue(StatType type)
    {
        if (m_stats.TryGetValue(type, out Stat stat)) {
            return stat.GetValue();
        }
        Debug.LogWarning($"Stat {type} not found on {gameObject.name}");
        return 0;
    }
    
    /// <summary>
    /// Adds a modifier to a stat (buff/debuff).
    /// </summary>
    public void AddModifier(StatType type, StatModifier mod)
    {
        if (m_stats.TryGetValue(type, out Stat stat)) {
            stat.AddModifier(mod);
            if (m_resources.TryGetValue(type, out var resource)) {
                resource.ClampToMax();
            }
        }
    }
    
    /// <summary>
    /// Removes all modifiers from a specific source.
    /// </summary>
    public void RemoveModifiersFromSource(object source)
    {
        foreach (var stat in m_stats.Values) {
            stat.RemoveModifiersFromSource(source);
        }
        foreach (var resource in m_resources.Values) {
            resource.ClampToMax();
        }
    }

    // =========================================================================
    // RESOURCE STATS API
    // =========================================================================
    
    /// <summary>Checks if a stat type is a resource.</summary>
    public bool IsResourceStat(StatType type) => m_resources.ContainsKey(type);
    
    /// <summary>Gets the current value of a resource stat.</summary>
    public float GetCurrentValue(StatType type)
    {
        if (m_resources.TryGetValue(type, out var resource)) {
            return resource.Current;
        }
        return GetStatValue(type);
    }
    
    /// <summary>Gets the max value of a resource stat.</summary>
    public float GetMaxValue(StatType type)
    {
        if (m_resources.TryGetValue(type, out var resource)) {
            return resource.Max;
        }
        return GetStatValue(type);
    }
    
    /// <summary>Gets the current/max ratio (0-1) for UI elements like health bars.</summary>
    public float GetResourceRatio(StatType type)
    {
        if (m_resources.TryGetValue(type, out var resource)) {
            return resource.Ratio;
        }
        return 1f;
    }
    
    /// <summary>Modifies a resource by a delta. Use negative for damage, positive for healing.</summary>
    public void ModifyResource(StatType type, float delta)
    {
        if (m_resources.TryGetValue(type, out var resource)) {
            resource.Modify(delta);
        } else {
            Debug.LogWarning($"{type} is not a resource stat on {gameObject.name}");
        }
    }
    
    /// <summary>Sets a resource's current value directly (clamped).</summary>
    public void SetResourceCurrent(StatType type, float value)
    {
        if (m_resources.TryGetValue(type, out var resource)) {
            resource.SetCurrent(value);
        }
    }
    
    /// <summary>Restores a resource to full.</summary>
    public void SetResourceToMax(StatType type)
    {
        if (m_resources.TryGetValue(type, out var resource)) {
            resource.SetToMax();
        }
    }
    
    /// <summary>Depletes a resource completely.</summary>
    public void SetResourceToZero(StatType type)
    {
        if (m_resources.TryGetValue(type, out var resource)) {
            resource.SetToZero();
        }
    }
    
    /// <summary>Subscribes to changes for a specific resource stat.</summary>
    public void SubscribeToResource(StatType type, System.EventHandler<ResourceChangedEventArgs> handler)
    {
        if (m_resources.TryGetValue(type, out var resource)) {
            resource.OnChanged += handler;
        }
    }
    
    /// <summary>Unsubscribes from changes for a specific resource stat.</summary>
    public void UnsubscribeFromResource(StatType type, System.EventHandler<ResourceChangedEventArgs> handler)
    {
        if (m_resources.TryGetValue(type, out var resource)) {
            resource.OnChanged -= handler;
        }
    }
}
