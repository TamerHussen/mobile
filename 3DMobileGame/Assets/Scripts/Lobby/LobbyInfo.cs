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
    public Transform previewSpawnPoint;  // for cosmetics
    public GameObject currentPreviewModel;

    // Lobby Settings
    public int MaxPlayers = 3;

    private string selectedLevel = "None";
    private string selectedCosmetic = "None";

    private ILobbyEvents m_LobbyEvents;
    private Lobby currentLobby;

    private List<LobbyPlayer> players = new List<LobbyPlayer>();

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

        // CRITICAL FIX: Wait for SaveManager to be ready
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

            var relationshipsManager = FindFirstObjectByType<RelationshipsManager>();
            if (relationshipsManager != null)
            {
                relationshipsManager.RefreshAll();
                relationshipsManager.RefreshLocalPlayerName();
            }
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
        await FriendsService.Instance.SetPresenceAsync(
            Availability.Online,
            new Activity { Status = "In Lobby" }
        );
    }

    void UpdateUI()
    {
        selectedLevelText.text = "Level: " + selectedLevel;
        selectedCosmeticText.text = "Cosmetic: " + selectedCosmetic;
        playerCountText.text = players.Count + "/" + MaxPlayers;
    }

    public async void SetSelectedLevel(string levelName)
    {
        selectedLevel = levelName;
        UpdateUI();

        // Save the selected level
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.data.lastSelectedLevel = levelName;
            SaveManager.Instance.Save();
        }

        if (UnityLobbyManager.Instance != null && UnityLobbyManager.Instance.CurrentLobby != null)
        {
            await UnityLobbyManager.Instance.UpdatePlayerDataAsync(
                AuthenticationService.Instance.PlayerName,
                selectedCosmetic
            );
        }
    }

    public async Task SetSelectedCosmetic(string CosmeticName)
    {
        selectedCosmetic = CosmeticName;

        // Save the selected cosmetic
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.data.selectedCosmetic = CosmeticName;
            SaveManager.Instance.Save();
        }

        UpdatePreviewModel(CosmeticName);
        UpdateUI();

        if (UnityLobbyManager.Instance != null && UnityLobbyManager.Instance.CurrentLobby != null)
        {
            await UnityLobbyManager.Instance.UpdatePlayerDataAsync(
                SaveManager.Instance.data.playerName,
                CosmeticName
            );
        }
    }

    public void SetCurrentLobby(Lobby lobby)
    {
        currentLobby = lobby;
    }

    public void SetPlayers(List<LobbyPlayer> newPlayers)
    {
        if (players.Count == newPlayers.Count &&
            players.SequenceEqual(newPlayers, new LobbyPlayerComparer()))
            return;

        foreach (var p in newPlayers)
        {
            if (string.IsNullOrEmpty(p.Cosmetic)) p.Cosmetic = "Default";
        }

        players = newPlayers;

        foreach (var p in players)
        {
            p.IsLocal = p.PlayerID == AuthenticationService.Instance.PlayerId;
        }

        if (newPlayers.Any(p => p.PlayerName == "Joining...")) return;

        UpdateUI();
        StopAllCoroutines();
        StartCoroutine(DelayedSpawn());
    }

    IEnumerator DelayedSpawn()
    {
        yield return null;
        ForceRespawn();
    }

    bool HasHost()
    {
        return players != null && players.Count > 0;
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
        try
        {
            // Get the current lobby ID (assuming it is stored in UnityLobbyManager)
            string lobbyId = UnityLobbyManager.Instance.CurrentLobby.Id;

            // The host uses this API to remove someone else
            await LobbyService.Instance.RemovePlayerAsync(lobbyId, targetPlayerId);

            Debug.Log($"Successfully kicked player: {targetPlayerId}");

            // Refresh local UI
            ForceRespawn();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to kick player: {e.Message}");
        }
    }
    class LobbyPlayerComparer : IEqualityComparer<LobbyPlayer>
    {
        public bool Equals(LobbyPlayer a, LobbyPlayer b)
        {
            return a.PlayerID == b.PlayerID && a.Cosmetic == b.Cosmetic;
        }

        public int GetHashCode(LobbyPlayer obj)
        {
            return obj.PlayerID.GetHashCode();
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

    public async void RefreshLobbyData()
    {
        try
        {
            currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);

            List<LobbyPlayer> lobbyPlayers = new List<LobbyPlayer>();
            foreach (var p in currentLobby.Players)
            {
                string pName = (p.Data != null && p.Data.ContainsKey("Name")) ? p.Data["Name"].Value : "Unknown";
                string pCosmetic = (p.Data != null && p.Data.ContainsKey("Cosmetic")) ? p.Data["Cosmetic"].Value : "Default";
                bool isLocal = p.Id == AuthenticationService.Instance.PlayerId;
                lobbyPlayers.Add(new LobbyPlayer(p.Id, pName, pCosmetic, isLocal));
            }

            SetPlayers(lobbyPlayers);
        }
        catch (LobbyServiceException e) { Debug.LogError(e); }
    }

    // Replace your SubscribeToLobby method with this fixed version:

    public async void SubscribeToLobby(string lobbyId)
    {
        if (string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogError("Cannot subscribe to lobby: Lobby ID is null or empty!");
            return;
        }

        if (m_LobbyEvents != null)
        {
            Debug.LogWarning("Already subscribed to lobby events");
            return;
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

        var callbacks = new LobbyEventCallbacks();
        callbacks.LobbyChanged += OnLobbyChanged;
        callbacks.PlayerJoined += (joinedPlayers) =>
        {
            Debug.Log($"Player joined event received. Count: {joinedPlayers.Count}");
            RefreshLobbyData();
        };
        callbacks.PlayerLeft += (leftPlayers) =>
        {
            Debug.Log($"Player left event received. Count: {leftPlayers.Count}");
            RefreshLobbyData();
        };

        try
        {
            Debug.Log($"Subscribing to lobby events for lobby: {lobbyId}");
            m_LobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyId, callbacks);
            Debug.Log("✅ Successfully subscribed to lobby events!");
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"Failed to subscribe to lobby events: {ex.Message} (Code: {ex.Reason})");
            m_LobbyEvents = null;
        }
    }

    private void OnLobbyChanged(ILobbyChanges changes)
    {
        _ = HandleLobbyChangeAsync(changes);
    }

    private Task HandleLobbyChangeAsync(ILobbyChanges changes)
    {
        if (currentLobby == null) return Task.CompletedTask;

        bool stillInLobby = currentLobby.Players.Any(p => p.Id == AuthenticationService.Instance.PlayerId);
        if (!stillInLobby)
        {
            Debug.Log("Kicked");
            ClearLocalLobby();

            SceneManager.LoadScene("Lobby");
            return Task.CompletedTask;
        }

        changes.ApplyToLobby(currentLobby);
        RefreshLobbyData();

        return Task.CompletedTask;
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
        }
    }

    public void ClearLocalLobby()
    {
        UnsubscribeFromLobby();

        if (LobbyPlayerSpawner.Instance != null)
        {
            LobbyPlayerSpawner.Instance.ClearAll();
        }

        players.Clear();
        UpdateUI();

        currentLobby = null;

        Debug.Log("✅ Local lobby cleared");
    }

    public List<LobbyPlayer> GetPlayers() => players;
}
