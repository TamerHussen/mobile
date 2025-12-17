using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
        friends.Add(new FriendData { ID = "3", DisplayName = "MJ_PJ_DJ", isOnline = true });

    }

    // search
    public FriendData Search(string query)
    {
        return friends.FirstOrDefault(f =>
        f.DisplayName.ToLower().Contains(query.ToLower()) ||
        f.ID == query);
    }
    public void InviteFriend(FriendData friend)
    {
        if (!friend.isOnline) return;

        LobbyInfo.Instance.AddTestPlayer(friend.DisplayName);
    }

    public void RemoveFriend(FriendData friend)
    {
       if (friend == null) return;

       if (friends.Contains(friend))
        {
            friends.Remove(friend);
        }

        LobbyInfo.Instance.RemoveTestPlayer(friend.DisplayName);
    }
}
