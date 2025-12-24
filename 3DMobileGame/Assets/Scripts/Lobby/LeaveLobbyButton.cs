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

        bool isHost = lobbyManager.CurrentLobby.HostId == AuthenticationService.Instance.PlayerId;

        if (isHost && lobbyManager.CurrentLobby.Players.Count == 1)
        {
            Debug.Log("Already in personal lobby.");
            return;
        }

        await lobbyManager.LeaveLobby();
        SceneManager.LoadScene(LobbySceneName);
    }


}
