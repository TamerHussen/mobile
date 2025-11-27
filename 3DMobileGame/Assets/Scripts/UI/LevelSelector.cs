using UnityEngine;

public class LevelSelector : MonoBehaviour
{
    public LobbyInfo lobbyInfo;

    public void SelectLevel(string levelName)
    {
        lobbyInfo.SetSelectedLevel(levelName);
    }
}
