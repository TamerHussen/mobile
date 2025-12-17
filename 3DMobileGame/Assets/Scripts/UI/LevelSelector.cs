using UnityEngine;

public class LevelSelector : MonoBehaviour
{

    public void SelectLevel(string levelName)
    {
        LobbyInfo.Instance.SetSelectedLevel(levelName);
    }
}
