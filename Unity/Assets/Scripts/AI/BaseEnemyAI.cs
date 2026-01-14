using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Linq;

/// <summary>
/// Base class for all enemy AI controllers. Provides shared patrol, vision,
/// state machine, and detection functionality. Subclasses implement attack behavior.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public abstract class BaseEnemyAI : MonoBehaviour
{
    // =========================================================================
    // STATE
    // =========================================================================
    
    protected enum AIState { Patrol, Aggro, Search }
    [Header("AI State")]
    [SerializeField] protected AIState currentState = AIState.Patrol;
    
    // =========================================================================
    // PATROL SETTINGS
    // =========================================================================
    
    [Header("Patrol")]
    [SerializeField] protected Transform patrolParent;
    [SerializeField] protected float waitTimeAtPoint = 2f;
    [SerializeField] protected float patrolSpeed = 1.5f;
    
    protected Transform[] patrolPoints;
    protected int currentPatrolIndex = -1;
    protected float waitTimer = 0f;
    protected bool isWaiting = false;
    
    // =========================================================================
    // VISION SETTINGS
    // =========================================================================
    
    [Header("Vision")]
    [SerializeField] protected float visionRange = 15f;
    [SerializeField] protected float visionAngleHorizontal = 60f;
    [Tooltip("Vertical vision angle (allows looking up/down)")]
    [SerializeField] protected float visionAngleVertical = 30f;
    [SerializeField] protected LayerMask visionMask;
    [Tooltip("Height offset for vision raycast origin")]
    [SerializeField] protected float eyeHeight = 1.0f;
    
    // =========================================================================
    // PROXIMITY DETECTION
    // =========================================================================
    
    [Header("Proximity Detection")]
    [Tooltip("Enemies sense player in 360° within this radius")]
    [SerializeField] protected float proximityRadius = 3f;
    
    // =========================================================================
    // SCAN SETTINGS
    // =========================================================================
    
    [Header("Scanning")]
    [SerializeField] protected float scanRotationSpeed = 120f;
    [SerializeField] protected float scanDuration = 2.5f;
    
    protected bool isScanning = false;
    protected float scanTimer = 0f;
    
    // =========================================================================
    // AGGRO SETTINGS
    // =========================================================================
    
    [Header("Aggro")]
    [Tooltip("Time to keep chasing after losing sight")]
    [SerializeField] protected float postSightTrackDuration = 1.5f;
    [SerializeField] protected float searchSpeed = 2.0f;
    [SerializeField] protected float aggroSpeed = 3.5f;
    
    protected Vector3 lastSeenPlayerPosition;
    protected bool hasLastSeenPosition = false;
    protected bool reachedLastSeen = false;
    protected float loseAggroTimer = 0f;
    
    // =========================================================================
    // REFERENCES
    // =========================================================================
    
    protected NavMeshAgent agent;
    protected Animator animator;
    protected Transform player;
    protected bool canSeePlayer = false;
    protected bool isDead = false;
    
    /// <summary>
    /// Reference to Enemy component for damage events. Can be null if not present.
    /// </summary>
    protected Enemy enemyComponent;
    
    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================
    
    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        enemyComponent = GetComponent<Enemy>();
    }
    
    protected virtual void Start()
    {
        agent.updatePosition = true;
        agent.updateRotation = true;
        
        if (patrolParent != null) {
            patrolPoints = patrolParent.GetComponentsInChildren<Transform>()
                .Where(p => p != patrolParent)
                .ToArray();
        } else {
            patrolPoints = new Transform[0];
        }
        
        PickRandomPatrolPoint();
        
        // Find player
        var playerObj = GameObject.Find("Player_Body");
        if (playerObj != null) {
            player = playerObj.transform;
        }
        
        // Subscribe to damage events for aggro-on-damage
        if (enemyComponent != null) {
            enemyComponent.OnDamageTaken += OnDamageTakenHandler;
        }
    }
    
    protected virtual void OnDestroy()
    {
        if (enemyComponent != null) {
            enemyComponent.OnDamageTaken -= OnDamageTakenHandler;
        }
    }
    
    protected virtual void Update()
    {
        if (isDead) return;
        
        // Check all detection methods
        canSeePlayer = CheckVision() || CheckProximity();
        
        switch (currentState)
        {
            case AIState.Patrol:
                if (canSeePlayer) {
                    SwitchToAggro();
                } else {
                    PatrolBehavior();
                }
                break;
                
            case AIState.Aggro:
                if (canSeePlayer) {
                    UpdateLastSeenPosition();
                    loseAggroTimer = 0f;
                    AggroBehavior();
                } else {
                    // Post-sight tracking: keep chasing for a bit
                    loseAggroTimer += Time.deltaTime;
                    
                    agent.isStopped = false;
                    if (animator != null) animator.SetBool("isWalking", true);
                    
                    if (hasLastSeenPosition) {
                        agent.SetDestination(lastSeenPlayerPosition);
                    }
                    
                    if (loseAggroTimer >= postSightTrackDuration) {
                        loseAggroTimer = 0f;
                        currentState = AIState.Search;
                    }
                }
                break;
                
            case AIState.Search:
                if (canSeePlayer) {
                    SwitchToAggro();
                    UpdateLastSeenPosition();
                } else {
                    SearchBehavior();
                }
                break;
        }
    }
    
    // =========================================================================
    // DETECTION
    // =========================================================================
    
    /// <summary>
    /// Checks if the player is visible using cone vision with both horizontal and vertical FOV.
    /// </summary>
    protected virtual bool CheckVision()
    {
        if (player == null) return false;
        
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 targetPos = player.position + Vector3.up * 1.0f;
        Vector3 toPlayer = targetPos - origin;
        float distance = toPlayer.magnitude;
        
        if (distance > visionRange) return false;
        
        Vector3 toPlayerNormalized = toPlayer.normalized;
        
        // Horizontal angle check (Y-axis rotation)
        Vector3 toPlayerFlat = new Vector3(toPlayer.x, 0, toPlayer.z).normalized;
        Vector3 forwardFlat = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        float horizontalAngle = Vector3.Angle(forwardFlat, toPlayerFlat);
        
        if (horizontalAngle > visionAngleHorizontal) return false;
        
        // Vertical angle check (pitch)
        float verticalAngle = Mathf.Asin(toPlayerNormalized.y) * Mathf.Rad2Deg;
        if (Mathf.Abs(verticalAngle) > visionAngleVertical) return false;
        
        // Raycast to check for obstructions
        if (Physics.Raycast(origin, toPlayerNormalized, out RaycastHit hit, distance, visionMask)) {
            Transform hitT = hit.collider.transform;
            if (hitT == player || hitT.IsChildOf(player)) {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Checks if player is within proximity radius (360° awareness).
    /// </summary>
    protected virtual bool CheckProximity()
    {
        if (player == null) return false;
        
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > proximityRadius) return false;
        
        // Optional: Line of sight check for proximity (can hear through walls?)
        // For now, proximity works through walls (hearing footsteps)
        return true;
    }
    
    // =========================================================================
    // STATE BEHAVIORS
    // =========================================================================
    
    protected virtual void PatrolBehavior()
    {
        agent.speed = patrolSpeed;
        
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        
        if (HasReachedDestination()) {
            if (animator != null) animator.SetBool("isWalking", false);
            agent.isStopped = true;
            
            if (!isScanning) {
                isWaiting = true;
                waitTimer += Time.deltaTime;
                
                if (waitTimer >= waitTimeAtPoint) {
                    waitTimer = 0f;
                    isWaiting = false;
                    isScanning = true;
                    scanTimer = 0f;
                }
                return;
            }
            
            if (isScanning) {
                transform.Rotate(Vector3.up, scanRotationSpeed * Time.deltaTime);
                scanTimer += Time.deltaTime;
                
                if (scanTimer >= scanDuration) {
                    isScanning = false;
                    agent.isStopped = false;
                    PickRandomPatrolPoint();
                }
            }
        } else {
            agent.isStopped = false;
            if (animator != null) animator.SetBool("isWalking", true);
        }
    }
    
    /// <summary>
    /// Override in subclass to implement attack behavior (melee, dash, shoot).
    /// </summary>
    protected abstract void AggroBehavior();
    
    protected virtual void SearchBehavior()
    {
        if (!hasLastSeenPosition) {
            currentState = AIState.Patrol;
            return;
        }
        
        agent.speed = searchSpeed;
        
        if (!reachedLastSeen) {
            agent.isStopped = false;
            agent.SetDestination(lastSeenPlayerPosition);
            if (animator != null) animator.SetBool("isWalking", true);
            
            if (HasReachedDestination()) {
                reachedLastSeen = true;
                agent.isStopped = true;
                if (animator != null) animator.SetBool("isWalking", false);
            }
        } else {
            if (!isScanning) {
                isScanning = true;
                scanTimer = 0f;
            }
            
            transform.Rotate(Vector3.up, scanRotationSpeed * Time.deltaTime);
            scanTimer += Time.deltaTime;
            
            if (scanTimer >= scanDuration) {
                isScanning = false;
                reachedLastSeen = false;
                hasLastSeenPosition = false;
                currentState = AIState.Patrol;
                PickRandomPatrolPoint();
            }
        }
    }
    
    // =========================================================================
    // UTILITY
    // =========================================================================
    
    protected bool HasReachedDestination()
    {
        if (agent.pathPending) return false;
        if (agent.remainingDistance > agent.stoppingDistance) return false;
        return !agent.hasPath || agent.velocity.sqrMagnitude == 0f;
    }
    
    protected void PickRandomPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        
        int newIndex;
        do {
            newIndex = Random.Range(0, patrolPoints.Length);
        } while (newIndex == currentPatrolIndex && patrolPoints.Length > 1);
        
        currentPatrolIndex = newIndex;
    }
    
    protected void SwitchToAggro()
    {
        currentState = AIState.Aggro;
        isWaiting = false;
        isScanning = false;
        reachedLastSeen = false;
        loseAggroTimer = 0f;
    }
    
    protected void UpdateLastSeenPosition()
    {
        lastSeenPlayerPosition = player.position;
        
        // Snap to NavMesh
        if (NavMesh.SamplePosition(lastSeenPlayerPosition, out NavMeshHit hit, 2.0f, NavMesh.AllAreas)) {
            lastSeenPlayerPosition = hit.position;
        }
        
        hasLastSeenPosition = true;
    }
    
    /// <summary>
    /// Called when this enemy takes damage. Triggers immediate aggro.
    /// </summary>
    protected virtual void OnDamageTakenHandler()
    {
        if (isDead) return;
        
        // Immediately switch to aggro
        if (currentState != AIState.Aggro) {
            SwitchToAggro();
            
            // If we can't see the player, at least go to our current position
            // (the player was here when they shot us)
            if (!canSeePlayer && player != null) {
                lastSeenPlayerPosition = player.position;
                if (NavMesh.SamplePosition(lastSeenPlayerPosition, out NavMeshHit hit, 2.0f, NavMesh.AllAreas)) {
                    lastSeenPlayerPosition = hit.position;
                }
                hasLastSeenPosition = true;
            }
        }
    }
    
    /// <summary>
    /// Rotates to face the player. Call from AggroBehavior when attacking.
    /// </summary>
    protected void FacePlayer()
    {
        if (player == null) return;
        
        Vector3 dir = player.position - transform.position;
        dir.y = 0;
        if (dir != Vector3.zero) {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                10f * Time.deltaTime
            );
        }
    }
    
    /// <summary>
    /// Call this when the enemy dies. Override for custom death behavior.
    /// </summary>
    public virtual void Die()
    {
        isDead = true;
        if (agent != null) agent.enabled = false;
    }
    
    // =========================================================================
    // DEBUG
    // =========================================================================
    
    protected virtual void OnDrawGizmos()
    {
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        
        // Vision cone
        Gizmos.color = Color.yellow;
        Vector3 leftBoundary = Quaternion.Euler(0, -visionAngleHorizontal, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, visionAngleHorizontal, 0) * transform.forward;
        Gizmos.DrawLine(origin, origin + leftBoundary * visionRange);
        Gizmos.DrawLine(origin, origin + rightBoundary * visionRange);
        
        // Proximity radius
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, proximityRadius);
        
        // Vision to player (only in play mode)
        if (Application.isPlaying && player != null) {
            Gizmos.color = canSeePlayer ? Color.red : Color.green;
            Gizmos.DrawLine(origin, player.position + Vector3.up);
        }
        
        // Patrol points
        if (patrolParent != null) {
            Gizmos.color = Color.cyan;
            foreach (Transform p in patrolParent) {
                if (p != patrolParent)
                    Gizmos.DrawSphere(p.position, 0.25f);
            }
        }
    }
}
