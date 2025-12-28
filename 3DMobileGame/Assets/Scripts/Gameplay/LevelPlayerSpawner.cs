using UnityEngine;
using Unity.Services.Authentication;

public class LevelPlayerSpawner : MonoBehaviour
{
    [Header("Player Setup")]
    public GameObject playerPrefab;
    public Transform playerSpawnPoint;

    [Header("Respawn Points")]
    public Transform[] respawnPoints;

    private GameObject spawnedPlayer;
    private MazeGenerator mazeGen;

    void Awake()
    {
        mazeGen = FindFirstObjectByType<MazeGenerator>();
    }

    void OnEnable()
    {
        if (Application.isPlaying)
        {
            SpawnPlayer();
        }
    }

    void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab not assigned!");
            return;
        }

        Vector3 spawnPosition;
        Quaternion spawnRotation;

        if (playerSpawnPoint != null)
        {
            spawnPosition = playerSpawnPoint.position;
            spawnRotation = playerSpawnPoint.rotation;
        }
        else if (mazeGen != null)
        {
            spawnPosition = mazeGen.GetRandomFloorPosition();
            spawnRotation = Quaternion.identity;
            Debug.Log(" No spawn point assigned, using maze position");
        }
        else
        {
            spawnPosition = Vector3.zero;
            spawnRotation = Quaternion.identity;
            Debug.LogWarning(" No MazeGenerator found, spawning at origin");
        }

        spawnedPlayer = Instantiate(playerPrefab, spawnPosition, spawnRotation);
        spawnedPlayer.name = "Player";
        spawnedPlayer.tag = "Player";

        Debug.Log($" Player spawned at {spawnPosition}");

        LobbyPlayer localPlayerData = GetLocalPlayerData();

        if (localPlayerData != null)
        {
            var playerView = spawnedPlayer.GetComponent<PlayerView>();
            if (playerView != null)
            {
                playerView.Bind(localPlayerData);
                Debug.Log($" Applied cosmetic '{localPlayerData.Cosmetic}' to player '{localPlayerData.PlayerName}'");
            }
        }
        else
        {
            Debug.LogWarning("No player data found - using defaults");
            ApplyDefaultCosmetic();
        }

        SetupLivesSystem();
        SetupScoreUI();

        Debug.Log(" Player setup complete");
    }

    LobbyPlayer GetLocalPlayerData()
    {
        if (GameSessionData.Instance != null && GameSessionData.Instance.players != null)
        {
            string localPlayerId = AuthenticationService.Instance?.PlayerId;

            if (!string.IsNullOrEmpty(localPlayerId))
            {
                foreach (var player in GameSessionData.Instance.players)
                {
                    if (player.PlayerID == localPlayerId || player.IsLocal)
                    {
                        Debug.Log($"Found local player data: {player.PlayerName}, Cosmetic: {player.Cosmetic}");
                        return player;
                    }
                }
            }

            if (GameSessionData.Instance.players.Count == 1)
            {
                Debug.Log("Solo mode - using first player data");
                return GameSessionData.Instance.players[0];
            }
        }

        if (SaveManager.Instance != null && SaveManager.Instance.data != null)
        {
            Debug.Log("Loading player data from SaveManager");
            string playerId = AuthenticationService.Instance?.PlayerId ?? "local_player";
            return new LobbyPlayer(
                playerId,
                SaveManager.Instance.data.playerName,
                SaveManager.Instance.data.selectedCosmetic,
                true
            );
        }

        return null;
    }

    void ApplyDefaultCosmetic()
    {
        var playerCosmetic = spawnedPlayer.GetComponent<PlayerCosmetic>();
        if (playerCosmetic != null)
        {
            playerCosmetic.Apply("DefaultCosmetic");
        }

        var playerNames = spawnedPlayer.GetComponentInChildren<PlayerNames>();
        if (playerNames != null)
        {
            playerNames.SetName("Player");
        }
    }

    void SetupLivesSystem()
    {
        var livesSystem = spawnedPlayer.GetComponent<PlayerLivesSystem>();
        if (livesSystem != null && respawnPoints != null && respawnPoints.Length > 0)
        {
            livesSystem.respawnPoints = respawnPoints;
            Debug.Log($" Assigned {respawnPoints.Length} respawn points");
        }
    }

    void SetupScoreUI()
    {
        var playerScore = spawnedPlayer.GetComponent<PlayerScore>();
        if (playerScore != null)
        {
            playerScore.ScoreText = GameObject.Find("ScoreText")?.GetComponent<TMPro.TextMeshProUGUI>();
            playerScore.CollectibleText = GameObject.Find("CollectibleText")?.GetComponent<TMPro.TextMeshProUGUI>();
            playerScore.CoinsEarnedText = GameObject.Find("CoinsEarnedText")?.GetComponent<TMPro.TextMeshProUGUI>();

            Debug.Log(" Auto-assigned Score UI references");
        }
    }

    public GameObject GetSpawnedPlayer()
    {
        return spawnedPlayer;
    }

    public Vector3 GetPlayerSpawnPosition()
    {
        return spawnedPlayer != null ? spawnedPlayer.transform.position : Vector3.zero;
    }

    void OnDrawGizmos()
    {
        if (playerSpawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerSpawnPoint.position, 1f);
            Gizmos.DrawRay(playerSpawnPoint.position, playerSpawnPoint.forward * 2f);
        }

        if (respawnPoints != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var point in respawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.5f);
                }
            }
        }
    }
}