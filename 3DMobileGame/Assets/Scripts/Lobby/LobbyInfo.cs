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
        Instance = this;
    }

    void Start()
    {
        // Host
        players.Add(new LobbyPlayer
        {
            PlayerID = "Host",
            Cosmetic = "Default"
        });

        UpdateUI();

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

        players.Add(new LobbyPlayer
        {
            PlayerID = id,
            Cosmetic = "Default"
        });

        UpdateUI();

        FindFirstObjectByType<LobbyPlayerSpawner>()?.SpawnPlayers();
    }
    // test leave friend
    public void RemoveTestPlayer()
    {
        if (players.Count <= 1) return;

        players.RemoveAt(players.Count - 1);

        UpdateUI();

        FindFirstObjectByType<LobbyPlayerSpawner>()?.SpawnPlayers();
    }

     public List<LobbyPlayer> GetPlayers() => players;
}
