using System.Collections.Generic;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using UnityEngine;

[System.Serializable]
public class LobbyActivity
{
    public string join_lobby;
}

namespace Unity.Services.Samples.Friends
{
    public class FriendsLobbyBridge : MonoBehaviour
    {
        public async void InviteFriendToLobby(string friendID)
        {
            var lobby = UnityLobbyManager.Instance.CurrentLobby;
            if (lobby == null) return;

            var activityData = new LobbyActivity
            {
                join_lobby = lobby.Id
            };

            await FriendsService.Instance.SetPresenceAsync(
                Availability.Online,
                activityData
            );

            Debug.Log("Lobby advertised via presence.");
        }
    }
}
