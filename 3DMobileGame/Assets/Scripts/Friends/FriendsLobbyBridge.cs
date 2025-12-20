using UnityEngine;

namespace Unity.Services.Samples.Friends
{
    public class FriendsLobbyBridge : MonoBehaviour
    {
        public void InviteFriendToLobby(string friendID)
        {
            if (LobbyInfo.Instance == null) return;

            if (LobbyInfo.Instance.IsLobbyFull())
            {
                Debug.Log("lobby full");
                return;
            }

            if (LobbyInfo.Instance.GetPlayers().Exists(p => p.PlayerID == friendID)) return;

            LobbyInfo.Instance.AddTestPlayer(friendID);
        }
    }
}

