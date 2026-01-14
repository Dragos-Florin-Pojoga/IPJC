/////////////////////////////////
// WIP / VERY EXPERIMENTAL !!! //
/////////////////////////////////

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attach to enemies that fire projectiles. Handles targeting and firing.
/// Uses SpellDefinition for full effect pipeline support.
/// </summary>
[RequireComponent(typeof(StatController))]
public class EnemyAttackController : MonoBehaviour
{
    [Header("Spell Configuration")]
    [Tooltip("The spell definition to fire (projectile prefab + effects)")]
    public SpellDefinition spellDefinition;
    
    [Tooltip("Where projectiles spawn from")]
    public Transform firePoint;
    
    [Header("Attack Timing")]
    public float attackCooldown = 2f;
    public float attackRange = 15f;
    
    [Header("Targeting")]
    [Tooltip("Tag of the target (typically 'Player')")]
    public string targetTag = "Player";
    
    [Tooltip("If true, only attack when target is visible (no walls between)")]
    public bool requireLineOfSight = true;
    
    [Tooltip("Layers that block line of sight")]
    public LayerMask lineOfSightBlockers;
    
    private StatController m_stats;
    private Transform m_target;
    private float m_nextAttackTime;
    
    void Awake()
    {
        m_stats = GetComponent<StatController>();
    }
    
    void Start()
    {
        // Find initial target
        FindTarget();
    }
    
    void Update()
    {
        if (m_target == null) {
            FindTarget();
            return;
        }
        
        if (Time.time < m_nextAttackTime) return;
        
        float distance = Vector3.Distance(transform.position, m_target.position);
        if (distance > attackRange) return;
        
        if (requireLineOfSight && !HasLineOfSight()) return;
        
        FireProjectile();
        m_nextAttackTime = Time.time + attackCooldown;
    }
    
    private void FindTarget()
    {
        var targetObj = GameObject.FindGameObjectWithTag(targetTag);
        if (targetObj != null) {
            m_target = targetObj.transform;
        }
    }
    
    private bool HasLineOfSight()
    {
        if (m_target == null) return false;
        
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector3 direction = (m_target.position - origin).normalized;
        float distance = Vector3.Distance(origin, m_target.position);
        
        return !Physics.Raycast(origin, direction, distance, lineOfSightBlockers);
    }
    
    private void FireProjectile()
    {
        if (spellDefinition == null || spellDefinition.projectilePrefab == null) {
            Debug.LogWarning($"{gameObject.name}: EnemyAttackController has no spell definition or projectile prefab!");
            return;
        }
        
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Vector3 direction = (m_target.position - spawnPos).normalized;
        
        // Spawn the projectile prefab
        var projectileObj = Instantiate(spellDefinition.projectilePrefab, spawnPos, Quaternion.identity);
        
        // Clone effects to create runtime instances (just like player weapon does)
        List<SpellEffect> runtimeEffects = new List<SpellEffect>();
        foreach (var effect in spellDefinition.effects) {
            runtimeEffects.Add(Instantiate(effect));
        }
        
        // Initialize based on projectile type
        if (projectileObj.TryGetComponent<EnemyProjectile>(out var enemyProj)) {
            enemyProj.Initialize(runtimeEffects, direction, m_stats);
        }
        else if (projectileObj.TryGetComponent<Projectile>(out var playerProj)) {
            // Fallback: can use regular Projectile component too
            playerProj.Initialize(runtimeEffects, direction, m_stats);
        }
        else {
            Debug.LogWarning($"{gameObject.name}: Spawned projectile has no Projectile or EnemyProjectile component!");
            Destroy(projectileObj);
        }
    }
    
    // Editor visualization
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 center = firePoint != null ? firePoint.position : transform.position;
        Gizmos.DrawWireSphere(center, attackRange);
    }
}
