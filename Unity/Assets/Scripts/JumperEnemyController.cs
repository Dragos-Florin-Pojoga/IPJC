using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.InputSystem;
using System.Linq;
using System;
using Random = UnityEngine.Random;
using static UnityEngine.UI.Image;

[RequireComponent(typeof(NavMeshAgent))]
public class JumperEnemyController : MonoBehaviour
{
    [Header("NavMesh")]
    private NavMeshAgent agent;

    [Header("PatrolPoints")]
    [SerializeField] private Transform patrolParent;
    [SerializeField] private float waitTimeAtPoint = 2f;
    [SerializeField] private float arrivalDistance = 0.4f;

    private bool isWaiting = false;
    private Transform[] patrolPoints;
    private int currentPatrolIndex = -1;
    private float waitTimer = 0f;

    [Header("Vision")]
    [SerializeField] private float visionRange = 15f;
    [SerializeField] private float visionAngle = 60f;
    [SerializeField] private LayerMask visionMask;
    [SerializeField] private Transform player;
    private bool canSeePlayer = false;

    [Header("Scan")]
    [SerializeField] private float scanRotationSpeed = 120f;
    [SerializeField] private float scanDuration = 2.5f;

    private bool isScanning = false;
    private float scanTimer = 0f;

    [SerializeField] private float searchArrivalDistance = 0.8f;

    [Header("Stats")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;
    private bool isDead = false;

    private Rigidbody rb;
    private Collider col;

    [SerializeField] private FloatingHealthBar healthBar;

   
    [Header("Dash")]
    [Tooltip("Distanta maxima la care poate porni un dash.")]
    [SerializeField] private float jumpRange = 6f;           
    [Tooltip("Cooldown intre doua dash-uri.")]
    [SerializeField] private float jumpCooldown = 1.5f;      
    [Tooltip("Cat dureaza un dash (secunde).")]
    [SerializeField] private float dashDuration = 0.4f;
    [Tooltip("Viteza de baza in chase.")]
    [SerializeField] private float baseChaseSpeed = 2.5f;
    [Tooltip("Viteza in timpul dash-ului.")]
    [SerializeField] private float dashSpeed = 6.0f;
    [Tooltip("Viteza la patrulare.")]
    [SerializeField] private float patrolSpeed = 1.5f;
    [Tooltip("Viteza în Search.")]
    [SerializeField] private float searchSpeed = 2.0f;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float jumpCooldownTimer = 0f;

    private enum AIState { Patrol, Aggro, Search }
    [SerializeField] private AIState currentState = AIState.Patrol;

    [Header("Aggro")]
    [SerializeField] private float loseAggroDelay = 0.5f;
    private float loseAggroTimer = 0f;

    private Vector3 lastSeenPlayerPosition;
    private bool hasLastSeenPosition = false;
    private bool reachedLastSeen = false;

    Animator animator;
    [SerializeField] Transform spiderVisual;

    [Header("Attack")]
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackCooldown = 1.5f;
    private float attackTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (healthBar != null)
            healthBar.UpdateHealthBar(currentHealth, maxHealth);

        patrolPoints = patrolParent.GetComponentsInChildren<Transform>()
            .Where(p => p != patrolParent)
            .ToArray();

       
        agent.updatePosition = true;
        agent.updateRotation = true;

        PickRandomPatrolPoint();
    }


