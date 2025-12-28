using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject enemyPrefab;

    [Header("Scaling")]
    public int baseEnemyCount = 1;
    public int enemiesPerExtraPlayer = 1;

    [Header("Spawn Rules")]
    [Tooltip("Minimum distance from player spawn")]
    public float minDistanceFromPlayer = 10f;

    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private MazeGenerator mazeGen;
    private LevelPlayerSpawner playerSpawner;

    void Awake()
    {
        mazeGen = FindFirstObjectByType<MazeGenerator>();
        playerSpawner = FindFirstObjectByType<LevelPlayerSpawner>();
    }

    void OnEnable()
    {
        if (Application.isPlaying && mazeGen != null)
        {
            SpawnEnemies();
        }
    }

    void SpawnEnemies()
    {
        int playerCount = 1;
        if (GameSessionData.Instance?.players != null)
        {
            playerCount = GameSessionData.Instance.players.Count;
        }

        int enemiesToSpawn = baseEnemyCount + (playerCount > 1 ? (playerCount - 1) * enemiesPerExtraPlayer : 0);
        Debug.Log($"Spawning {enemiesToSpawn} enemies for {playerCount} player(s)");

        Vector3 playerPosition = Vector3.zero;
        if (playerSpawner != null)
        {
            playerPosition = playerSpawner.GetPlayerSpawnPosition();
        }

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy(i, playerPosition);
        }
    }

    void SpawnEnemy(int index, Vector3 playerPosition)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError(" Enemy prefab not assigned!");
            return;
        }

        Vector3 spawnPosition;
        if (mazeGen != null)
        {
            spawnPosition = mazeGen.GetSpawnPointAwayFrom(playerPosition, minDistanceFromPlayer);
        }
        else
        {
            spawnPosition = Vector3.zero;
        }

        // CRITICAL: Ensure spawn point is on NavMesh BEFORE instantiating
        NavMeshHit hit;
        if (!NavMesh.SamplePosition(spawnPosition, out hit, 10f, NavMesh.AllAreas))
        {
            Debug.LogError($"Enemy {index + 1} spawn position NOT on NavMesh! Skipping spawn.");
            return; // Don't spawn if not on NavMesh
        }

        spawnPosition = hit.position;

        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        enemy.name = $"Enemy_{index + 1}";

        // Wait one frame before initializing NavMeshAgent
        StartCoroutine(InitializeEnemyAfterSpawn(enemy, index));
    }

    IEnumerator InitializeEnemyAfterSpawn(GameObject enemy, int index)
    {
        yield return null; // Wait one frame

        var agent = enemy.GetComponent<NavMeshAgent>();
        if (agent != null && !agent.isOnNavMesh)
        {
            Debug.LogError($"Enemy {index + 1} NavMeshAgent is NOT on NavMesh after spawn!");
            Destroy(enemy);
            yield break;
        }

        var enemyBehavior = enemy.GetComponent<EnemyBehaviorSystem>();
        if (enemyBehavior != null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                enemyBehavior.player = player.transform;
            }

            var jumpscareManager = FindFirstObjectByType<JumpScareManager>();
            if (jumpscareManager != null)
            {
                enemyBehavior.jumpScareManager = jumpscareManager;
            }

            enemyBehavior.centrePoint = transform;
        }

        var proximityHaptics = enemy.GetComponent<EnemyProximityHaptics>();
        if (proximityHaptics != null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                proximityHaptics.player = player.transform;
            }

            if (enemyBehavior != null)
            {
                proximityHaptics.enemy = enemyBehavior;
            }
        }

        spawnedEnemies.Add(enemy);
        Debug.Log($"Enemy {index + 1} initialized on NavMesh");
    }

    public void ClearEnemies()
    {
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        spawnedEnemies.Clear();
    }
}