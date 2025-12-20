using UnityEngine;
using TMPro;
using System.Collections.Generic;
using NUnit.Framework;
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

    private List<LobbyPlayer> players = new List<LobbyPlayer>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        Instance = this;

        players.Clear();

        // Host
        players.Add(new LobbyPlayer
        {
            PlayerID = "Host",
            Cosmetic = "Default"
        });
    }

    void Start()
    {
        UpdateUI();

        ForceRespawn();
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
        players[0].Cosmetic = CosmeticName;

        UpdatePreviewModel(CosmeticName);
        UpdateUI();
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

    public bool IsLobbyFull()
    {
        return players.Count >= MaxPlayers;
    }

    public void ForceRespawn()
    {
        LobbyPlayerSpawner.Instance?.SpawnPlayers();
    }

    public List<LobbyPlayer> GetPlayers() => players;
}
