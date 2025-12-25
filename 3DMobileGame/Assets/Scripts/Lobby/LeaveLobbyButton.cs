using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LeaveLobbyButton : MonoBehaviour
{
    [SerializeField] private string LobbySceneName = "Lobby";

    public async void Leave()
    {
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
            Debug.Log("Already in personal lobby alone, nothing to do");
            return;
        }

        // Leave the lobby
        await lobbyManager.LeaveLobby();

        Debug.Log("✅ Left lobby, new personal lobby created");
    }
}