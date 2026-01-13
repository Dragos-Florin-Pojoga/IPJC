using UnityEngine;
using UnityEngine.AI; // MODIFICARE: Necesar pentru NavMesh
using System.Collections;
using UnityEngine.InputSystem;
using System.Linq;
using System;
using Random = UnityEngine.Random;
using Unity.Burst.CompilerServices;
using static UnityEngine.UI.Image;

// MODIFICARE: Cere automat componenta NavMeshAgent
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("NavMesh Settings")]
    private NavMeshAgent agent; // Referinta catre agent

    [Header("PatrolPoints")]
    [SerializeField] private Transform patrolParent;
    [SerializeField] private float waitTimeAtPoint = 2f;
    // arrivalDistance e gestionat acum de agent.stoppingDistance

    private bool isWaiting = false;
    private Transform[] patrolPoints;
    private int currentPatrolIndex = 0;
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

    // searchArrivalDistance gestionat de agent

    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    private bool isDead = false;

    private Collider col;
    [SerializeField] private FloatingHealthBar healthBar;

    private enum AIState { Patrol, Aggro, Search }
    [SerializeField] private AIState currentState = AIState.Patrol;

    [Header("Aggro")]
    [SerializeField] private float loseAggroDelay = 0.5f;
    private float loseAggroTimer = 0f;
    private Vector3 lastSeenPlayerPosition;
    private bool hasLastSeenPosition = false;
    private bool reachedLastSeen = false;

    Animator animator;
    [SerializeField] Transform warriorVisual;

    [SerializeField] private float attackRange = 2.0f;
    [SerializeField] private float attackCooldown = 1.5f;
    private float attackTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>(); // Initializare NavMesh
        animator = GetComponentInChildren<Animator>();
        col = GetComponent<Collider>();
        currentHealth = maxHealth;

        // Configurare Agent
        agent.updateRotation = true; // Lasa agentul sa se roteasca singur cand merge
        agent.updatePosition = true;

        if (healthBar != null)
            healthBar.UpdateHealthBar(currentHealth, maxHealth);

        patrolPoints = patrolParent.GetComponentsInChildren<Transform>()
            .Where(p => p != patrolParent)
            .ToArray();

        PickRandomPatrolPoint();
    }

    // Aceasta functie inlocuieste logica veche de verificare distanta
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

    void PatrolBehaviour()
    {
        agent.speed = 1.5f; // Viteza de patrol
        if (patrolPoints.Length == 0) return;

        Vector3 target = patrolPoints[currentPatrolIndex].position;
        agent.SetDestination(target); // MODIFICARE: NavMesh calculeaza calea

        // ================= ARRIVED =================
        if (HasReachedDestination())
        {
            animator.SetBool("isWalking", false);

            // Opreste agentul ca sa nu alunece
            agent.isStopped = true;

            if (!isScanning)
            {
                isWaiting = true;
                waitTimer += Time.deltaTime; // NavMesh merge pe Update, folosim deltaTime

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
                // Scanning logic (Rotatie manuala)
                // Dezactivam rotatia automata a agentului temporar daca vrem control manual fin
                transform.Rotate(Vector3.up, scanRotationSpeed * Time.deltaTime);

                scanTimer += Time.deltaTime;

                if (CheckVision())
                {
                    Debug.Log("Player spotted during scan!");
                    return;
                }

                if (scanTimer >= scanDuration)
                {
                    isScanning = false;
                    agent.isStopped = false; // Pornim din nou agentul
                    PickRandomPatrolPoint();
                }
                return;
            }
        }
        else
        {
            // Moving
            agent.isStopped = false;
            animator.SetBool("isWalking", true);
        }
    }





    void AggroBehaviour()
    {
        if (isDead) return;

        agent.speed = 3.5f;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

  
        if (distanceToPlayer <= attackRange)
        {
            agent.isStopped = true;
            animator.SetBool("isWalking", false);


            Vector3 dir = player.position - transform.position;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
            }

            attackTimer += Time.deltaTime;
            if (attackTimer >= attackCooldown)
            {
                animator.SetTrigger("isAttacking");
                attackTimer = 0f;
            }
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            animator.SetBool("isWalking", true);

            attackTimer += Time.deltaTime;
        }
    }






    void SearchBehaviour()
    {
        if (!hasLastSeenPosition)
        {
            currentState = AIState.Patrol;
            return;
        }

        agent.speed = 2.5f;

        // Move to last seen
        if (!reachedLastSeen)
        {
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
            // Scan logic la destinatie
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

    // Folosim Update in loc de FixedUpdate pentru NavMesh (NavMesh nu e fizica pura)
    void Update()
    {
        if (isDead) return;

        // Debug Input
        if (Keyboard.current.hKey.wasPressedThisFrame) TakeDamage(25f);




        canSeePlayer = CheckVision();



        // State Machine logic (identic cu ce aveai, doar curatat putin)
        switch (currentState)
        {
            case AIState.Patrol:
                if (canSeePlayer) SwitchToAggro();
                else PatrolBehaviour();
                break;

            case AIState.Aggro:
                if (canSeePlayer)
                {
                    lastSeenPlayerPosition = player.position;
                    // Fix pentru NavMesh: Asigura-te ca punctul e pe NavMesh
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
                    // FIX: Pornim agentul si animatia cand pierdem player-ul
                    agent.isStopped = false;
                    animator.SetBool("isWalking", true);

                    loseAggroTimer += Time.deltaTime;
                    // Continuam sa mergem spre ultima pozitie cunoscuta cat timp pierdem aggro
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
                if (canSeePlayer) SwitchToAggro();
                else SearchBehaviour();
                break;
        }
    }

    void SwitchToAggro()
    {
        currentState = AIState.Aggro;
        isWaiting = false;
        isScanning = false;
        reachedLastSeen = false;
    }

    // Restul metodelor (CheckVision, TakeDamage, Die, Gizmos) raman la fel
    // Doar asigura-te ca in Die() opresti agentul:
    private void Die()
    {
        isDead = true;
        agent.enabled = false; // IMPORTANT: Oprim agentul cand moare
        if (col != null) col.enabled = false;
        animator.SetBool("isWalking", false);
        StartCoroutine(DestroyAfterDelay(5f));
        StartCoroutine(EnableCorpseCollider());
    }


    bool CheckVision()
    {
        if (player == null) return false;

        // 1. RIDICI ORIGINEA (Inamicul)
        // Schimba 1.5f cu 1.8f sau 2.0f ca sa plece raza mai de sus
        Vector3 origin = transform.position + Vector3.up * 1.0f;

        // 2. RIDICI TINTA (Player-ul)
        // Schimba 1.0f cu 1.5f sau 1.8f ca sa tintesti mai sus in player
        Vector3 targetPos = player.position + Vector3.up * 1.0f;

        // --- De aici in jos e la fel ---

        Debug.DrawLine(origin, targetPos, Color.blue); // Linia albastra de debug

        Vector3 direction = (targetPos - origin).normalized;

        // ... restul codului tau de unghi si raycast ...
        if (Vector3.Angle(transform.forward, direction) > visionAngle) return false;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, visionRange, visionMask))
        {
            if (hit.collider.transform == player || hit.collider.transform.IsChildOf(player))
                return true;
        }
        return false;
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
        Quaternion startRot = warriorVisual.rotation;
        Quaternion endRot = Quaternion.Euler(startRot.eulerAngles + new Vector3(90f, 0f, 0f));

        float duration = 1.2f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            warriorVisual.rotation = Quaternion.Slerp(startRot, endRot, t / duration);
            yield return null;
        }


        col.enabled = true;

    }



    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }



    void OnDrawGizmos()
    {
        if (patrolParent == null) return;

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
}