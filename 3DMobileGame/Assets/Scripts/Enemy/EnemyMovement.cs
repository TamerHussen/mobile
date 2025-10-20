using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    public NavMeshAgent Agent;
    public float Range;
    public Transform CentrePoint;

    public Transform Player;           // Reference to player
    public float DetectionRange = 8f;  // How close player needs to be to chase
    public float LosePlayerTime = 3f;  // Time before going back to patrol

    private float LastSeenPlayerTime;

    // --- HAPTIC SETTINGS (small, tweak in inspector) ---
    [Header("Haptics")]
    public float proximityHapticRange = 3f;   // vibrate when player within this distance
    public float proximityHapticCooldown = 1.0f; // seconds between proximity vibrates
    public float chaseStartHapticCooldown = 3.0f; // seconds between chase-start vibrates

    private float lastProximityHaptic = -999f;
    private float lastChaseStartHaptic = -999f;

    public JumpScareManager jumpScareManager;


    void Start()
    {
        Agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, Player.position);

        // HAPTIC: proximity pulse (throttled)
        if (distanceToPlayer <= proximityHapticRange && Time.time - lastProximityHaptic >= proximityHapticCooldown)
        {
            TriggerPlayerHaptic();
            lastProximityHaptic = Time.time;
        }

        if (distanceToPlayer <= DetectionRange)
        {
            // haptic when chase begins (throttled)
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
                    Debug.DrawRay(point, Vector3.up, Color.red, 1.0f); // shows enemy tracks
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
        return false;
    }

    // Calls the player's Vibrate helper if available.
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
            // fallback to generic vibrate (device)
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
                jumpScareManager.TriggerRandomJumpScare();
        }
    }

}
