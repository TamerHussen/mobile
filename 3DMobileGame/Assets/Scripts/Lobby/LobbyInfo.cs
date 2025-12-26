using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Samples.Friends;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyInfo : MonoBehaviour
{
    public static LobbyInfo Instance;

    // UI
    public TextMeshProUGUI selectedLevelText;
    public TextMeshProUGUI selectedCosmeticText;
    public TextMeshProUGUI playerCountText;

    // Preview
    public Transform previewSpawnPoint;
    public GameObject currentPreviewModel;

    // Lobby Settings
    public int MaxPlayers = 3;

    private string selectedLevel = "None";
    private string selectedCosmetic = "None";

    private ILobbyEvents m_LobbyEvents;
    private Lobby currentLobby;

    private List<LobbyPlayer> players = new List<LobbyPlayer>();

    private bool isHandlingRemoval = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        players.Clear();
    }

    void Start()
    {
        SetLobbyPresence();

        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("SaveManager not found in LobbyInfo.Start()");
            return;
        }

        SaveManager.Instance.Load();

        var data = SaveManager.Instance.data;

        if (!string.IsNullOrEmpty(data.selectedCosmetic))
        {
            selectedCosmetic = data.selectedCosmetic;
            Debug.Log($"LobbyInfo: Loaded cosmetic = {selectedCosmetic}");
            _ = SetSelectedCosmetic(data.selectedCosmetic);
        }
        else
        {
            Debug.LogWarning("No saved cosmetic found, using default");
            selectedCosmetic = "Default";
        }

        if (!string.IsNullOrEmpty(data.lastSelectedLevel))
        {
            selectedLevel = data.lastSelectedLevel;
            Debug.Log($"LobbyInfo: Loaded level = {selectedLevel}");
        }

        UpdateUI();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Lobby")
        {
            Debug.Log("Lobby scene loaded, rebinding...");
            RebindLobbyScene();
        }
    }

    void RebindLobbyScene()
    {
        selectedLevelText = GameObject.Find("SelectedLevelText")
            ?.GetComponent<TextMeshProUGUI>();

        selectedCosmeticText = GameObject.Find("SelectedCosmeticText")
            ?.GetComponent<TextMeshProUGUI>();

        playerCountText = GameObject.Find("PlayerCountText")
            ?.GetComponent<TextMeshProUGUI>();

        var preview = GameObject.Find("PreviewSpawnPoint");
        if (preview != null)
            previewSpawnPoint = preview.transform;

        UpdateUI();
        ForceRespawn();
    }

    async void SetLobbyPresence()
    {
        try
        {
            await FriendsService.Instance.SetPresenceAsync(
                Availability.Online,
                new Activity { Status = "In Lobby" }
            );
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to set lobby presence: {e.Message}");
        }
    }

    void UpdateUI()
    {
        if (selectedLevelText != null)
            selectedLevelText.text = "Level: " + selectedLevel;

        if (selectedCosmeticText != null)
            selectedCosmeticText.text = "Cosmetic: " + selectedCosmetic;

        if (playerCountText != null)
            playerCountText.text = players.Count + "/" + MaxPlayers;
    }

    public async void SetSelectedLevel(string levelName)
    {
        if (UnityLobbyManager.Instance?.CurrentLobby != null)
        {
            string localPlayerId = AuthenticationService.Instance.PlayerId;
            bool isHost = UnityLobbyManager.Instance.CurrentLobby.HostId == localPlayerId;

            if (!isHost)
            {
                Debug.LogWarning("Only the host can change the level");
                return;
            }
        }

        selectedLevel = levelName;
        UpdateUI();

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.data.lastSelectedLevel = levelName;
            SaveManager.Instance.Save();
        }

        if (UnityLobbyManager.Instance != null)
        {
            await UnityLobbyManager.Instance.UpdateLobbyLevel(levelName);
        }
    }

    public void SetSelectedLevelFromLobby(string levelName)
    {
        if (selectedLevel == levelName)
            return;

        Debug.Log($"Syncing level from lobby: {levelName}");

        selectedLevel = levelName;
        UpdateUI();

        UpdateLevelButtonVisuals();
    }

    void UpdateLevelButtonVisuals()
    {
        var levelButtons = FindObjectsByType<LevelSelector>(FindObjectsSortMode.None);
        foreach (var levelButton in levelButtons)
        {
            levelButton.UpdateVisual();
        }
    }

    public void SetSelectedCosmeticSync(string cosmeticName)
    {
        _ = SetSelectedCosmetic(cosmeticName);
    }

    public async Task SetSelectedCosmetic(string CosmeticName)
    {
        Debug.Log($"Setting cosmetic to: {CosmeticName}");

        selectedCosmetic = CosmeticName;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.data.selectedCosmetic = CosmeticName;
            SaveManager.Instance.Save();
        }

        UpdatePreviewModel(CosmeticName);
        UpdateUI();

        if (UnityLobbyManager.Instance != null && UnityLobbyManager.Instance.CurrentLobby != null)
        {
            string displayName = SaveManager.Instance?.data?.playerName ?? "Player";

            Debug.Log($"Updating player data in lobby - Name: {displayName}, Cosmetic: {CosmeticName}");

            await Task.Delay(200);

            await UnityLobbyManager.Instance.UpdatePlayerDataAsync(displayName, CosmeticName);

            // Force immediate refresh after update
            await RefreshLobbyDataAsync();
        }
    }

    public void SetCurrentLobby(Lobby lobby)
    {
        currentLobby = lobby;
    }

    public void SetPlayers(List<LobbyPlayer> newPlayers)
    {
        Debug.Log($"SetPlayers called with {newPlayers.Count} players");

        bool playerCountChanged = players.Count != newPlayers.Count;

        if (playerCountChanged)
        {
            Debug.Log($"Player count changed: {players.Count} -> {newPlayers.Count}");
        }

        // Check if any player data changed (including cosmetics!)
        bool playerDataChanged = false;
        if (!playerCountChanged && players.Count == newPlayers.Count)
        {
            for (int i = 0; i < players.Count; i++)
            {
                var oldPlayer = players[i];
                var newPlayer = newPlayers.FirstOrDefault(p => p.PlayerID == oldPlayer.PlayerID);

                if (newPlayer != null)
                {
                    if (oldPlayer.PlayerName != newPlayer.PlayerName ||
                        oldPlayer.Cosmetic != newPlayer.Cosmetic)
                    {
                        Debug.Log($"Player data changed for {newPlayer.PlayerName}: " +
                                  $"Name: {oldPlayer.PlayerName}->{newPlayer.PlayerName}, " +
                                  $"Cosmetic: {oldPlayer.Cosmetic}->{newPlayer.Cosmetic}");
                        playerDataChanged = true;
                        break;
                    }
                }
            }
        }

        if (!playerCountChanged && !playerDataChanged)
        {
            Debug.Log("No changes detected, skipping update");
            return;
        }

        // Ensure cosmetics are set
        foreach (var p in newPlayers)
        {
            if (string.IsNullOrEmpty(p.Cosmetic))
            {
                Debug.LogWarning($"Player {p.PlayerName} has no cosmetic, setting to Default");
                p.Cosmetic = "Default";
            }
        }

        // Update player list
        players = newPlayers;

        // Mark local player
        foreach (var p in players)
        {
            p.IsLocal = p.PlayerID == AuthenticationService.Instance.PlayerId;
        }

        // Skip if players are still joining
        if (newPlayers.Any(p => p.PlayerName == "Joining..."))
        {
            Debug.Log("Some players still joining, skipping spawn");
            return;
        }

        // Update UI
        UpdateUI();

        if (LobbyPlayerSpawner.Instance != null)
        {
            Debug.Log("Cosmetic update detected, respawning players...");
            LobbyPlayerSpawner.Instance.ClearAll();

            StopAllCoroutines();
            StartCoroutine(DelayedSpawn());
        }
    }

    IEnumerator DelayedSpawn()
    {
        yield return null;
        ForceRespawn();
    }

    void UpdatePreviewModel(string Cosmetic)
    {
        if (currentPreviewModel == null) return;

        currentPreviewModel
            .GetComponent<PlayerCosmetic>()
            .Apply(Cosmetic);
    }

    public string GetSelectedLevel() => selectedLevel;
    public string GetSelectedCosmetic() => selectedCosmetic;

    public bool IsLobbyFull()
    {
        return players.Count >= MaxPlayers;
    }

    public void ForceRespawn()
    {
        if (players == null || players.Count == 0) return;

        if (LobbyPlayerSpawner.Instance == null) return;

        LobbyPlayerSpawner.Instance.SpawnPlayers();
    }

    public void UpdateHostName(string NewName)
    {
        if (players.Count == 0) return;

        var localPlayer = GetLocalPlayer();
        if (localPlayer == null) return;

        localPlayer.PlayerName = NewName;
        ForceRespawn();
        UpdateUI();
    }

    public async void KickPlayer(string targetPlayerId)
    {
        if (UnityLobbyManager.Instance?.CurrentLobby == null)
        {
            Debug.LogError("Cannot kick: No active lobby");
            return;
        }

        string localPlayerId = AuthenticationService.Instance.PlayerId;
        if (UnityLobbyManager.Instance.CurrentLobby.HostId != localPlayerId)
        {
            Debug.LogError("Cannot kick: Not the host");
            return;
        }

        try
        {
            string lobbyId = UnityLobbyManager.Instance.CurrentLobby.Id;

            Debug.Log($"Host kicking player: {targetPlayerId} from lobby: {lobbyId}");

            await LobbyService.Instance.RemovePlayerAsync(lobbyId, targetPlayerId);

            Debug.Log($"✅ Successfully kicked player: {targetPlayerId}");

            await Task.Delay(1000);

            ForceRespawn();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to kick player: {e.Message}");
        }
    }

    public void UpdatePlayerName(string playerId, string newName)
    {
        foreach (var p in players)
        {
            if (p.PlayerID == playerId)
            {
                p.PlayerName = newName;
                break;
            }
        }
        ForceRespawn();
    }

    public async void SubscribeToLobby(string lobbyId)
    {
        if (string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogError("Cannot subscribe to lobby: Lobby ID is null or empty!");
            return;
        }

        if (m_LobbyEvents != null)
        {
            Debug.LogWarning("Already subscribed to lobby events, unsubscribing first...");
            UnsubscribeFromLobby();
            await Task.Delay(300);
        }

        if (UnityLobbyManager.Instance?.CurrentLobby == null)
        {
            Debug.LogError("Cannot subscribe: CurrentLobby is null!");
            return;
        }

        if (UnityLobbyManager.Instance.CurrentLobby.Id != lobbyId)
        {
            Debug.LogWarning($"Lobby ID mismatch! Requested: {lobbyId}, Current: {UnityLobbyManager.Instance.CurrentLobby.Id}");
            lobbyId = UnityLobbyManager.Instance.CurrentLobby.Id;
        }

        currentLobby = UnityLobbyManager.Instance.CurrentLobby;
        isHandlingRemoval = false;

        var callbacks = new LobbyEventCallbacks();
        callbacks.LobbyChanged += OnLobbyChanged;

        callbacks.PlayerJoined += (joinedPlayers) =>
        {
            Debug.Log($"Player joined event - Count: {joinedPlayers.Count}");
        };

        callbacks.PlayerLeft += (leftPlayers) =>
        {
            Debug.Log($"Player left event - Count: {leftPlayers.Count}");
        };

        callbacks.KickedFromLobby += () =>
        {
            Debug.Log("Kicked from lobby callback triggered!");
            HandleLocalPlayerRemoved();
        };

        callbacks.LobbyDeleted += () =>
        {
            Debug.Log("Lobby deleted callback triggered!");
            HandleLocalPlayerRemoved();
        };

        try
        {
            Debug.Log($"Subscribing to lobby events for lobby: {lobbyId}");
            m_LobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyId, callbacks);

            UnityLobbyManager.Instance?.EnableLobbyEvents();

            Debug.Log("✅ Successfully subscribed to lobby events!");
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"Failed to subscribe to lobby events: {ex.Message} (Code: {ex.Reason})");
            m_LobbyEvents = null;

            UnityLobbyManager.Instance?.DisableLobbyEvents();
        }
    }

    private async void HandleLocalPlayerRemoved()
    {
        if (isHandlingRemoval)
        {
            Debug.Log("Already handling removal, skipping");
            return;
        }

        isHandlingRemoval = true;

        Debug.Log("=== HANDLING LOCAL PLAYER REMOVAL ===");

        UnsubscribeFromLobby();

        if (LobbyPlayerSpawner.Instance != null)
        {
            LobbyPlayerSpawner.Instance.ClearAll();
        }

        players.Clear();
        UpdateUI();

        currentLobby = null;

        if (UnityLobbyManager.Instance != null)
        {
            UnityLobbyManager.Instance.CurrentLobby = null;
        }

        Debug.Log("Local state cleared, creating new lobby...");

        await Task.Delay(1000);

        Debug.Log("Creating new personal lobby after being removed...");

        if (UnityLobbyManager.Instance != null)
        {
            await UnityLobbyManager.Instance.EnsurePersonalLobby();

            if (SceneManager.GetActiveScene().name == "Lobby")
            {
                await Task.Delay(500);

                ForceRespawn();
            }
        }

        isHandlingRemoval = false;

        Debug.Log("✅ Rejoined personal lobby after being kicked");
    }

    public async Task RefreshLobbyDataAsync()
    {
        if (isHandlingRemoval)
        {
            Debug.Log("Skipping refresh - handling removal");
            return;
        }

        if (currentLobby == null && UnityLobbyManager.Instance?.CurrentLobby != null)
        {
            currentLobby = UnityLobbyManager.Instance.CurrentLobby;
        }

        if (currentLobby == null)
        {
            Debug.LogWarning("Cannot refresh: currentLobby is null");
            return;
        }

        try
        {
            currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);

            string localPlayerId = AuthenticationService.Instance.PlayerId;
            bool stillInLobby = currentLobby.Players.Any(p => p.Id == localPlayerId);

            if (!stillInLobby)
            {
                Debug.LogWarning("Local player not found in lobby during refresh - handling removal");
                HandleLocalPlayerRemoved();
                return;
            }

            List<LobbyPlayer> lobbyPlayers = new List<LobbyPlayer>();
            foreach (var p in currentLobby.Players)
            {
                string pName = (p.Data != null && p.Data.ContainsKey("Name")) ? p.Data["Name"].Value : "Unknown";
                string pCosmetic = (p.Data != null && p.Data.ContainsKey("Cosmetic")) ? p.Data["Cosmetic"].Value : "Default";
                bool isLocal = p.Id == localPlayerId;

                Debug.Log($"Refresh found player: {pName} with cosmetic: {pCosmetic}");

                lobbyPlayers.Add(new LobbyPlayer(p.Id, pName, pCosmetic, isLocal));
            }

            SetPlayers(lobbyPlayers);
        }
        catch (LobbyServiceException e)
        {
            if (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                Debug.LogWarning("Lobby not found during refresh - handling removal");
                HandleLocalPlayerRemoved();
            }
            else if (e.Reason == LobbyExceptionReason.RateLimited)
            {
                // Silently skip this refresh
            }
            else
            {
                Debug.LogError($"Failed to refresh lobby: {e.Message}");
            }
        }
    }

    public async void RefreshLobbyData()
    {
        await RefreshLobbyDataAsync();
    }

    private void OnLobbyChanged(ILobbyChanges changes)
    {
        _ = HandleLobbyChangeAsync(changes);
    }

    private async Task HandleLobbyChangeAsync(ILobbyChanges changes)
    {
        if (isHandlingRemoval)
        {
            Debug.Log("Skipping lobby change - handling removal");
            return;
        }

        if (currentLobby == null) return;

        string localPlayerId = AuthenticationService.Instance.PlayerId;

        changes.ApplyToLobby(currentLobby);

        bool stillInLobby = currentLobby.Players.Any(p => p.Id == localPlayerId);

        if (!stillInLobby)
        {
            Debug.Log("Local player no longer in lobby after changes - was kicked!");
            HandleLocalPlayerRemoved();
            return;
        }

        if (changes.Data.Changed && currentLobby.Data != null)
        {
            if (currentLobby.Data.ContainsKey("SelectedLevel"))
            {
                string newLevel = currentLobby.Data["SelectedLevel"].Value;
                if (selectedLevel != newLevel)
                {
                    Debug.Log($"Level changed from '{selectedLevel}' to '{newLevel}'");
                    SetSelectedLevelFromLobby(newLevel);
                }
            }
        }

        // Always refresh after lobby changes to catch cosmetic updates
        await RefreshLobbyDataAsync();
    }

    public LobbyPlayer GetLocalPlayer()
    {
        return players.Find(p => p.PlayerID == AuthenticationService.Instance.PlayerId);
    }

    public void UnsubscribeFromLobby()
    {
        if (m_LobbyEvents != null)
        {
            try
            {
                _ = m_LobbyEvents.UnsubscribeAsync();
                Debug.Log("✅ Unsubscribed from lobby events");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error unsubscribing from lobby: {e.Message}");
            }

            m_LobbyEvents = null;

            UnityLobbyManager.Instance?.DisableLobbyEvents();
        }
    }

    public void ClearLocalLobby()
    {
        Debug.Log("ClearLocalLobby called");

        UnsubscribeFromLobby();

        if (LobbyPlayerSpawner.Instance != null)
        {
            LobbyPlayerSpawner.Instance.ClearAll();
        }

        players.Clear();
        UpdateUI();

        currentLobby = null;

        isHandlingRemoval = false;

        Debug.Log("✅ Local lobby cleared");
    }

    public List<LobbyPlayer> GetPlayers() => players;
}