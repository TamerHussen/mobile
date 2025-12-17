using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class LobbyPlayerSpawner : MonoBehaviour
{
    public Transform[] SpawnPoints;
    public GameObject playerPrefab;

    private List<GameObject> SpawnedPlayers = new();

    void Start()
    {
        SpawnPlayers();
    }

    public void SpawnPlayers()
    {
        foreach (var p in SpawnedPlayers) Destroy(p);

        SpawnedPlayers.Clear();

        var players = LobbyInfo.Instance.GetPlayers();

        for (int i = 0; i < players.Count; i++)
        {
            GameObject player = Instantiate(
                playerPrefab,
                SpawnPoints[i].position,
                SpawnPoints[i].rotation
                );

            // add cosmetic
            player.GetComponent<PlayerCosmetic>().Apply(players[i].Cosmetic);

            SpawnedPlayers.Add(player);
        }
    }
}
