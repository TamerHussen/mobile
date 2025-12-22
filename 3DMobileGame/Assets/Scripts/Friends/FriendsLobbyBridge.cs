using System.Collections.Generic;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using UnityEngine;

namespace Unity.Services.Samples.Friends
{
    public class FriendsLobbyBridge : MonoBehaviour
    {
        [System.Serializable]
        public class InviteMessage
        {
            public string LobbyCode;
        }

        public async void InviteFriendToLobby(string targetId)
        {
            try
            {
                string code = UnityLobbyManager.Instance.CurrentLobby?.LobbyCode;

                if (string.IsNullOrEmpty(code))
                {
                    Debug.LogWarning("Cannot invite: No active lobby or join code");
                    return;
                }

                var invite = new InviteMessage { LobbyCode = code };

                await FriendsService.Instance.MessageAsync(targetId, invite);

                Debug.Log($"Invite sent to {targetId} with code {code}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Invite failed: {e.Message}");
            }
        }
    }
}
