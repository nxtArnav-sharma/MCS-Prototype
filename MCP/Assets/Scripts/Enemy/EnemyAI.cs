using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(HealthSystem))]
public class EnemyAI : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack, HitReact, Dead }

    [Header("Detection")]
    public float detectionRange  = 8f;
    public float attackRange     = 2f;
    public float loseTargetRange = 15f;

    [Header("Combat")]
    public float attackDamage    = 15f;
    public float attackCooldown  = 1.8f;
    public float attackWindup    = 0.6f;     // seconds before hit lands

    [Header("Patrol")]
    public Transform[] patrolPoints;
    private int patrolIndex;
    private float waitAtPoint = 2f;
    private float waitTimer;

    [Header("State")]
    public State currentState = State.Idle;

    private NavMeshAgent agent;
    private Animator animator;
    private HealthSystem health;
    private Transform player;
    private float lastAttackTime;
    private bool hitLanded;

    private static readonly int SpeedHash   = Animator.StringToHash("Speed");
    private static readonly int AttackHash  = Animator.StringToHash("Attack");
    private static readonly int HitHash     = Animator.StringToHash("HitReact");
    private static readonly int DeathHash   = Animator.StringToHash("Death");

    void Awake()
    {
        agent   = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health  = GetComponent<HealthSystem>();

        health.onDamageTaken.AddListener(_ => OnHit());
        health.onDeath.AddListener(OnDeath);

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;
    }

    void Update()
    {
        if (currentState == State.Dead) return;

        float distToPlayer = player ? Vector3.Distance(transform.position, player.position) : Mathf.Infinity;

        switch (currentState)
        {
            case State.Idle:
                if (patrolPoints.Length > 0)
                    TransitionTo(State.Patrol);
                else if (distToPlayer < detectionRange)
                    TransitionTo(State.Chase);
                break;

            case State.Patrol:
                DoPatrol();
                if (distToPlayer < detectionRange)
                    TransitionTo(State.Chase);
                break;

            case State.Chase:
                DoChase(distToPlayer);
                break;

            case State.Attack:
                DoAttack();
                break;

            case State.HitReact:
                // Handled by animation event — auto-returns to Chase
                break;
        }

        animator.SetFloat(SpeedHash, agent.velocity.magnitude);
    }

    void DoPatrol()
    {
        if (patrolPoints.Length == 0) return;
        agent.SetDestination(patrolPoints[patrolIndex].position);

        if (agent.remainingDistance < 0.5f)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                waitTimer = waitAtPoint;
            }
        }
    }

    void DoChase(float dist)
    {
        if (dist > loseTargetRange) { TransitionTo(State.Patrol); return; }
        agent.SetDestination(player.position);

        if (dist < attackRange && Time.time - lastAttackTime > attackCooldown)
            TransitionTo(State.Attack);
    }

    void DoAttack()
    {
        agent.ResetPath();
        // Face player
        Vector3 dir = (player.position - transform.position);
        dir.y = 0;
        if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);

        if (!hitLanded) return;   // wait for anim event callback
        hitLanded = false;
        TransitionTo(State.Chase);
    }

    // Called by animation event on the Attack clip at the hit frame
    public void AnimEvent_AttackHit()
    {
        hitLanded = true;
        lastAttackTime = Time.time;

        Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * attackRange * 0.7f,
                                                 0.7f);
        foreach (var c in hits)
        {
            var h = c.GetComponentInParent<HealthSystem>();
            if (h != null && c.gameObject != gameObject)
                h.TakeDamage(attackDamage);
        }
    }

    void OnHit()
    {
        if (currentState == State.Dead) return;
        TransitionTo(State.HitReact);
        Invoke(nameof(RecoverFromHit), 0.5f);
    }

    void RecoverFromHit()
    {
        if (currentState != State.Dead)
            TransitionTo(State.Chase);
    }

    void OnDeath()
    {
        TransitionTo(State.Dead);
        agent.enabled = false;
        GetComponent<Collider>().enabled = false;
        Destroy(gameObject, 4f);
    }

    void TransitionTo(State next)
    {
        currentState = next;
        switch (next)
        {
            case State.Attack:
                animator.SetTrigger(AttackHash);
                hitLanded = false;
                break;
            case State.HitReact:
                animator.SetTrigger(HitHash);
                break;
            case State.Dead:
                animator.SetTrigger(DeathHash);
                break;
        }
    }
}
