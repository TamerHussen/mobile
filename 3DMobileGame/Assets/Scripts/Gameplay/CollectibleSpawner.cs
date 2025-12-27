using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CollectibleSpawner : MonoBehaviour
{
    [Header("Collectible Settings")]
    public GameObject collectiblePrefab;
    public int baseCollectibleCount = 10;
    public int collectiblesPerExtraPlayer = 5;

    [Header("Visuals")]
    public bool rotateCollectibles = true;
    public float rotationSpeed = 50f;

    private List<GameObject> spawnedCollectibles = new List<GameObject>();
    private MazeGenerator mazeGen;

    void Start()
    {
        mazeGen = FindFirstObjectByType<MazeGenerator>();
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
        Debug.Log($"Spawning {collectiblesToSpawn} collectibles for {playerCount} player(s)");

        for (int i = 0; i < collectiblesToSpawn; i++)
        {
            Vector3 spawnPos = mazeGen != null
                ? mazeGen.GetRandomFloorPosition()
                : Vector3.up * 0.5f;

            SpawnCollectible(spawnPos);
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