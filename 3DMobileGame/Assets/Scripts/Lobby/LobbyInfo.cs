using UnityEngine;
using TMPro;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.VisualScripting;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Samples.Friends;
using System.Threading.Tasks;

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

    async void Start()
    {
        await WaitForAuthentication();

        EnsureHostExists();
        UpdateUI();
        ForceRespawn();
        SetLobbyPresence();
    }
    async Task WaitForAuthentication()
    {
        while (!ServicesBootstrapper.IsReady)
            await Task.Yield();
    }

    void EnsureHostExists()
    {
        if (players.Count > 0) return;

        string playerName = AuthenticationService.Instance.PlayerName;

        players.Add(new LobbyPlayer
        {
            PlayerID = string.IsNullOrEmpty(playerName) ? "Player" : playerName,
            Cosmetic = "Default"
        });
    }

    async void SetLobbyPresence()
    {
        if (!ServicesBootstrapper.IsReady)
            return;

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

    // test join friend
    public void AddTestPlayer(string id)
    {
        if (players.Count >= MaxPlayers) return;

        if (players.Exists(p => p.PlayerID == id)) return;

        players.Add(new LobbyPlayer
        {
            PlayerID = id,
            Cosmetic = "Default"
        });

        UpdateUI();

        ForceRespawn();
    }
    // test leave friend
    public void RemoveTestPlayer(string id)
    {
        if (id == "Host") return;

        var playerToRemove = players.Find(p => p.PlayerID == id);
        if (playerToRemove == null) return;

        players.Remove(playerToRemove);


        UpdateUI();
        ForceRespawn();
    }

    // fake invite
    public void DebugInviteFakeFriend()
    {
        if (LobbyInfo.Instance.IsLobbyFull()) return;
        LobbyInfo.Instance.AddTestPlayer("Friends_" + Random.Range(1, 999));
    }

    // host can kick players
    public void KickPlayer(string playerID)
    {
        if (playerID == players[0].PlayerID) return;
        RemoveTestPlayer(playerID);
    }

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

        players[0].PlayerID = NewName;
        ForceRespawn();
        UpdateUI();
    }

    public List<LobbyPlayer> GetPlayers() => players;
}
