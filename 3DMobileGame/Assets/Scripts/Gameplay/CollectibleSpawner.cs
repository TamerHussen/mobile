using System.Collections.Generic;
using UnityEngine;

public class CollectibleSpawner : MonoBehaviour
{
    [Header("Collectible Settings")]
    public GameObject collectiblePrefab;
    public int baseCollectibleCount = 10;
    public int collectiblesPerExtraPlayer = 5;

    [Header("Spawn Rules")]
    [Tooltip("Minimum distance from player spawn")]
    public float minDistanceFromPlayer = 5f;

    [Header("Visuals")]
    public bool rotateCollectibles = true;
    public float rotationSpeed = 50f;

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
        if (Application.isPlaying && mazeGen != null)
        {
            SpawnCollectibles();
        }
    }

    void SpawnCollectibles()
    {
        int playerCount = 1;
        if (GameSessionData.Instance?.players != null)
        {
            playerCount = GameSessionData.Instance.players.Count;
        }

        int collectiblesToSpawn = baseCollectibleCount + (playerCount > 1 ? (playerCount - 1) * collectiblesPerExtraPlayer : 0);
        Debug.Log($" Spawning {collectiblesToSpawn} collectibles for {playerCount} player(s)");

        Vector3 playerPosition = Vector3.zero;
        if (playerSpawner != null)
        {
            playerPosition = playerSpawner.GetPlayerSpawnPosition();
        }

        HashSet<Vector3> usedPositions = new HashSet<Vector3>();
        int attempts = 0;
        int maxAttempts = collectiblesToSpawn * 10;

        for (int i = 0; i < collectiblesToSpawn && attempts < maxAttempts; attempts++)
        {
            Vector3 spawnPos;
            if (mazeGen != null)
            {
                spawnPos = mazeGen.GetSpawnPointAwayFrom(playerPosition, minDistanceFromPlayer);
            }
            else
            {
                spawnPos = Vector3.up * 0.5f;
            }

            Vector3 gridPos = new Vector3(
                Mathf.Round(spawnPos.x * 2f) / 2f,
                Mathf.Round(spawnPos.y * 2f) / 2f,
                Mathf.Round(spawnPos.z * 2f) / 2f
            );

            if (!usedPositions.Contains(gridPos))
            {
                usedPositions.Add(gridPos);
                SpawnCollectible(spawnPos);
                i++;
            }
        }

        Debug.Log($"✅ Spawned {spawnedCollectibles.Count} collectibles");
    }

    void SpawnCollectible(Vector3 position)
    {
        if (collectiblePrefab == null)
        {
            Debug.LogError("Collectible prefab not assigned!");
            return;
        }

        GameObject collectible = Instantiate(collectiblePrefab, position, Quaternion.identity);
        collectible.tag = "Collectible";

        if (rotateCollectibles)
        {
            var rotator = collectible.GetComponent<RotatePreview>();
            if (rotator == null)
            {
                rotator = collectible.AddComponent<RotatePreview>();
            }
            rotator.speed = rotationSpeed;
        }

        spawnedCollectibles.Add(collectible);
    }
}