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
        if (SpawnPoints == null || SpawnPoints.Length == 0) return;

        foreach (var p in SpawnedPlayers) Destroy(p);

        SpawnedPlayers.Clear();

        var players = LobbyInfo.Instance.GetPlayers();


        for (int i = 0; i < players.Count && i < SpawnPoints.Length; i++)
        {
            var lobbyPlayer = players[i];
            var spawn = SpawnPoints[i];
            if (spawn == null) continue;


            var GOplayer = Instantiate(playerPrefab, spawn.position, spawn.rotation);

            var avatar = GOplayer.GetComponent<PlayerAvatar>();
            if (avatar != null) avatar.PlayerID = lobbyPlayer.PlayerID;
            
            var view = GOplayer.GetComponent<PlayerView>();
            if(view != null) view.Bind(lobbyPlayer);

            SpawnedPlayers.Add(GOplayer);
        }
    }

    public void ClearAll()
    {
        foreach(var p in SpawnedPlayers) Destroy(p); 
        SpawnedPlayers.Clear();
    }
}
