using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class LeaveLobbyButton : MonoBehaviour
{
    [SerializeField] private string LobbySceneName = "Lobby";

    public async void Leave()
    {
        Debug.Log("=== LEAVE BUTTON CLICKED ===");

        var lobbyManager = UnityLobbyManager.Instance;
        if (lobbyManager == null)
        {
            Debug.LogError("UnityLobbyManager not found!");
            return;
        }

        if (lobbyManager.CurrentLobby == null)
        {
            Debug.LogWarning("No lobby to leave");
            return;
        }

        bool isHost = lobbyManager.CurrentLobby.HostId == AuthenticationService.Instance.PlayerId;
        int playerCount = lobbyManager.CurrentLobby.Players.Count;

        Debug.Log($"Leaving lobby - IsHost: {isHost}, PlayerCount: {playerCount}");

        // Check if already in personal lobby alone
        if (isHost && playerCount == 1)
        {
            Debug.Log("Already in personal lobby alone");
            return;
        }

        if (LobbyInfo.Instance != null)
        {
            LobbyInfo.Instance.UnsubscribeFromLobby();

            if (LobbyPlayerSpawner.Instance != null)
            {
                LobbyPlayerSpawner.Instance.ClearAll();
            }
        }

        // Leave the lobby
        await lobbyManager.LeaveLobby();

        Debug.Log("✅ Left lobby successfully");
    }
}