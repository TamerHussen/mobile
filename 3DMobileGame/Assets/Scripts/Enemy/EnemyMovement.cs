using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    public NavMeshAgent Agent;
    public float Range;
    public Transform CentrePoint;

    public Transform Player;
    public float DetectionRange = 8f;
    public float LosePlayerTime = 3f;

    private float LastSeenPlayerTime;

    public float proximityHapticRange = 3f;
    public float proximityHapticCooldown = 1.0f;
    public float chaseStartHapticCooldown = 3.0f;

    private float lastProximityHaptic = -999f;
    private float lastChaseStartHaptic = -999f;

    public JumpScareManager jumpScareManager;

    public float freezeTimeAfterScare = 2.5f; // how long enemy freezes
    private bool frozen = false;

    void Start()
    {
        Agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (frozen) return;

        float distanceToPlayer = Vector3.Distance(transform.position, Player.position);

        if (distanceToPlayer <= proximityHapticRange && Time.time - lastProximityHaptic >= proximityHapticCooldown)
        {
            TriggerPlayerHaptic();
            lastProximityHaptic = Time.time;
        }

        if (distanceToPlayer <= DetectionRange)
        {
            if (Time.time - lastChaseStartHaptic >= chaseStartHapticCooldown)
            {
                TriggerPlayerHaptic();
                lastChaseStartHaptic = Time.time;
            }

            LastSeenPlayerTime = Time.time;
            Agent.SetDestination(Player.position);
        }
        else if (Time.time - LastSeenPlayerTime <= LosePlayerTime)
        {
            Agent.SetDestination(Player.position);
        }
        else
        {
            if (Agent.remainingDistance <= Agent.stoppingDistance)
            {
                Vector3 point;
                if (RandomPoint(CentrePoint.position, Range, out point))
                {
                    Debug.DrawRay(point, Vector3.up, Color.red, 1.0f);
                    Agent.SetDestination(point);
                }
            }
        }
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
        result = Vector3.zero;
        return false;
    }

    private void TriggerPlayerHaptic()
    {
        if (Player == null) return;

        var playerController = Player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.Vibrate();
        }
        else
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (jumpScareManager != null)
            {
                jumpScareManager.TriggerRandomJumpScare();
                StartCoroutine(FreezeEnemy()); // enemy freezes after scare
            }
        }
    }

    private IEnumerator FreezeEnemy()
    {
        frozen = true;
        if (Agent != null) Agent.isStopped = true;
        yield return new WaitForSeconds(freezeTimeAfterScare);
        if (Agent != null) Agent.isStopped = false;
        frozen = false;
    }
}
