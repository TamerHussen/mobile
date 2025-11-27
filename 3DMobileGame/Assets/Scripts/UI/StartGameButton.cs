using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGameButton : MonoBehaviour
{
    public LobbyInfo lobbyInfo;

    public void StartGame()
    {
        string levelName = lobbyInfo.GetSelectedLevel();

        if (levelName == "None")
        {
            Debug.Log("No level selected!");
            return;
        }

        SceneManager.LoadScene(levelName);
    }
}
