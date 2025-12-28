using UnityEngine;
using TMPro;

public class SaveManagerDebugger : MonoBehaviour
{
    [Header("Debug UI")]
    public TextMeshProUGUI debugText;
    public GameObject debugPanel;

    void Start()
    {
        if (debugPanel != null)
            debugPanel.SetActive(false);
    }

    void Update()
    {
        if (debugPanel != null && debugPanel.activeSelf)
        {
            UpdateDebugInfo();
        }
    }

    public void ToggleDebugPanel()
    {
        if (debugPanel != null)
        {
            debugPanel.SetActive(!debugPanel.activeSelf);
            if (debugPanel.activeSelf)
                UpdateDebugInfo();
        }
    }

    private void UpdateDebugInfo()
    {
        if (debugText == null) return;

        string info = "=== SAVE MANAGER DEBUG ===\n\n";

        if (SaveManager.Instance == null)
        {
            info += "SaveManager.Instance: NULL\n";
        }
        else if (SaveManager.Instance.data == null)
        {
            info += "SaveManager.Instance: EXISTS\n";
            info += "SaveManager.data: NULL\n";
        }
        else
        {
            info += "SaveManager.Instance: EXISTS\n";
            info += "SaveManager.data: EXISTS\n\n";
            info += $"Player Name: {SaveManager.Instance.data.playerName}\n";
            info += $"Cosmetic: {SaveManager.Instance.data.selectedCosmetic}\n";
            info += $"Level: {SaveManager.Instance.data.lastSelectedLevel}\n";
            info += $"Coins: {SaveManager.Instance.data.coins}\n";
        }

        info += "\n";

        if (UnityLobbyManager.Instance == null)
        {
            info += "UnityLobbyManager: NULL\n";
        }
        else
        {
            info += "UnityLobbyManager: EXISTS\n";
            if (UnityLobbyManager.Instance.CurrentLobby != null)
            {
                info += $"Lobby ID: {UnityLobbyManager.Instance.CurrentLobby.Id}\n";
                info += $"Players: {UnityLobbyManager.Instance.CurrentLobby.Players.Count}\n";
            }
            else
            {
                info += "No active lobby\n";
            }
        }

        info += "\n";

        if (LobbyInfo.Instance == null)
        {
            info += "LobbyInfo: NULL\n";
        }
        else
        {
            info += "LobbyInfo: EXISTS\n";
            info += $"Selected Cosmetic: {LobbyInfo.Instance.GetSelectedCosmetic()}\n";
            info += $"Selected Level: {LobbyInfo.Instance.GetSelectedLevel()}\n";
            info += $"Players in Lobby: {LobbyInfo.Instance.GetPlayers().Count}\n";
        }

        debugText.text = info;
    }

    public void LogDebugInfo()
    {
        Debug.Log("=== SAVE MANAGER DEBUG ===");

        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager.Instance is NULL!");
        }
        else if (SaveManager.Instance.data == null)
        {
            Debug.LogError("SaveManager.Instance.data is NULL!");
        }
        else
        {
            Debug.Log($"Player: {SaveManager.Instance.data.playerName}");
            Debug.Log($"Cosmetic: {SaveManager.Instance.data.selectedCosmetic}");
            Debug.Log($"Level: {SaveManager.Instance.data.lastSelectedLevel}");
        }

        if (UnityLobbyManager.Instance == null)
        {
            Debug.LogError("UnityLobbyManager.Instance is NULL!");
        }
        else
        {
            Debug.Log($"UnityLobbyManager exists. Lobby: {(UnityLobbyManager.Instance.CurrentLobby != null ? "Active" : "None")}");
        }

        if (LobbyInfo.Instance == null)
        {
            Debug.LogError("LobbyInfo.Instance is NULL!");
        }
        else
        {
            Debug.Log($"LobbyInfo exists. Players: {LobbyInfo.Instance.GetPlayers().Count}");
        }
    }
}