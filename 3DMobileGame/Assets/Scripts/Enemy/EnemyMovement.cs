using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

    // Start is called before the first frame update
    void Start()
    {
        Agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, Player.position);

        if (distanceToPlayer <= DetectionRange)
        {
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
}
