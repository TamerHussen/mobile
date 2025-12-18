using UnityEngine;
using UnityEngine.SceneManagement;

public class LeaveLobbyButton : MonoBehaviour
{
    [SerializeField] private string LobbySceneName = "Lobby";

    public void Leave()
    {
        SceneManager.LoadScene(LobbySceneName);
    }
}
