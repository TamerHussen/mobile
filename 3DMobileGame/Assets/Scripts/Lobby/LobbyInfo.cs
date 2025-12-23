using NUnit.Framework;
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
using Unity.VisualScripting;

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

    public void SetSelectedLevel(string levelName)
    {
        selectedLevel = levelName;
        UpdateUI();
    }

    public void SetSelectedCosmetic(string CosmeticName)
    {
        selectedCosmetic = CosmeticName;

        if (!HasHost())
        {
            Debug.LogWarning("SetSelectedCosmetic called but no players exist yet.");
            return;
        }

        players[0].Cosmetic = CosmeticName;

        UpdatePreviewModel(CosmeticName);
        UpdateUI();
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

        players = newPlayers;
        UpdateUI();
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
        LobbyPlayerSpawner.Instance?.SpawnPlayers();
    }

    public void UpdateHostName(string NewName)
    {
        if (players.Count == 0) return;

        players[0].PlayerName = NewName;
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

            List<LobbyPlayer> updatedList = new List<LobbyPlayer>();
            foreach (var p in currentLobby.Players)
            {
                updatedList.Add(new LobbyPlayer
                {
                    PlayerID = p.Id,
                    PlayerName = p.Data["Name"].Value,
                    Cosmetic = p.Data["Cosmetic"].Value
                });
            }

            SetPlayers(updatedList);
        }
        catch (LobbyServiceException e) { Debug.LogError(e); }
    }

    public async void SubscribeToLobby(string lobbyId)
    {
        if (UnityLobbyManager.Instance.CurrentLobby != null)
        {
            currentLobby = UnityLobbyManager.Instance.CurrentLobby;
        }

        var callbacks = new LobbyEventCallbacks();
        callbacks.LobbyChanged += OnLobbyChanged;
        callbacks.PlayerJoined += (joinedPlayers) => RefreshLobbyData();
        callbacks.PlayerLeft += (leftPlayers) => RefreshLobbyData();

        try
        {
            m_LobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyId, callbacks);
        }
        catch (LobbyServiceException ex) { Debug.LogWarning(ex.Message); }
    }

    private void OnLobbyChanged(ILobbyChanges changes)
    {
        if (currentLobby == null) return;
        changes.ApplyToLobby(currentLobby);
        RefreshLobbyData();
    }

    public List<LobbyPlayer> GetPlayers() => players;
}
