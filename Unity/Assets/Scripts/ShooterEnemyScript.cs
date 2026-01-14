using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Shooter enemy AI controller. Extends BaseEnemyAI with ranged attack behavior.
/// Uses EnemyProjectile and SpellDefinition for proper damage system integration.
/// </summary>
[RequireComponent(typeof(StatController))]
public class ShooterEnemyController : BaseEnemyAI
{
    [Header("Shooting")]
    [Tooltip("The spell definition to fire (projectile + effects)")]
    [SerializeField] private SpellDefinition spellDefinition;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float shootCooldown = 1.5f;
    [SerializeField] private float shootRange = 12f;
    [Tooltip("Accuracy deviation in degrees")]
    [SerializeField] private float accuracyDegrees = 5f;
    [Tooltip("Height offset to aim at (0.5 = chest, 1.0 = head)")]
    [SerializeField] private float aimHeightOffset = 0.5f;
    
    [Header("Random Spell (Debug/Variety)")]
    [Tooltip("If true, generates a random constrained spell on spawn instead of using spellDefinition")]
    [SerializeField] private bool useRandomSpell = false;
    
    private float shootTimer = 0f;
    private StatController m_stats;
    private Collider col;
    
    protected override void Awake()
    {
        base.Awake();
        m_stats = GetComponent<StatController>();
        col = GetComponent<Collider>();
        
        // Generate random spell if enabled
        if (useRandomSpell && spellDefinition != null && spellDefinition.projectilePrefab != null) {
            spellDefinition = RandomSpellGenerator.GenerateEnemySpell(spellDefinition.projectilePrefab);
            Debug.Log($"[{gameObject.name}] Generated random enemy spell");
        }
    }
    
    protected override void AggroBehavior()
    {
        if (isDead || player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        shootTimer += Time.deltaTime;
        
        if (distanceToPlayer <= shootRange)
        {
            // In shooting range - stop and shoot
            agent.isStopped = true;
            if (animator != null) animator.SetBool("isWalking", false);
            
            FacePlayer();
            
            if (shootTimer >= shootCooldown)
            {
                ShootAtPlayer();
                shootTimer = 0f;
            }
        }
        else
        {
            // Move closer to get in range
            agent.isStopped = false;
            agent.speed = aggroSpeed;
            agent.SetDestination(player.position);
            if (animator != null) animator.SetBool("isWalking", true);
        }
    }
    
    private void ShootAtPlayer()
    {
        if (animator != null) animator.SetTrigger("isAttacking");
        
        if (spellDefinition == null || spellDefinition.projectilePrefab == null) {
            Debug.LogWarning($"{gameObject.name}: ShooterEnemyController has no spell definition or projectile prefab!");
            return;
        }
        
        if (player == null || shootPoint == null) return;
        
        Vector3 spawnPos = shootPoint.position;
        
        // Aim at player's center mass, not too high
        Vector3 targetPos = player.position + Vector3.up * aimHeightOffset;
        Vector3 direction = (targetPos - spawnPos).normalized;
        
        // Apply accuracy deviation
        direction = ApplyAccuracy(direction, accuracyDegrees);
        
        // Spawn the projectile
        var projectileObj = Instantiate(spellDefinition.projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
        
        // Clone effects for runtime instances
        List<SpellEffect> runtimeEffects = new List<SpellEffect>();
        foreach (var effect in spellDefinition.effects) {
            runtimeEffects.Add(Instantiate(effect));
        }
        
        // Initialize the projectile with the spell system
        if (projectileObj.TryGetComponent<EnemyProjectile>(out var enemyProj)) {
            enemyProj.Initialize(runtimeEffects, direction, m_stats);
        }
        else if (projectileObj.TryGetComponent<Projectile>(out var playerProj)) {
            // Fallback to regular Projectile component
            playerProj.Initialize(runtimeEffects, direction, m_stats);
        }
        else {
            Debug.LogWarning($"{gameObject.name}: Spawned projectile has no Projectile or EnemyProjectile component!");
            Destroy(projectileObj);
        }
    }
    
    private Vector3 ApplyAccuracy(Vector3 direction, float maxDegrees)
    {
        float yaw = Random.Range(-maxDegrees, maxDegrees);
        float pitch = Random.Range(-maxDegrees, maxDegrees);
        return Quaternion.Euler(pitch, yaw, 0) * direction;
    }
    
    public override void Die()
    {
        base.Die();
        
        if (col != null) col.enabled = false;
        if (animator != null) animator.SetBool("isDead", true);
        
        StartCoroutine(DestroyAfterDelay(5f));
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
    
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        
        // Draw shoot range
        Gizmos.color = Color.red;
        Vector3 center = shootPoint != null ? shootPoint.position : transform.position;
        Gizmos.DrawWireSphere(center, shootRange);
    }
}
