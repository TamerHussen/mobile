using System.Collections.Generic;
using Unity.Services.Authentication;
using UnityEngine;

public class LobbyPlayerSpawner : MonoBehaviour
{
    public static LobbyPlayerSpawner Instance;
    public Transform[] SpawnPoints;
    public GameObject playerPrefab;

    private List<GameObject> SpawnedPlayers = new List<GameObject>();

    void Awake()
    {
        Instance = this;
    }

    public void SpawnPlayers()
    {
        Debug.Log($"=== SPAWNING PLAYERS ===");

        ClearAll();

        if (SpawnPoints == null || SpawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned!");
            return;
        }

        var players = LobbyInfo.Instance?.GetPlayers();

        if (players == null || players.Count == 0)
        {
            Debug.LogWarning("No players to spawn");
            return;
        }

        Debug.Log($"Spawning {players.Count} players");

        for (int i = 0; i < players.Count && i < SpawnPoints.Length; i++)
        {
            var lobbyPlayer = players[i];
            var spawn = SpawnPoints[i];

            if (spawn == null)
            {
                Debug.LogWarning($"Spawn point {i} is null!");
                continue;
            }

            Debug.Log($"Spawning player {i}: {lobbyPlayer.PlayerName} (ID: {lobbyPlayer.PlayerID})");

            var playerObj = Instantiate(playerPrefab, spawn.position, spawn.rotation);
            playerObj.name = $"Player_{lobbyPlayer.PlayerName}";

            var avatar = playerObj.GetComponent<PlayerAvatar>();
            if (avatar != null)
            {
                avatar.PlayerID = lobbyPlayer.PlayerID;
            }

            var view = playerObj.GetComponent<PlayerView>();
            if (view != null)
            {
                view.Bind(lobbyPlayer);
            }
            else
            {
                Debug.LogWarning($"PlayerView component not found on {playerObj.name}");
            }

            SpawnedPlayers.Add(playerObj);

            Debug.Log($"✅ Spawned: {lobbyPlayer.PlayerName} with cosmetic: {lobbyPlayer.Cosmetic}");
        }

        Debug.Log($"=== SPAWN COMPLETE: {SpawnedPlayers.Count} players ===");
    }

    public void ClearAll()
    {
        Debug.Log($"Clearing {SpawnedPlayers.Count} spawned players");

        // Destroy all spawned players
        foreach (var player in SpawnedPlayers)
        {
            if (player != null)
            {
                Destroy(player);
            }
        }

        SpawnedPlayers.Clear();

        Debug.Log("✅ All players cleared");
    }
}