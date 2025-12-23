using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyPlayerSpawner : MonoBehaviour
{
    public static LobbyPlayerSpawner Instance;
    public Transform[] SpawnPoints;
    public GameObject playerPrefab;

    private List<GameObject> SpawnedPlayers = new();

    void Awake()
    {
        Instance = this;
    }

    public void SpawnPlayers()
    {
        foreach (var p in SpawnedPlayers) Destroy(p);

        SpawnedPlayers.Clear();

        var players = LobbyInfo.Instance.GetPlayers();

        int count = Mathf.Min(players.Count, SpawnPoints.Length);

        for (int i = 0; i < count; i++)
        {
            GameObject player = Instantiate(playerPrefab, SpawnPoints[i].position, SpawnPoints[i].rotation);

            player.GetComponent<PlayerCosmetic>().Apply(players[i].Cosmetic);

            PlayerNames nameTag = player.GetComponentInChildren<PlayerNames>();
            if (nameTag != null)
            {
                nameTag.SetName(players[i].PlayerName);
            }

            SpawnedPlayers.Add(player);
        }


    }
}
