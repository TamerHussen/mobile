using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class FriendManager : MonoBehaviour
{
    public static FriendManager Instance;

    public List<FriendData> friends = new();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // invite
    public void InviteFriend(FriendData friend)
    {
        if (!friend.isOnline) return;

        LobbyInfo.Instance.AddTestPlayer(friend.DisplayName);
    }

    // remove
    public void RemoveFriend(FriendData friend)
    {
        if (friends.Contains(friend))
            friends.Remove(friend);

    }

    // add
    public void AddFriend(FriendData user)
    {
        if (friends.Exists(f => f.ID == user.ID))
            return;

        friends.Add(user);
    }

    // search
    public FriendData Search(string query)
    {
        return friends.Find(f =>
            f.DisplayName.ToLower().Contains(query.ToLower()) ||
            f.ID == query);
    }
}
