using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class LeaveLobbyButton : MonoBehaviour
{
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

        await lobbyManager.LeaveLobby();

        Debug.Log(" Left lobby successfully");
    }
    public void OnQuitButton()
    {
        Debug.Log("Quitting game...");

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Save();
        }

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}