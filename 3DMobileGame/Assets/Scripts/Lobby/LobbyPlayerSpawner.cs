using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

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

    private void Start()
    {
        LobbyInfo.Instance?.ForceRespawn();
    }

    public void SpawnPlayers()
    {
        foreach (var p in SpawnedPlayers) Destroy(p);

        SpawnedPlayers.Clear();

        var players = LobbyInfo.Instance.GetPlayers();

        int count = Mathf.Min(players.Count, SpawnPoints.Length);

        for (int i = 0; i < count; i++)
        {
            GameObject player = Instantiate(
                playerPrefab,
                SpawnPoints[i].position,
                SpawnPoints[i].rotation
                );

            // add cosmetic
            player.GetComponent<PlayerCosmetic>().Apply(players[i].Cosmetic);

            // add nametag
            player.GetComponentInChildren<PlayerNames>().SetName(players[i].PlayerID);

            SpawnedPlayers.Add(player);
        }
    }
}
