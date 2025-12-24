using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Samples.Friends.UGUI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UnityLobbyManager : MonoBehaviour
{
    public static UnityLobbyManager Instance;

    private float heartbeatTimer;
    private const float HeartbeatInterval = 15f;
    private const float PollInterval = 1.5f;
    private float pollTimer;
    private bool isJoining = false;
    public Lobby CurrentLobby { get; set; }


    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (CurrentLobby == null || isJoining)
            return;

        // Host heartbeat
        if (IsHost())
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0)
            {
                heartbeatTimer = HeartbeatInterval;
                _ = LobbyService.Instance.SendHeartbeatPingAsync(CurrentLobby.Id);
            }
        }

        // Poll lobby state
        pollTimer -= Time.deltaTime;
        if (pollTimer <= 0)
        {
            pollTimer = PollInterval;
            _ = PollLobby();
        }
    }

    bool IsHost()
    {
        return CurrentLobby.HostId == AuthenticationService.Instance.PlayerId;
    }
    async Task PollLobby()
    {
        if (isJoining || CurrentLobby == null) return;

        try
        {
            CurrentLobby = await LobbyService.Instance.GetLobbyAsync(CurrentLobby.Id);
            SyncLobbyToLocal();
        }
        catch (LobbyServiceException e)
        {
            if (!isJoining && e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                Debug.LogWarning("Lobby closed");

                CurrentLobby = null;
                LobbyInfo.Instance?.ClearLocalLobby();
                await EnsurePersonalLobby();
            }

        }
    }

    // HOST Migration
    void HandleHostMigration()
    {
        if (CurrentLobby == null)
            return;

        if (IsHost())
            return;

        if (CurrentLobby.HostId != AuthenticationService.Instance.PlayerId)
            return;

        Debug.Log("I am new host");
    }


    // HOST creates lobby
    public async Task CreateLobby(int maxPlayers)
    {
        try
        {
            Player hostPlayer = GetLocalPlayerData();
            string lobbyName = hostPlayer.Data["Name"].Value + "'s Lobby";

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = hostPlayer
            };

            CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            pollTimer = PollInterval;
            Debug.Log("Lobby Created: " + CurrentLobby.LobbyCode);
        }
        catch (Exception e) { Debug.LogError($"Create Lobby failed: {e.Message}"); }
    }


    // JOIN by invite
    public async Task JoinLobbyById(string lobbyId)
    {
        if(isJoining) return;
        isJoining = true;
        try
        {
            var localPlayer = GetLocalPlayerData();
            var options = new JoinLobbyByIdOptions { Player = localPlayer };
            CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);

            await HandlePostJoin();
        }
        catch (Exception e)
        {
            Debug.LogError($"Join ID failed: {e.Message}");
            isJoining = false;
        }
    }

    public async Task JoinLobbyByCode(string code)
    {
        if (isJoining) return;
        isJoining = true;
        try
        {
            var localPlayer = GetLocalPlayerData();
            var options = new JoinLobbyByCodeOptions { Player = localPlayer };
            CurrentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code, options);

            await HandlePostJoin();
        }
        catch (Exception e) 
        { 
            Debug.LogError($"Join Code failed: {e.Message}");
            isJoining = false;
        }
    }

    // Shared logic to avoid repeating Task.Delay and Sync calls
    private async Task HandlePostJoin()
    {
        await Task.Delay(150);
        CurrentLobby = await LobbyService.Instance.GetLobbyAsync(CurrentLobby.Id);

        pollTimer = PollInterval;

        LobbyInfo.Instance.SubscribeToLobby(CurrentLobby.Id);
        SyncLobbyToLocal();

        LobbyInfo.Instance.MarkFullyJoined();
        _ = DelayedSyncAfterJoin();
    }

    private async Task DelayedSyncAfterJoin()
    {
        await Task.Delay(500);
        await SyncSaveDataToLobby();
    }


    private Player GetLocalPlayerData()
    {
        string playerName = name;
        string cosmetic = "Default";

        // Use saved player data if available
        if (SaveManager.Instance != null && SaveManager.Instance.data != null)
        {
            if (!string.IsNullOrEmpty(SaveManager.Instance.data.playerName))
                playerName = SaveManager.Instance.data.playerName;

            if (!string.IsNullOrEmpty(SaveManager.Instance.data.selectedCosmetic))
                cosmetic = SaveManager.Instance.data.selectedCosmetic;
        }

        return new Player(id: AuthenticationService.Instance.PlayerId)
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "Name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) },
                { "Cosmetic", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, cosmetic) }
            }
        };
    }

    public async Task UpdatePlayerDataAsync(string name, string cosmetic)
    {
        if (CurrentLobby == null) return;

        var finalName = !string.IsNullOrEmpty(name)
            ? name
            : SaveManager.Instance?.data?.playerName;

        var finalCosmetic = !string.IsNullOrEmpty(cosmetic)
            ? cosmetic
            : SaveManager.Instance?.data?.selectedCosmetic;

        if (string.IsNullOrEmpty(finalName))
            return;

        UpdatePlayerOptions options = new UpdatePlayerOptions
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "Name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, finalName) },
                { "Cosmetic", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, finalCosmetic) }
            }
        };

        CurrentLobby = await LobbyService.Instance.UpdatePlayerAsync(
            CurrentLobby.Id,
            AuthenticationService.Instance.PlayerId,
            options
        );

        Debug.Log("Player data updated successfully.");

    }


    // link data to lobby
    public async Task SyncSaveDataToLobby()
    {
        if (CurrentLobby == null) return;
        
        if (CurrentLobby.Players == null || CurrentLobby.Players.Count == 0) return;

        if (!CurrentLobby.Players.Any(p =>
            p.Id == AuthenticationService.Instance.PlayerId))
            return;

        try
        {
            UpdatePlayerOptions options = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
            {
                { "Name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, SaveManager.Instance.data.playerName) },
                { "Cosmetic", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, SaveManager.Instance.data.selectedCosmetic) }
            }
            };

            CurrentLobby = await LobbyService.Instance.UpdatePlayerAsync(
                CurrentLobby.Id,
                AuthenticationService.Instance.PlayerId,
                options
            );

            Debug.Log("Lobby data synced with SaveManager.");
        }
        catch (LobbyServiceException e) { Debug.LogError(e); }
    }

    // lobby sync
    public void SyncLobbyToLocal()
    {
        if (CurrentLobby == null || LobbyInfo.Instance == null)
            return;

        var lobbyPlayers = new List<LobbyPlayer>();

        foreach (var p in CurrentLobby.Players)
        {
            string displayName =(p.Data != null && p.Data.ContainsKey("Name") && !string.IsNullOrEmpty(p.Data["Name"].Value))
                    ? p.Data["Name"].Value
                    : "Unknown Player";

            string pCosmetic = (p.Data != null && p.Data.ContainsKey("Cosmetic"))
                ? p.Data["Cosmetic"].Value
                : "Default";

            bool isLocal = p.Id == AuthenticationService.Instance.PlayerId;
            lobbyPlayers.Add(new LobbyPlayer(p.Id, displayName, pCosmetic, isLocal));

            Debug.Log($"SYNC {p.Id} | Name={displayName} | Cosmetic={pCosmetic}");

        }

        LobbyInfo.Instance?.SetPlayers(lobbyPlayers);
        LobbyInfo.Instance.SetCurrentLobby(CurrentLobby);
        FindFirstObjectByType<FriendsViewUGUI>()?.Refresh();
    }

    // Make lobby
    public async Task EnsurePersonalLobby()
    {
        if (CurrentLobby != null)
            return;

        Debug.Log("Creating personal lobby...");
        await CreateLobby(3);

        await SyncSaveDataToLobby();

        LobbyInfo.Instance.SubscribeToLobby(CurrentLobby.Id);
        SyncLobbyToLocal();
    }

    // LEAVE lobby
    public async Task LeaveLobby(bool willJoinAnother = false)
    {
        isJoining = true;

        if (CurrentLobby == null) return;
        
        try
        {
            string playerId = AuthenticationService.Instance.PlayerId;
            if (IsHost() && CurrentLobby.Players.Count <= 1)
            {
                await LobbyService.Instance.DeleteLobbyAsync(CurrentLobby.Id);
            }
            else
            {

                await LobbyService.Instance.RemovePlayerAsync(CurrentLobby.Id, playerId);

            }
            Debug.Log("Left or deleted lobby.");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Error leaving lobby: {e.Message}");
        }

        await Task.Delay(200);

        CurrentLobby = null;
        LobbyInfo.Instance?.ClearLocalLobby();
        SceneManager.LoadScene("Lobby");

        if (!willJoinAnother)
        {
            await EnsurePersonalLobby();
            isJoining = false;
        }
    }

}
