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
    public int maxSpawnAttempts = 10;

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
            StartCoroutine(WaitAndSpawnEnemies());
        }
    }

    IEnumerator WaitAndSpawnEnemies()
    {
        // Wait for NavMesh to be built
        yield return new WaitForSeconds(1f);

        // Wait for player to spawn
        int waitFrames = 0;
        while (playerSpawner == null || playerSpawner.GetSpawnedPlayer() == null)
        {
            playerSpawner = FindFirstObjectByType<LevelPlayerSpawner>();
            yield return null;

            waitFrames++;
            if (waitFrames > 100)
            {
                Debug.LogWarning("⚠️ Player didn't spawn in time, spawning enemies anyway");
                break;
            }
        }

        SpawnEnemies();
    }

    void SpawnEnemies()
    {
        int playerCount = 1;
        if (GameSessionData.Instance?.players != null)
        {
            playerCount = GameSessionData.Instance.players.Count;
        }

        int enemiesToSpawn = baseEnemyCount + (playerCount > 1 ? (playerCount - 1) * enemiesPerExtraPlayer : 0);
        Debug.Log($"👹 Spawning {enemiesToSpawn} enemies for {playerCount} player(s)");

        Vector3 playerPosition = Vector3.zero;
        if (playerSpawner != null && playerSpawner.GetSpawnedPlayer() != null)
        {
            playerPosition = playerSpawner.GetPlayerSpawnPosition();
        }

        int successfulSpawns = 0;
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            if (SpawnEnemy(i, playerPosition))
            {
                successfulSpawns++;
            }
        }

        Debug.Log($"✓ Successfully spawned {successfulSpawns}/{enemiesToSpawn} enemies");
    }

    bool SpawnEnemy(int index, Vector3 playerPosition)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("❌ Enemy prefab not assigned!");
            return false;
        }

        Vector3 spawnPosition = Vector3.zero;
        bool foundValidPosition = false;

        // Try multiple times to find a valid NavMesh position
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            if (mazeGen != null)
            {
                spawnPosition = mazeGen.GetSpawnPointAwayFrom(playerPosition, minDistanceFromPlayer);
            }
            else
            {
                spawnPosition = playerPosition + Random.insideUnitSphere * 15f;
                spawnPosition.y = 0;
            }

            // Check if position is on NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPosition, out hit, 5f, NavMesh.AllAreas))
            {
                spawnPosition = hit.position;
                foundValidPosition = true;
                break;
            }
        }

        if (!foundValidPosition)
        {
            Debug.LogWarning($"⚠️ Enemy {index + 1} couldn't find valid NavMesh position after {maxSpawnAttempts} attempts");
            return false;
        }

        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        enemy.name = $"Enemy_{index + 1}";

        // Verify NavMeshAgent is on NavMesh
        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            if (!agent.isOnNavMesh)
            {
                Debug.LogError($"❌ Enemy {index + 1} NavMeshAgent is NOT on NavMesh after spawn! Destroying.");
                Destroy(enemy);
                return false;
            }
        }

        // Setup enemy references
        var enemyBehavior = enemy.GetComponent<EnemyBehaviorSystem>();
        if (enemyBehavior != null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                enemyBehavior.player = player.transform;
                Debug.Log($"✓ Enemy {index + 1} assigned player reference");
            }
            else
            {
                Debug.LogWarning($"⚠️ No GameObject with tag 'Player' found for Enemy {index + 1}");
            }

            var jumpscareManager = FindFirstObjectByType<JumpScareManager>();
            if (jumpscareManager != null)
            {
                enemyBehavior.jumpScareManager = jumpscareManager;
                Debug.Log($"✓ Enemy {index + 1} assigned JumpScareManager");
            }
            else
            {
                Debug.LogWarning($"⚠️ JumpScareManager not found for Enemy {index + 1}");
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
        Debug.Log($"✓ Enemy {index + 1} spawned at {spawnPosition} (distance from player: {Vector3.Distance(spawnPosition, playerPosition):F1}m)");

        return true;
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

    void OnDisable()
    {
        ClearEnemies();
    }
}