using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Samples.Friends;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections;

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

    public bool isFullyJoined {  get; private set; }

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

        // Load saved player data if available
        if (SaveManager.Instance != null && SaveManager.Instance.data != null)
        {
            var data = SaveManager.Instance.data;
            if (!string.IsNullOrEmpty(data.playerName))
            {

            }

            // Set the selected cosmetic from saved data
            if (!string.IsNullOrEmpty(data.selectedCosmetic))
            {
                _ = SetSelectedCosmetic(data.selectedCosmetic);
            }

            // Set the last selected level if available
            if (!string.IsNullOrEmpty(data.lastSelectedLevel))
            {
                SetSelectedLevel(data.lastSelectedLevel);
            }
        }
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
            if(!isFullyJoined) return;
            await UnityLobbyManager.Instance.UpdatePlayerDataAsync(
                SaveManager.Instance.data.playerName,
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
            if (!isFullyJoined) return;
            await UnityLobbyManager.Instance.UpdatePlayerDataAsync(
                SaveManager.Instance.data.playerName,
                CosmeticName
            );
        }
    }

    public void SetCurrentLobby(Lobby lobby)
    {
        RefreshLobbyData();
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

        foreach (var incoming in newPlayers)
        {
            var existing = players.FirstOrDefault(p => p.PlayerID == incoming.PlayerID);
            if (existing != null && incoming.PlayerName == "Unknown Player")
            {
                incoming.PlayerName = existing.PlayerName;
                incoming.Cosmetic = existing.Cosmetic;
            }
        }

        players = newPlayers;

        foreach (var p in players)
        {
            p.IsLocal = p.PlayerID == AuthenticationService.Instance.PlayerId;
        }

        Debug.Log($"Save:{SaveManager.Instance.data.playerName} Auth:{AuthenticationService.Instance.PlayerName}");

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

    private void RefreshLobbyData()
    {
        var lobby = UnityLobbyManager.Instance.CurrentLobby;
        if (lobby == null) return;

        if (playerCountText != null)
            playerCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";

        // Get Level from Lobby Data
        if (lobby.Data != null && lobby.Data.ContainsKey("SelectedLevel"))
        {
            selectedLevel = lobby.Data["SelectedLevel"].Value;
            if (selectedLevelText != null) selectedLevelText.text = selectedLevel;
        }
    }

    public async void SubscribeToLobby(string lobbyId)
    {
        if (m_LobbyEvents != null) return;

        var callbacks = new LobbyEventCallbacks();
        callbacks.LobbyChanged += OnLobbyChanged;
        callbacks.PlayerJoined += (joinedPlayers) => UnityLobbyManager.Instance.SyncLobbyToLocal();
        callbacks.PlayerLeft += (leftPlayers) => UnityLobbyManager.Instance.SyncLobbyToLocal();

        try
        {
            m_LobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyId, callbacks);
        }
        catch (LobbyServiceException ex) { Debug.LogWarning(ex.Message); }
    }

    private void OnLobbyChanged(ILobbyChanges changes)
    {
        _ = HandleLobbyChangeAsync(changes);
    }

    private Task HandleLobbyChangeAsync(ILobbyChanges changes)
    {

        if (!isFullyJoined || UnityLobbyManager.Instance.CurrentLobby == null) return Task.CompletedTask;

        changes.ApplyToLobby(UnityLobbyManager.Instance.CurrentLobby);

        bool stillInLobby = UnityLobbyManager.Instance.CurrentLobby.Players.Any(p => p.Id == AuthenticationService.Instance.PlayerId);
        if (!stillInLobby)
        {
            Debug.LogWarning("Detected we are no longer in lobby via Events.");
            ClearLocalLobby();

            SceneManager.LoadScene("Lobby");
            return Task.CompletedTask;
        }

        UnityLobbyManager.Instance.SyncLobbyToLocal();
        return Task.CompletedTask;
    }

    public LobbyPlayer GetLocalPlayer()
    {
        return players.Find(p => p.PlayerID == AuthenticationService.Instance.PlayerId);
    }

    public void ClearLocalLobby()
    {
        if (LobbyPlayerSpawner.Instance != null)
        {
            LobbyPlayerSpawner.Instance.ClearAll();
        }

        players.Clear();
        UpdateUI();

        if (m_LobbyEvents != null)
        {
            _ = m_LobbyEvents.UnsubscribeAsync();
            m_LobbyEvents = null;
        }

    }

    public void MarkFullyJoined()
    {
        isFullyJoined = true;
    }

    public List<LobbyPlayer> GetPlayers() => players;
}
