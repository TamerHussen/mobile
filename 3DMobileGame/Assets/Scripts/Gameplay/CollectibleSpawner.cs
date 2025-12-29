using System.Collections.Generic;
using UnityEngine;

public class CollectibleSpawner : MonoBehaviour
{
    [Header("Collectible Settings")]
    public GameObject collectiblePrefab;
    public int baseCollectibleCount = 10;
    public int collectiblesPerExtraPlayer = 5;

    [Header("Spawn Rules")]
    public float minDistanceFromPlayer = 5f;

    private List<GameObject> spawnedCollectibles = new List<GameObject>();
    private MazeGenerator mazeGen;
    private LevelPlayerSpawner playerSpawner;

    void Awake()
    {
        mazeGen = FindFirstObjectByType<MazeGenerator>();
        playerSpawner = FindFirstObjectByType<LevelPlayerSpawner>();
    }

    void OnEnable()
    {
        MazeGenerator.MazeReady += SpawnCollectibles;
    }

    void OnDisable()
    {
        MazeGenerator.MazeReady -= SpawnCollectibles;
    }

    void SpawnCollectibles()
    {
        if (mazeGen == null || collectiblePrefab == null)
        {
            Debug.LogError("CollectibleSpawner missing references");
            return;
        }

        List<Vector3> spawnPoints = mazeGen.GetAllFloorPositions();
        if (spawnPoints.Count == 0)
        {
            Debug.LogError("No floor spawn points available!");
            return;
        }

        int playerCount = GameSessionData.Instance?.players?.Count ?? 1;
        int count = baseCollectibleCount + (playerCount - 1) * collectiblesPerExtraPlayer;

        Vector3 playerPos = playerSpawner?.GetPlayerSpawnPosition() ?? Vector3.zero;

        spawnPoints.RemoveAll(p => Vector3.Distance(p, playerPos) < minDistanceFromPlayer);

        count = Mathf.Min(count, spawnPoints.Count);

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, spawnPoints.Count);
            Instantiate(collectiblePrefab, spawnPoints[index], Quaternion.identity);
            spawnPoints.RemoveAt(index);
        }

        Debug.Log($"✓ Spawned {count} collectibles");
    }
}