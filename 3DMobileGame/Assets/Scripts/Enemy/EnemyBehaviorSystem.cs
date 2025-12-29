using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviorSystem : MonoBehaviour
{
    [Header("Face Models")]
    public GameObject angryFace;
    public GameObject happyFace;
    public GameObject staticFace;

    [Header("Behavior Settings")]
    public float behaviorSwitchInterval = 30f;
    public EnemyMode startingMode = EnemyMode.Angry;

    [Header("Chase Settings")]
    public float angryChaseSpeed = 5f;
    public float happyChaseSpeed = 3f;
    public float staticChaseSpeed = 3.5f;

    [Header("Patrol Settings")]
    public float patrolSpeed = 1.5f;

    [Header("Jumpscare Pools")]
    public JumpscareData[] angryJumpscares;
    public JumpscareData[] happyJumpscares;
    public JumpscareData[] staticJumpscares;

    [Header("References")]
    public NavMeshAgent agent;
    [HideInInspector] public Transform player;
    public AudioSource audioSource;
    [HideInInspector] public JumpScareManager jumpScareManager;
    public Animator animator;

    [Header("Patrol Settings")]
    public float patrolRange = 15f;
    [HideInInspector] public Transform centrePoint;
    public float detectionRange = 8f;
    public float losePlayerTime = 3f;

    [Header("Post-Capture")]
    public float stunDuration = 3f;
    public float retreatDistance = 10f;

    private float idleTimer = 0f;
    private float idleDuration = 1.5f;

    private EnemyMode currentMode;
    private bool isStunned = false;
    private bool isChasing = false;
    private float lastSeenPlayerTime;
    private float normalSpeed;

    public enum EnemyMode { Angry, Happy, Static }

    void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (animator == null)
            animator = GetComponent<Animator>();

        normalSpeed = patrolSpeed;
        agent.speed = normalSpeed;

        if (centrePoint == null)
            centrePoint = transform;

        SetMode(startingMode);
        StartCoroutine(ModeSwitchRoutine());
    }

    void Update()
    {
        if (isStunned || player == null) { UpdateAnimator(); return; }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
        {
            if (!isChasing) StartChase();
            lastSeenPlayerTime = Time.time;
            agent.SetDestination(player.position);
        }
        else if (Time.time - lastSeenPlayerTime <= losePlayerTime)
        {
            agent.SetDestination(player.position);
        }
        else
        {
            if (isChasing) StopChase();

            if (agent.pathPending == false && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (idleTimer <= 0f)
                {
                    Vector3 point;
                    if (RandomPoint(centrePoint.position, patrolRange, out point))
                    {
                        agent.SetDestination(point);
                        idleDuration = Random.Range(0.5f, 2f);
                    }
                }
                else
                {
                    idleTimer -= Time.deltaTime;
                    agent.isStopped = true;
                }
            }
            else
            {
                agent.isStopped = false;
                idleTimer = idleDuration;
            }
        }

        UpdateAnimator();
    }

    void UpdateAnimator()
    {
        if (animator == null || agent == null) return;

        float speed = agent.velocity.magnitude;

        animator.SetFloat("Speed", speed);
        animator.SetBool("IsChasing", isChasing);
    }



    void StartChase()
    {
        isChasing = true;

        switch (currentMode)
        {
            case EnemyMode.Angry:
                agent.speed = angryChaseSpeed;
                break;
            case EnemyMode.Happy:
                agent.speed = happyChaseSpeed;
                break;
            case EnemyMode.Static:
                agent.speed = staticChaseSpeed;
                break;
        }

        Debug.Log($"Enemy started chasing in {currentMode} mode!");
    }

    void StopChase()
    {
        isChasing = false;
        agent.speed = patrolSpeed;
    }

    IEnumerator ModeSwitchRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(behaviorSwitchInterval);
            EnemyMode newMode = (EnemyMode)Random.Range(0, 3);
            SetMode(newMode);
        }
    }

    void SetMode(EnemyMode mode)
    {
        currentMode = mode;

        if (angryFace != null) angryFace.SetActive(false);
        if (happyFace != null) happyFace.SetActive(false);
        if (staticFace != null) staticFace.SetActive(false);

        switch (mode)
        {
            case EnemyMode.Angry:
                if (angryFace != null) angryFace.SetActive(true);
                break;
            case EnemyMode.Happy:
                if (happyFace != null) happyFace.SetActive(true);
                break;
            case EnemyMode.Static:
                if (staticFace != null) staticFace.SetActive(true);
                break;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        if (!isStunned)
            agent.isStopped = false;
        agent.velocity = Vector3.zero;

        animator.SetTrigger("Attack");

        TriggerJumpscareByMode();

        collision.gameObject
            .GetComponent<PlayerLivesSystem>()
            ?.LoseLife();

        StartCoroutine(StunAndRetreat());
    }

    void TriggerJumpscareByMode()
    {
        if (jumpScareManager == null)
        {
            Debug.LogWarning("JumpScareManager not assigned!");
            return;
        }

        JumpscareData[] pool = null;

        switch (currentMode)
        {
            case EnemyMode.Angry:
                pool = angryJumpscares;
                break;
            case EnemyMode.Happy:
                pool = happyJumpscares;
                break;
            case EnemyMode.Static:
                pool = staticJumpscares;
                break;
        }

        if (pool == null || pool.Length == 0)
        {
            Debug.LogWarning($"No jumpscares configured for {currentMode} mode!");
            return;
        }

        JumpscareData selectedJumpscare = pool[Random.Range(0, pool.Length)];

        jumpScareManager.TriggerCustomJumpscare(
            selectedJumpscare.image,
            selectedJumpscare.audio
        );

#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    IEnumerator StunAndRetreat()
    {
        isStunned = true;
        isChasing = false;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        float effectiveStun = stunDuration + 2.5f;


        yield return new WaitForSeconds(effectiveStun);

        if (player != null)
        {
            Vector3 retreatDirection = (transform.position - player.position).normalized;
            Vector3 retreatPoint = transform.position + retreatDirection * retreatDistance;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(retreatPoint, out hit, retreatDistance, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
            }
        }

        agent.isStopped = false;
        isStunned = false;
    }

    bool RandomPoint(Vector3 center, float range, out Vector3 result)
    {
        Vector3 randomPoint = center + Random.insideUnitSphere * range;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }
        result = Vector3.zero;
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (centrePoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(centrePoint.position, patrolRange);
        }
    }
}

[System.Serializable]
public class JumpscareData
{
    public string name;
    public Sprite image;
    public AudioClip audio;
}