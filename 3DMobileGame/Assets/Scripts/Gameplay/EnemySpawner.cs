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

    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private MazeGenerator mazeGen;

    void Start()
    {
        mazeGen = FindFirstObjectByType<MazeGenerator>();
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

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy(i);
        }
    }

    void SpawnEnemy(int index)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Enemy prefab not assigned!");
            return;
        }

        Vector3 spawnPosition = mazeGen != null ? mazeGen.GetRandomFloorPosition() : Vector3.zero;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(spawnPosition, out hit, 5f, NavMesh.AllAreas))
        {
            spawnPosition = hit.position;
        }

        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        enemy.name = $"Enemy_{index + 1}";

        // ✅ AUTO-ASSIGN REFERENCES
        var enemyBehavior = enemy.GetComponent<EnemyBehaviorSystem>();
        if (enemyBehavior != null)
        {
            // Find player by TAG
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                enemyBehavior.player = player.transform;
                Debug.Log("✅ Auto-assigned Player via tag");
            }
            else
            {
                Debug.LogWarning("⚠️ No GameObject with tag 'Player' found!");
            }

            // Find JumpScareManager by TYPE
            var jumpscareManager = FindFirstObjectByType<JumpScareManager>();
            if (jumpscareManager != null)
            {
                enemyBehavior.jumpScareManager = jumpscareManager;
                Debug.Log("✅ Auto-assigned JumpScareManager");
            }
            else
            {
                Debug.LogWarning("⚠️ JumpScareManager not found in scene!");
            }

            enemyBehavior.centrePoint = transform;
        }

        // ✅ AUTO-ASSIGN PROXIMITY HAPTICS
        var proximityHaptics = enemy.GetComponent<EnemyProximityHaptics>();
        if (proximityHaptics != null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                proximityHaptics.player = player.transform;
                Debug.Log("✅ Auto-assigned Player to EnemyProximityHaptics");
            }

            if (enemyBehavior != null)
            {
                proximityHaptics.enemy = enemyBehavior;
                Debug.Log("✅ Auto-assigned EnemyBehaviorSystem to Haptics");
            }
        }

        spawnedEnemies.Add(enemy);
        Debug.Log($"✅ Enemy {index + 1} fully configured at {spawnPosition}");
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