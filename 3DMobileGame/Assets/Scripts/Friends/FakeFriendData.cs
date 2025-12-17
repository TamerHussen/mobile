using UnityEngine;
using System.Collections.Generic;

public static class FakeFriendData
{
    // fake user database
    public static List<FriendData> users = new()
    {
        new FriendData { ID = "1", DisplayName = "TimothyThe1ST", isOnline = true },
        new FriendData { ID = "2", DisplayName = "FartSmella_SmartFella", isOnline = false },
        new FriendData { ID = "3", DisplayName = "MJ_PJ_DJ", isOnline = true },
        new FriendData { ID = "4", DisplayName = "Bob", isOnline = true },
        new FriendData { ID = "5", DisplayName = "Dave", isOnline = true },
        new FriendData { ID = "6", DisplayName = "Eve", isOnline = false }

    };

    public static FriendData Find(string query)
    {
        return users.Find(u =>
            u.DisplayName.ToLower().Contains(query.ToLower()) ||
            u.ID == query);
    }
}