    bool HasReachedDestination()
    {
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    return true;
                }
            }
        }
        return false;
    }

    bool CheckVision()
    {
        if (player == null) return false;

        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Vector3 targetPos = player.position + Vector3.up * 1.0f;

        Vector3 toPlayer = (targetPos - origin).normalized;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > visionAngle) return false;

        if (Physics.Raycast(origin, toPlayer, out RaycastHit hit, visionRange, visionMask))
        {
            Transform hitT = hit.collider.transform;
            if (hitT == player || hitT.IsChildOf(player))
                return true;
        }

        return false;
    }

    void PatrolBehaviour()
    {
        agent.speed = patrolSpeed;

        if (patrolPoints.Length == 0) return;

        Vector3 target = patrolPoints[currentPatrolIndex].position;
        agent.SetDestination(target);

        if (HasReachedDestination())
        {
            animator.SetBool("isWalking", false);
            agent.isStopped = true;

            if (!isScanning)
            {
                isWaiting = true;
                waitTimer += Time.deltaTime;

                if (waitTimer >= waitTimeAtPoint)
                {
                    waitTimer = 0f;
                    isWaiting = false;
                    isScanning = true;
                    scanTimer = 0f;
                }
                return;
            }

            if (isScanning)
            {
                transform.Rotate(Vector3.up, scanRotationSpeed * Time.deltaTime);
                scanTimer += Time.deltaTime;

                if (CheckVision())
                {
                    return;
                }

                if (scanTimer >= scanDuration)
                {
                    isScanning = false;
                    agent.isStopped = false;
                    PickRandomPatrolPoint();
                }
                return;
            }
        }
        else
        {
            agent.isStopped = false;
            animator.SetBool("isWalking", true);
        }
    }

    void AggroBehaviour()
    {
        if (isDead) return;
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

       
        attackTimer += Time.deltaTime;

        if (distanceToPlayer <= attackRange)
        {
            agent.isStopped = true;
            animator.SetBool("isWalking", false);

            Vector3 dir = player.position - transform.position;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dir),
                    10f * Time.deltaTime
                );
            }

            if (attackTimer >= attackCooldown)
            {
                animator.SetTrigger("isAttacking");
                attackTimer = 0f;
            }

            return;
        }

        
        agent.isStopped = false;
        animator.SetBool("isWalking", true);

        // update cooldown
        jumpCooldownTimer += Time.deltaTime;

        if (isDashing)
        {
            dashTimer += Time.deltaTime;
            
            agent.speed = dashSpeed;
            agent.SetDestination(player.position);

            if (dashTimer >= dashDuration)
            {
                isDashing = false;
                agent.speed = baseChaseSpeed;
            }
        }
        else
        {
           
            if (distanceToPlayer < jumpRange && jumpCooldownTimer >= jumpCooldown)
            {
                isDashing = true;
                dashTimer = 0f;
                jumpCooldownTimer = 0f;
                agent.speed = dashSpeed;

               
                agent.SetDestination(player.position);

               
                animator.SetTrigger("isAttacking");
            }
            else
            {
               
                agent.speed = baseChaseSpeed;
                agent.SetDestination(player.position);
            }
        }
    }

    void SearchBehaviour()
    {
        if (!hasLastSeenPosition)
        {
            currentState = AIState.Patrol;
            return;
        }

        agent.speed = searchSpeed;

        if (!reachedLastSeen)
        {
            agent.isStopped = false;
            agent.SetDestination(lastSeenPlayerPosition);
            animator.SetBool("isWalking", true);

            if (HasReachedDestination())
            {
                reachedLastSeen = true;
                agent.isStopped = true;
                animator.SetBool("isWalking", false);
            }
        }
        else
        {
            if (!isScanning)
            {
                isScanning = true;
                scanTimer = 0f;
            }

            transform.Rotate(Vector3.up, scanRotationSpeed * Time.deltaTime);
            scanTimer += Time.deltaTime;

            if (CheckVision())
            {
                reachedLastSeen = false;
                currentState = AIState.Aggro;
                return;
            }

            if (scanTimer >= scanDuration)
            {
                isScanning = false;
                reachedLastSeen = false;
                hasLastSeenPosition = false;
                currentState = AIState.Patrol;
                PickRandomPatrolPoint();
            }
        }
    }

    void SwitchToAggro()
    {
        currentState = AIState.Aggro;
        isWaiting = false;
        isScanning = false;
        reachedLastSeen = false;
        
        isDashing = false;
        dashTimer = 0f;
    }

    void PickRandomPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;

        int newIndex;
        do
        {
            newIndex = Random.Range(0, patrolPoints.Length);
        }
        while (newIndex == currentPatrolIndex && patrolPoints.Length > 1);

        currentPatrolIndex = newIndex;
    }

    void OnDrawGizmos()
    {
        if (patrolParent == null || player == null) return;

        Gizmos.color = Color.cyan;
        foreach (Transform p in patrolParent)
            Gizmos.DrawSphere(p.position, 0.25f);

        Gizmos.color = canSeePlayer ? Color.red : Color.green;
        Gizmos.DrawLine(
            transform.position + Vector3.up * 0.5f,
            player.position
        );

        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Gizmos.color = Color.yellow;

        Vector3 leftBoundary = Quaternion.Euler(0, -visionAngle, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, visionAngle, 0) * transform.forward;

        Gizmos.DrawLine(origin, origin + leftBoundary * visionRange);
        Gizmos.DrawLine(origin, origin + rightBoundary * visionRange);
    }

    public void Update()
    {
        if (isDead) return;

        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            Debug.Log("Enemy takes damage!");
            TakeDamage(25f);
        }

        canSeePlayer = CheckVision();
        bool seesPlayer = canSeePlayer;

        switch (currentState)
        {
            case AIState.Patrol:
                if (seesPlayer)
                {
                    Debug.Log("ENTER AGGRO");
                    SwitchToAggro();
                }
                else
                {
                    PatrolBehaviour();
                }
                break;

            case AIState.Aggro:
                if (seesPlayer)
                {
                    lastSeenPlayerPosition = player.position;

                    
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(lastSeenPlayerPosition, out hit, 2.0f, NavMesh.AllAreas))
                    {
                        lastSeenPlayerPosition = hit.position;
                    }

                    hasLastSeenPosition = true;
                    loseAggroTimer = 0f;

                    AggroBehaviour();
                }
                else
                {
  
                    agent.isStopped = false;
                    animator.SetBool("isWalking", true);

                    loseAggroTimer += Time.deltaTime;

                    if (hasLastSeenPosition)
                        agent.SetDestination(lastSeenPlayerPosition);

                    if (loseAggroTimer >= loseAggroDelay)
                    {
                        Debug.Log("Lost player -> SEARCH");
                        loseAggroTimer = 0f;
                        currentState = AIState.Search;
                    }
                }
                break;

            case AIState.Search:
                if (seesPlayer)
                {
                    Debug.Log("SEARCH -> AGGRO");
                    SwitchToAggro();

                    lastSeenPlayerPosition = player.position;
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(lastSeenPlayerPosition, out hit, 2.0f, NavMesh.AllAreas))
                    {
                        lastSeenPlayerPosition = hit.position;
                    }

                    hasLastSeenPosition = true;
                }
                else
                {
                    SearchBehaviour();
                }
                break;
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthBar != null)
            healthBar.UpdateHealthBar(currentHealth, maxHealth);

        if (currentHealth <= 0)
            Die();
    }

    IEnumerator EnableCorpseCollider()
    {
        Quaternion startRot = spiderVisual.rotation;
        Quaternion endRot = Quaternion.Euler(startRot.eulerAngles + new Vector3(180f, 0f, 0f));

        float duration = 1.2f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            spiderVisual.rotation = Quaternion.Slerp(startRot, endRot, t / duration);
            yield return null;
        }

        if (col != null)
            col.enabled = true;

        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    private void Die()
    {
        isDead = true;

        if (agent != null)
            agent.enabled = false;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.None;
        }

        if (col != null)
            col.enabled = false;

        animator.SetBool("isDead", true);

        StartCoroutine(DestroyAfterDelay(5f));
        StartCoroutine(EnableCorpseCollider());
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}
