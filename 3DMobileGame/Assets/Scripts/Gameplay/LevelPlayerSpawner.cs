using UnityEngine;
using Unity.Services.Authentication;

/// <summary>
/// Spawns player and auto-assigns all references
/// Attach to: LevelManager/LevelPlayerSpawner GameObject
/// </summary>
public class LevelPlayerSpawner : MonoBehaviour
{
    [Header("Player Setup")]
    public GameObject playerPrefab;
    public Transform playerSpawnPoint;

    [Header("Respawn Points")]
    public Transform[] respawnPoints;

    private GameObject spawnedPlayer;

    void Start()
    {
        SpawnPlayer();
    }

    void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab not assigned!");
            return;
        }

        // Get spawn position
        Vector3 spawnPosition = playerSpawnPoint != null
            ? playerSpawnPoint.position
            : Vector3.zero;

        Quaternion spawnRotation = playerSpawnPoint != null
            ? playerSpawnPoint.rotation
            : Quaternion.identity;

        // Spawn player
        spawnedPlayer = Instantiate(playerPrefab, spawnPosition, spawnRotation);
        spawnedPlayer.name = "Player";

        // ✅ IMPORTANT: Tag the player so EnemySpawner can find it!
        spawnedPlayer.tag = "Player";

        Debug.Log($"Player spawned at {spawnPosition}");

        // Get local player data from GameSessionData
        LobbyPlayer localPlayerData = GetLocalPlayerData();

        if (localPlayerData != null)
        {
            // Apply cosmetic and name
            var playerView = spawnedPlayer.GetComponent<PlayerView>();
            if (playerView != null)
            {
                playerView.Bind(localPlayerData);
                Debug.Log($"✅ Applied cosmetic '{localPlayerData.Cosmetic}' to player '{localPlayerData.PlayerName}'");
            }
            else
            {
                Debug.LogWarning("PlayerView component not found on player prefab!");
            }
        }
        else
        {
            Debug.LogWarning("No player data found - using defaults");
            ApplyDefaultCosmetic();
        }

        // ✅ Setup lives system (just assign respawn points)
        SetupLivesSystem();

        // ✅ Setup score UI references
        SetupScoreUI();

        Debug.Log("✅ Player setup complete - UI will auto-assign in PlayerLivesSystem.Start()");
    }

    LobbyPlayer GetLocalPlayerData()
    {
        // Try to get from GameSessionData first
        if (GameSessionData.Instance != null && GameSessionData.Instance.players != null)
        {
            string localPlayerId = AuthenticationService.Instance.PlayerId;

            // Find local player in session data
            foreach (var player in GameSessionData.Instance.players)
            {
                if (player.PlayerID == localPlayerId || player.IsLocal)
                {
                    Debug.Log($"Found local player data: {player.PlayerName}, Cosmetic: {player.Cosmetic}");
                    return player;
                }
            }

            // Fallback: use first player if solo
            if (GameSessionData.Instance.players.Count == 1)
            {
                Debug.Log("Solo mode - using first player data");
                return GameSessionData.Instance.players[0];
            }
        }

        // Fallback: create from SaveManager
        if (SaveManager.Instance != null && SaveManager.Instance.data != null)
        {
            Debug.Log("Loading player data from SaveManager");
            return new LobbyPlayer(
                AuthenticationService.Instance.PlayerId,
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
            playerCosmetic.Apply("Default");
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
            // Only assign respawn points - UI is auto-found in PlayerLivesSystem.Start()
            livesSystem.respawnPoints = respawnPoints;
            Debug.Log($"✅ Assigned {respawnPoints.Length} respawn points to PlayerLivesSystem");
        }
    }

    void SetupScoreUI()
    {
        var playerScore = spawnedPlayer.GetComponent<PlayerScore>();
        if (playerScore != null)
        {
            // Auto-find UI elements by name
            playerScore.ScoreText = GameObject.Find("ScoreText")?.GetComponent<TMPro.TextMeshProUGUI>();
            playerScore.CollectibleText = GameObject.Find("CollectibleText")?.GetComponent<TMPro.TextMeshProUGUI>();
            playerScore.CoinsEarnedText = GameObject.Find("CoinsEarnedText")?.GetComponent<TMPro.TextMeshProUGUI>();

            Debug.Log("✅ Auto-assigned Score UI references");
        }
    }

    public GameObject GetSpawnedPlayer()
    {
        return spawnedPlayer;
    }

    void OnDrawGizmos()
    {
        // Visualize spawn point
        if (playerSpawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerSpawnPoint.position, 1f);
            Gizmos.DrawRay(playerSpawnPoint.position, playerSpawnPoint.forward * 2f);
        }

        // Visualize respawn points
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