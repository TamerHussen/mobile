using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LeaveLobbyButton : MonoBehaviour
{
    [SerializeField] private string LobbySceneName = "Lobby";

    public async void Leave()
    {
        var lobbyManager = UnityLobbyManager.Instance;
        if (lobbyManager == null) return;

        var lobby = lobbyManager.CurrentLobby;
        if (lobby == null) return;

        bool isHost = lobby.HostId == AuthenticationService.Instance.PlayerId;

        if (isHost && lobby.Players.Count == 1)
        {
            Debug.Log("Already in personal lobby.");
            return;
        }

        await lobbyManager.LeaveLobby();
        SceneManager.LoadScene("Lobby");
    }


}
