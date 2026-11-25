using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    [SerializeField] EnemyData data;
    [SerializeField] Animator animator;      // normalmente no filho (o modelo)
    [SerializeField] Transform target;       // se vazio, procura a tag "Player"
    [SerializeField] float crossFadeTime = 0.15f;

    enum State { Patrol, Chase, Attack, Stunned, Dead }

    NavMeshAgent agent;
    Health health;
    State state;
    Vector3 spawnPoint;
    float nextAttackTime;
    float patrolWaitTimer;
    int currentAnim;
    Coroutine routine;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        animator.applyRootMotion = false; // quem move é o NavMeshAgent
    }

    void Start()
    {
        spawnPoint = transform.position;
        health.Init(data.maxHealth);
        health.Damaged += OnDamaged;
        health.Died += OnDied;

        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        ValidateStates();
        SetState(State.Patrol);
    }

    void OnDestroy()
    {
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
    }

    void Update()
    {
        if (state == State.Dead) return;

        UpdateLocomotionAnim();
        if (target == null) { if (state == State.Patrol) Patrol(); return; }

        float dist = Vector3.Distance(transform.position, target.position);

        switch (state)
        {
            case State.Patrol:
                Patrol();
                if (dist <= data.detectionRange) SetState(State.Chase);
                break;

            case State.Chase:
                agent.SetDestination(target.position);
                if (dist <= data.attackRange)
                {
                    FaceTarget();
                    if (Time.time >= nextAttackTime)
                        routine = StartCoroutine(AttackRoutine());
                }
                else if (dist > data.loseRange)
                {
                    SetState(State.Patrol);
                }
                break;
        }
    }

    // ---------- Animação (por nome de estado) ----------
    void Play(string stateName, bool force = false)
    {
        int hash = Animator.StringToHash(stateName);
        if (!force && hash == currentAnim) return;
        currentAnim = hash;
        animator.CrossFadeInFixedTime(hash, crossFadeTime);
    }

    static string Pick(string[] options) =>
        options != null && options.Length > 0 ? options[Random.Range(0, options.Length)] : "";

    void UpdateLocomotionAnim()
    {
        if (state != State.Patrol && state != State.Chase) return;

        bool moving = agent.velocity.sqrMagnitude > 0.05f;
        if (!moving) Play(data.idleState);
        else Play(state == State.Chase ? data.runState : data.walkState);
    }

    void ValidateStates()
    {
        void Check(string n)
        {
            if (!string.IsNullOrEmpty(n) && !animator.HasState(0, Animator.StringToHash(n)))
                Debug.LogWarning($"[{name}] Estado '{n}' não existe no Animator.", this);
        }
        Check(data.idleState); Check(data.walkState); Check(data.runState);
        foreach (var n in data.attackStates) Check(n);
        foreach (var n in data.hitStates) Check(n);
        foreach (var n in data.deathStates) Check(n);
    }

    // ---------- Estados ----------
    void SetState(State newState)
    {
        state = newState;
        switch (newState)
        {
            case State.Patrol:
                agent.isStopped = false;
                agent.speed = data.patrolSpeed;
                agent.stoppingDistance = 0.2f;
                patrolWaitTimer = 0f;
                break;
            case State.Chase:
                agent.isStopped = false;
                agent.speed = data.chaseSpeed;
                agent.stoppingDistance = data.attackRange * 0.8f;
                break;
            case State.Attack:
            case State.Stunned:
                agent.isStopped = true;
                break;
        }
    }

    void Patrol()
    {
        if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.1f) return;

        patrolWaitTimer += Time.deltaTime;
        if (patrolWaitTimer < data.patrolWaitTime) return;
        patrolWaitTimer = 0f;

        Vector3 random = spawnPoint + Random.insideUnitSphere * data.patrolRadius;
        if (NavMesh.SamplePosition(random, out var hit, data.patrolRadius, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    IEnumerator AttackRoutine()
    {
        SetState(State.Attack);
        nextAttackTime = Time.time + data.attackCooldown;
        Play(Pick(data.attackStates), true);

        float t = 0f;
        while (t < data.damageDelay)
        {
            FaceTarget();
            t += Time.deltaTime;
            yield return null;
        }

        if (target != null &&
            Vector3.Distance(transform.position, target.position) <= data.attackRange * 1.3f &&
            target.TryGetComponent(out IDamageable damageable) && !damageable.IsDead)
        {
            damageable.TakeDamage(data.damage);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, data.attackDuration - data.damageDelay));
        SetState(State.Chase);
    }

    IEnumerator HitRoutine()
    {
        SetState(State.Stunned);
        Play(Pick(data.hitStates), true);
        yield return new WaitForSeconds(data.hitStunTime);
        SetState(State.Chase);
    }

    // ---------- Eventos de vida ----------
    void OnDamaged(float amount)
    {
        if (state == State.Dead) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(HitRoutine());
    }

    void OnDied()
    {
        state = State.Dead;
        if (routine != null) StopCoroutine(routine);

        Play(Pick(data.deathStates), true);
        agent.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;

        Destroy(gameObject, data.corpseTime);
    }

    void FaceTarget()
    {
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(dir), 10f * Time.deltaTime);
    }

    void OnDrawGizmosSelected()
    {
        if (data == null) return;
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, data.detectionRange);
        Gizmos.color = Color.red;    Gizmos.DrawWireSphere(transform.position, data.attackRange);
    }
}
