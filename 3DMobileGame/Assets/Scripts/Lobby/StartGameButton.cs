using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Samples.Friends;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGameButton : MonoBehaviour
{
    public void StartGame()
    {
        if (LobbyInfo.Instance.GetSelectedLevel() == "None")
        {
            Debug.Log("No Levels have been selected!");
            return;
        }

        GameSessionData.Instance.LevelName = LobbyInfo.Instance.GetSelectedLevel();
        GameSessionData.Instance.players = LobbyInfo.Instance.GetPlayers();

        SceneManager.LoadScene(GameSessionData.Instance.LevelName);
        SetGamePresence();
    }

    async void SetGamePresence()
    {
        await FriendsService.Instance.SetPresenceAsync(
            Availability.Online,
            new Activity { Status = "In Game" }
        );
    }
}
