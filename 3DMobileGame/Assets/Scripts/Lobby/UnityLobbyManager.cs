using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class UnityLobbyManager : MonoBehaviour
{
    public static UnityLobbyManager Instance;

    private float heartbeatTimer;
    private const float HeartbeatInterval = 15f;
    private const float PollInterval = 1.5f;
    private float pollTimer;

    public Lobby CurrentLobby { get; private set; }


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
        if (CurrentLobby == null)
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
        try
        {
            CurrentLobby = await LobbyService.Instance.GetLobbyAsync(CurrentLobby.Id);
            SyncLobbyToLocal();
        }
        catch (LobbyServiceException)
        {
            Debug.LogWarning("Lobby no longer exists. Recreating personal lobby.");

            CurrentLobby = null;
            LobbyInfo.Instance?.SetPlayers(new List<LobbyPlayer>());

            await EnsurePersonalLobby();
        }
    }

    // HOST Migration
    async Task HandleHostMigration()
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
        string lobbyName = AuthenticationService.Instance.PlayerName + "'s Lobby";

        var playerData = new Dictionary<string, PlayerDataObject>
    {
        {
            "Name",
            new PlayerDataObject(
                PlayerDataObject.VisibilityOptions.Member,
                AuthenticationService.Instance.PlayerName
            )
        },
        {
            "Cosmetic",
            new PlayerDataObject(
                PlayerDataObject.VisibilityOptions.Member,
                "Default"
            )
        }
    };

        CreateLobbyOptions options = new CreateLobbyOptions
        {
            IsPrivate = false,
            Player = new Player(
                id: AuthenticationService.Instance.PlayerId,
                data: playerData
            )
        };

        CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(
            lobbyName,
            maxPlayers,
            options
        );

        Debug.Log("Lobby Created: " + CurrentLobby.LobbyCode);
    }


    // JOIN by invite
    public async Task JoinLobbyById(string lobbyId)
    {
        CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
        SyncLobbyToLocal();
    }

    // lobby sync
    public void SyncLobbyToLocal()
    {
        if (CurrentLobby == null || LobbyInfo.Instance == null)
            return;

        var lobbyPlayers = new List<LobbyPlayer>();

        foreach (var p in CurrentLobby.Players)
        {
            string name = p.Data != null && p.Data.ContainsKey("Name")
                ? p.Data["Name"].Value
                : "Player";

            string cosmetic = p.Data != null && p.Data.ContainsKey("Cosmetic")
                ? p.Data["Cosmetic"].Value
                : "Default";

            lobbyPlayers.Add(new LobbyPlayer
            {
                PlayerID = name,
                Cosmetic = cosmetic
            });
        }

        LobbyInfo.Instance.SetPlayers(lobbyPlayers);
    }

    // Make lobby
    public async Task EnsurePersonalLobby()
    {
        if (CurrentLobby != null)
            return;

        Debug.Log("Creating personal lobby...");
        await CreateLobby(3);
    }

    // LEAVE lobby
    public async Task LeaveLobby()
    {
        if (CurrentLobby == null) return;

        bool isHost = IsHost();
        var lobbyId = CurrentLobby.Id;

        await LobbyService.Instance.RemovePlayerAsync(
            lobbyId,
            AuthenticationService.Instance.PlayerId
        );

        CurrentLobby = null;

        if (!isHost)
        {
            await EnsurePersonalLobby();
        }
    }

}
