using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Samples.Friends;
using Unity.Services.Samples.Friends.UGUI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UnityLobbyManager : MonoBehaviour
{
    public static UnityLobbyManager Instance;

    private float heartbeatTimer;
    private const float HeartbeatInterval = 15f;
    private const float PollInterval = 3.0f;
    private float pollTimer;
    private bool isJoining = false;
    public bool IsJoiningExternalLobby;
    public Lobby CurrentLobby { get; set; }

    private bool isUsingLobbyEvents = false;

    // Rate limit tracking
    private int rateLimitWarningCount = 0;
    private const int MaxRateLimitWarnings = 3;
    private float lastRateLimitWarning = 0f;
    private const float RateLimitWarningCooldown = 10f;

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

        if (!isUsingLobbyEvents)
        {
            pollTimer -= Time.deltaTime;
            if (pollTimer <= 0)
            {
                pollTimer = PollInterval;
                _ = PollLobby();
            }
        }
    }

    public void EnableLobbyEvents()
    {
        isUsingLobbyEvents = true;
        Debug.Log("✅ Lobby events enabled - polling disabled");
    }

    public void DisableLobbyEvents()
    {
        isUsingLobbyEvents = false;
        pollTimer = PollInterval;
        Debug.Log("✅ Lobby events disabled - polling re-enabled");
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
            else if (e.Reason == LobbyExceptionReason.RateLimited)
            {
                // Only log rate limit warnings occasionally
                if (Time.time - lastRateLimitWarning > RateLimitWarningCooldown)
                {
                    rateLimitWarningCount++;
                    if (rateLimitWarningCount <= MaxRateLimitWarnings)
                    {
                        Debug.LogWarning($"Lobby poll rate limited (#{rateLimitWarningCount})");
                    }
                    lastRateLimitWarning = Time.time;
                }
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

            string initialLevel = SaveManager.Instance?.data?.lastSelectedLevel ?? "None";

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = hostPlayer,
                Data = new Dictionary<string, DataObject>
                {
                    { "SelectedLevel", new DataObject(DataObject.VisibilityOptions.Public, initialLevel) }
                }
            };

            CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            pollTimer = PollInterval;

            Debug.Log($"Lobby Created: {CurrentLobby.LobbyCode} with level: {initialLevel}");
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

            if (CurrentLobby != null)
            {
                Debug.Log("Leaving current lobby before joining new one...");

                LobbyInfo.Instance?.UnsubscribeFromLobby();

                try
                {
                    await LobbyService.Instance.RemovePlayerAsync(
                        CurrentLobby.Id,
                        AuthenticationService.Instance.PlayerId
                    );
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Error leaving old lobby: {e.Message}");
                }

                CurrentLobby = null;
                LobbyInfo.Instance?.ClearLocalLobby();
            }

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
            IsJoiningExternalLobby = false;
        }
    }

    public async Task JoinLobbyByCode(string code)
    {
        isJoining = true;
        try
        {
            await EnsureSaveManagerExists();

            if (CurrentLobby != null)
            {
                Debug.Log("Leaving current lobby before joining new one...");

                LobbyInfo.Instance?.UnsubscribeFromLobby();

                try
                {
                    await LobbyService.Instance.RemovePlayerAsync(
                        CurrentLobby.Id,
                        AuthenticationService.Instance.PlayerId
                    );
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Error leaving old lobby: {e.Message}");
                }

                CurrentLobby = null;
                LobbyInfo.Instance?.ClearLocalLobby();
            }

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
            IsJoiningExternalLobby = false;
        }
    }

    private async Task EnsureSaveManagerExists()
    {
        int maxWaitFrames = 30;
        int frameCount = 0;

        while (SaveManager.Instance == null && frameCount < maxWaitFrames)
        {
            await Task.Yield();
            frameCount++;
        }

        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager failed to initialize! Creating emergency instance.");

            GameObject saveManagerObj = new GameObject("SaveManager");
            saveManagerObj.AddComponent<SaveManager>();
            DontDestroyOnLoad(saveManagerObj);
        }
        else
        {
            SaveManager.Instance.Load();
        }
    }

    private async Task HandlePostJoin()
    {
        await Task.Delay(300);

        CurrentLobby = await LobbyService.Instance.GetLobbyAsync(CurrentLobby.Id);

        if (CurrentLobby == null)
        {
            Debug.LogError("Failed to get lobby after join!");
            return;
        }

        Debug.Log($"Joined lobby: {CurrentLobby.Id} with {CurrentLobby.Players.Count} players");

        pollTimer = PollInterval;

        await SyncSaveDataToLobby();
        SyncLobbyToLocal();
        await UpdatePresenceAfterJoin();

        await Task.Delay(500);

        if (LobbyInfo.Instance != null)
        {
            LobbyInfo.Instance.SubscribeToLobby(CurrentLobby.Id);
        }

        var relationshipsManager = FindFirstObjectByType<RelationshipsManager>();
        if (relationshipsManager != null)
        {
            await relationshipsManager.EnsureFriendsConnection();
            relationshipsManager.RefreshAll();
            relationshipsManager.RefreshLocalPlayerName();
        }

        Debug.Log($"✅ Post-join complete.");
    }

    private async Task UpdatePresenceAfterJoin()
    {
        try
        {
            await FriendsService.Instance.SetPresenceAsync(
                Availability.Online,
                new Activity { Status = "In Lobby" }
            );
            Debug.Log("✅ Presence updated to 'In Lobby'");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to update presence: {e.Message}");
        }
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

    public async Task UpdateLobbyLevel(string levelName)
    {
        if (CurrentLobby == null)
        {
            Debug.LogWarning("Cannot update level: No active lobby");
            return;
        }

        if (!IsHost())
        {
            Debug.LogWarning("Only host can change level");
            return;
        }

        try
        {
            UpdateLobbyOptions options = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "SelectedLevel", new DataObject(DataObject.VisibilityOptions.Public, levelName) }
                }
            };

            CurrentLobby = await LobbyService.Instance.UpdateLobbyAsync(CurrentLobby.Id, options);

            Debug.Log($"✅ Updated lobby level to: {levelName}");

            SyncLobbyToLocal();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to update lobby level: {e.Message}");
        }
    }

    public void SyncLobbyToLocal()
    {
        if (CurrentLobby == null || LobbyInfo.Instance == null)
            return;

        Debug.Log($"Syncing lobby to local - {CurrentLobby.Players.Count} players");

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

            Debug.Log($"  Player: {displayName}, Cosmetic: {pCosmetic}, IsLocal: {isLocal}");

            lobbyPlayers.Add(new LobbyPlayer(p.Id, displayName, pCosmetic, isLocal));
        }

        LobbyInfo.Instance?.SetPlayers(lobbyPlayers);
        LobbyInfo.Instance.SetCurrentLobby(CurrentLobby);

        if (CurrentLobby.Data != null && CurrentLobby.Data.ContainsKey("SelectedLevel"))
        {
            string selectedLevel = CurrentLobby.Data["SelectedLevel"].Value;
            LobbyInfo.Instance.SetSelectedLevelFromLobby(selectedLevel);
        }

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
        if (CurrentLobby == null)
        {
            Debug.LogWarning("Cannot leave lobby: CurrentLobby is null");
            return;
        }

        Debug.Log("=== LEAVING LOBBY ===");

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Save();
        }

        string lobbyId = CurrentLobby.Id;
        bool wasHost = IsHost();
        int playerCount = CurrentLobby.Players.Count;

        Debug.Log($"Lobby info - ID: {lobbyId}, IsHost: {wasHost}, Players: {playerCount}");

        if (wasHost && playerCount == 1)
        {
            Debug.Log("Already in personal lobby alone");
            return;
        }

        if (LobbyInfo.Instance != null)
        {
            LobbyInfo.Instance.ClearLocalLobby();
        }

        CurrentLobby = null;

        try
        {
            string playerId = AuthenticationService.Instance.PlayerId;

            if (wasHost && playerCount <= 1)
            {
                await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
                Debug.Log("Deleted empty lobby");
            }
            else
            {
                await LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);
                Debug.Log("Removed player from lobby");
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Error leaving lobby: {e.Message}");
        }

        await Task.Delay(800);

        if (!IsJoiningExternalLobby)
        {
            Debug.Log("Creating new personal lobby after leaving...");
            await EnsurePersonalLobby();
        }

        IsJoiningExternalLobby = false;
        Debug.Log("✅ Left lobby successfully");
    }
}