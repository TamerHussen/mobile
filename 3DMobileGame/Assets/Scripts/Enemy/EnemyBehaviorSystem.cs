using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviorSystem : MonoBehaviour
{
    [Header("Face Models (Visual State Only)")]
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

    [Header("Jumpscare Pools - ANGRY MODE")]
    [Tooltip("Traditional scary jumpscares for Angry mode")]
    public JumpscareData[] angryJumpscares;

    [Header("Jumpscare Pools - HAPPY MODE")]
    [Tooltip("Meme/funny jumpscares for Happy mode")]
    public JumpscareData[] happyJumpscares;

    [Header("Jumpscare Pools - STATIC MODE")]
    [Tooltip("Mismatched/broken jumpscares (scary image + funny sound OR funny image + scary sound)")]
    public JumpscareData[] staticJumpscares;

    [Header("References")]
    public NavMeshAgent agent;
    [HideInInspector] public Transform player; // Assigned by spawner
    public AudioSource audioSource;
    [HideInInspector] public JumpScareManager jumpScareManager; // Assigned by spawner

    [Header("Patrol Settings")]
    public float patrolRange = 15f;
    [HideInInspector] public Transform centrePoint; // Assigned by spawner
    public float detectionRange = 8f;
    public float losePlayerTime = 3f;

    [Header("Post-Capture")]
    public float stunDuration = 3f;
    public float retreatDistance = 10f;

    private EnemyMode currentMode;
    private bool isStunned = false;
    private bool isChasing = false;
    private float lastSeenPlayerTime;
    private float normalSpeed;

    public enum EnemyMode
    {
        Angry,   // Scary jumpscares
        Happy,   // Meme/funny jumpscares
        Static   // Mismatched jumpscares
    }

    void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        normalSpeed = agent.speed;

        if (centrePoint == null)
            centrePoint = transform;

        SetMode(startingMode);
        StartCoroutine(ModeSwitchRoutine());
    }

    void Update()
    {
        if (isStunned || player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
        {
            if (!isChasing)
                StartChase();

            lastSeenPlayerTime = Time.time;
            agent.SetDestination(player.position);
        }
        else if (Time.time - lastSeenPlayerTime <= losePlayerTime)
        {
            agent.SetDestination(player.position);
        }
        else
        {
            if (isChasing)
                StopChase();

            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                Vector3 point;
                if (RandomPoint(centrePoint.position, patrolRange, out point))
                {
                    agent.SetDestination(point);
                }
            }
        }
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
        agent.speed = normalSpeed;
    }

    IEnumerator ModeSwitchRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(behaviorSwitchInterval);
            EnemyMode newMode = (EnemyMode)Random.Range(0, 3);
            SetMode(newMode);
            Debug.Log($"Enemy switched to {newMode} mode!");
        }
    }

    void SetMode(EnemyMode mode)
    {
        currentMode = mode;

        // Update visual face
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
        if (collision.gameObject.CompareTag("Player"))
        {
            var livesSystem = collision.gameObject.GetComponent<PlayerLivesSystem>();
            if (livesSystem != null && livesSystem.IsInvincible())
            {
                Debug.Log("Player is invincible - no damage");
                return;
            }

            // Trigger jumpscare based on current mode
            TriggerJumpscareByMode();

            if (livesSystem != null)
                livesSystem.LoseLife();

            StartCoroutine(StunAndRetreat());
        }
    }

    void TriggerJumpscareByMode()
    {
        if (jumpScareManager == null)
        {
            Debug.LogWarning("JumpScareManager not assigned!");
            return;
        }

        JumpscareData[] pool = null;

        // Select jumpscare pool based on current mode
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

        // Validate pool
        if (pool == null || pool.Length == 0)
        {
            Debug.LogWarning($"No jumpscares configured for {currentMode} mode!");
            return;
        }

        // Select random jumpscare from pool
        JumpscareData selectedJumpscare = pool[Random.Range(0, pool.Length)];

        // Trigger it
        jumpScareManager.TriggerCustomJumpscare(
            selectedJumpscare.image,
            selectedJumpscare.audio
        );

        Debug.Log($"Triggered {currentMode} jumpscare: {selectedJumpscare.name}");

#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    IEnumerator StunAndRetreat()
    {
        isStunned = true;
        isChasing = false;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        yield return new WaitForSeconds(stunDuration);

        if (player != null && agent != null)
        {
            Vector3 retreatDirection = (transform.position - player.position).normalized;
            Vector3 retreatPoint = transform.position + (retreatDirection * retreatDistance);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(retreatPoint, out hit, retreatDistance, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
            }
        }

        if (agent != null)
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

/// <summary>
/// Serializable jumpscare data - create as many as you need in the Inspector
/// </summary>
[System.Serializable]
public class JumpscareData
{
    public string name; // For organization in Inspector
    public Sprite image;
    public AudioClip audio;
}