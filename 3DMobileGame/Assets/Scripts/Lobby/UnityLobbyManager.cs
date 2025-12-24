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
    public bool IsJoiningExternalLobby;
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

        if (IsHost())
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0)
            {
                heartbeatTimer = HeartbeatInterval;
                _ = LobbyService.Instance.SendHeartbeatPingAsync(CurrentLobby.Id);
            }
        }

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
            if (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                Debug.LogWarning("Lobby closed");
                CurrentLobby = null;
                LobbyInfo.Instance?.ClearLocalLobby();
                await EnsurePersonalLobby();
            }
        }
    }

    public async Task CreateLobby(int maxPlayers)
    {
        try
        {
            await EnsureSaveManagerExists();

            Player hostPlayer = GetLocalPlayerData();
            string lobbyName = hostPlayer.Data["Name"].Value + "'s Lobby";

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = hostPlayer
            };

            CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            pollTimer = PollInterval;

            Debug.Log($"Lobby Created: {CurrentLobby.LobbyCode} with player data - Name: {hostPlayer.Data["Name"].Value}, Cosmetic: {hostPlayer.Data["Cosmetic"].Value}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Create Lobby failed: {e.Message}");
        }
    }

    public async Task JoinLobbyById(string lobbyId)
    {
        isJoining = true;
        try
        {
            await EnsureSaveManagerExists();

            var options = new JoinLobbyByIdOptions { Player = GetLocalPlayerData() };
            CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);
            await HandlePostJoin();
        }
        catch (Exception e)
        {
            Debug.LogError($"Join ID failed: {e.Message}");
        }
        finally
        {
            isJoining = false;
        }
    }

    public async Task JoinLobbyByCode(string code)
    {
        isJoining = true;
        try
        {
            await EnsureSaveManagerExists();

            var options = new JoinLobbyByCodeOptions { Player = GetLocalPlayerData() };
            CurrentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code, options);
            await HandlePostJoin();
        }
        catch (Exception e)
        {
            Debug.LogError($"Join Code failed: {e.Message}");
        }
        finally
        {
            isJoining = false;
        }
    }

    // NEW METHOD: Ensures SaveManager is properly instantiated
    private async Task EnsureSaveManagerExists()
    {
        int maxWaitFrames = 30; // Wait up to ~0.5 seconds
        int frameCount = 0;

        while (SaveManager.Instance == null && frameCount < maxWaitFrames)
        {
            await Task.Yield(); // Wait one frame
            frameCount++;
        }

        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager failed to initialize! Creating emergency instance.");

            // Create SaveManager if it doesn't exist
            GameObject saveManagerObj = new GameObject("SaveManager");
            saveManagerObj.AddComponent<SaveManager>();
            DontDestroyOnLoad(saveManagerObj);
        }
        else
        {
            // Reload data to ensure it's fresh
            SaveManager.Instance.Load();
        }
    }

    private async Task HandlePostJoin()
    {
        await Task.Delay(150);

        // CRITICAL FIX: Refresh lobby data FIRST
        CurrentLobby = await LobbyService.Instance.GetLobbyAsync(CurrentLobby.Id);

        if (CurrentLobby == null)
        {
            Debug.LogError("Failed to get lobby after join!");
            return;
        }

        Debug.Log($"Joined lobby: {CurrentLobby.Id} with {CurrentLobby.Players.Count} players");

        pollTimer = PollInterval;

        // Sync player data to lobby
        await SyncSaveDataToLobby();

        // Sync lobby to local view
        SyncLobbyToLocal();

        // CRITICAL FIX: Subscribe AFTER everything is set up
        if (LobbyInfo.Instance != null)
        {
            LobbyInfo.Instance.SubscribeToLobby(CurrentLobby.Id);
        }
        else
        {
            Debug.LogError("LobbyInfo.Instance is null during HandlePostJoin!");
        }

        Debug.Log($"Post-join complete. Player data synced.");
    }

    private Player GetLocalPlayerData()
    {
        string playerName = "Unknown Player";
        string cosmetic = "Default";

        if (SaveManager.Instance != null && SaveManager.Instance.data != null)
        {
            if (!string.IsNullOrEmpty(SaveManager.Instance.data.playerName))
                playerName = SaveManager.Instance.data.playerName;

            if (!string.IsNullOrEmpty(SaveManager.Instance.data.selectedCosmetic))
                cosmetic = SaveManager.Instance.data.selectedCosmetic;

            Debug.Log($"GetLocalPlayerData - Name={playerName}, Cosmetic={cosmetic}");
        }
        else
        {
            Debug.LogWarning("SaveManager or data is null! Using defaults.");
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

        try
        {
            if (SaveManager.Instance != null && SaveManager.Instance.data != null)
            {
                SaveManager.Instance.data.playerName = name;
                SaveManager.Instance.data.selectedCosmetic = cosmetic;
                SaveManager.Instance.Save();
            }

            UpdatePlayerOptions options = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "Name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, name) },
                    { "Cosmetic", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, cosmetic) }
                }
            };

            CurrentLobby = await LobbyService.Instance.UpdatePlayerAsync(
                CurrentLobby.Id,
                AuthenticationService.Instance.PlayerId,
                options
            );

            Debug.Log($"Player data updated: Name={name}, Cosmetic={cosmetic}");
            SyncLobbyToLocal();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to update player data: {e.Message}");
        }
    }

    public async Task SyncSaveDataToLobby()
    {
        if (CurrentLobby == null)
        {
            Debug.LogWarning("Cannot sync: CurrentLobby is null");
            return;
        }

        if (CurrentLobby.Players == null || CurrentLobby.Players.Count == 0)
        {
            Debug.LogWarning("Cannot sync: No players in lobby");
            return;
        }

        if (!CurrentLobby.Players.Any(p => p.Id == AuthenticationService.Instance.PlayerId))
        {
            Debug.LogWarning("Cannot sync: Local player not found in lobby");
            return;
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Load();
        }

        try
        {
            string playerName = SaveManager.Instance?.data?.playerName ?? "Unknown Player";
            string cosmetic = SaveManager.Instance?.data?.selectedCosmetic ?? "Default";

            UpdatePlayerOptions options = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "Name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) },
                    { "Cosmetic", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, cosmetic) }
                }
            };

            CurrentLobby = await LobbyService.Instance.UpdatePlayerAsync(
                CurrentLobby.Id,
                AuthenticationService.Instance.PlayerId,
                options
            );

            Debug.Log($"Synced SaveManager to Lobby: Name={playerName}, Cosmetic={cosmetic}");
            SyncLobbyToLocal();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to sync save data to lobby: {e.Message}");
        }
    }

    public void SyncLobbyToLocal()
    {
        if (CurrentLobby == null || LobbyInfo.Instance == null)
            return;

        var lobbyPlayers = new List<LobbyPlayer>();

        foreach (var p in CurrentLobby.Players)
        {
            string displayName = (p.Data != null && p.Data.ContainsKey("Name"))
                ? p.Data["Name"].Value
                : "Joining...";

            string pCosmetic = (p.Data != null && p.Data.ContainsKey("Cosmetic"))
                ? p.Data["Cosmetic"].Value
                : "Default";

            bool isLocal = p.Id == AuthenticationService.Instance.PlayerId;

            lobbyPlayers.Add(new LobbyPlayer(p.Id, displayName, pCosmetic, isLocal));
        }

        LobbyInfo.Instance?.SetPlayers(lobbyPlayers);
        LobbyInfo.Instance.SetCurrentLobby(CurrentLobby);
        FindFirstObjectByType<FriendsViewUGUI>()?.Refresh();
    }

    public async Task EnsurePersonalLobby()
    {
        if (CurrentLobby != null)
        {
            Debug.Log("Already in a lobby, skipping EnsurePersonalLobby");
            return;
        }

        Debug.Log("Creating personal lobby...");

        await EnsureSaveManagerExists();

        await CreateLobby(3);

        if (CurrentLobby == null)
        {
            Debug.LogError("Failed to create personal lobby!");
            return;
        }

        await SyncSaveDataToLobby();
        SyncLobbyToLocal();

        if (LobbyInfo.Instance != null)
        {
            LobbyInfo.Instance.SubscribeToLobby(CurrentLobby.Id);
        }

        Debug.Log($"Personal lobby created and subscribed: {CurrentLobby.Id}");
    }

    public async Task LeaveLobby()
    {
        if (CurrentLobby == null) return;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Save();
        }

        IsJoiningExternalLobby = true;

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

        CurrentLobby = null;
        LobbyInfo.Instance?.ClearLocalLobby();

        if (!IsJoiningExternalLobby)
        {
            SceneManager.LoadScene("Lobby");
            await EnsurePersonalLobby();
        }

        IsJoiningExternalLobby = false;
    }
}