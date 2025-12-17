using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class FriendManager : MonoBehaviour
{
    public static FriendManager instance;

    public List<FriendData> friends = new();

    void Awake()
    {
        instance = this;
    }

    // temp data
    void Start()
    {
        friends.Add(new FriendData { ID = "1", DisplayName = "TimothyThe1ST", isOnline = true });
        friends.Add(new FriendData { ID = "2", DisplayName = "FartSmella_SmartFella", isOnline = false });

    }

    public void InviteFriend(FriendData friend)
    {
        if (!friend.isOnline) return;

        LobbyInfo.Instance.AddTestPlayer(friend.DisplayName);
    }
}
