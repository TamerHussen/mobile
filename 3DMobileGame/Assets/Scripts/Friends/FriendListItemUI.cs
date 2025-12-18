using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Android.Gradle.Manifest;

public class FriendListItemUI : MonoBehaviour
{
    public TextMeshProUGUI NameText;
    public Image ProfileImage;
    public Button InviteBtn;
    public Button RemoveBtn;
    public Button KickBtn;

    private FriendData friend;

    public void Setup(FriendData data)
    {
        friend = data;
        NameText.text = data.DisplayName;

        ProfileImage.color = data.isOnline ? Color.green : Color.grey;

        bool IsInLobby = LobbyInfo.Instance.GetPlayers()
            .Exists(p => p.PlayerID == friend.DisplayName);

        InviteBtn.interactable = data.isOnline && !IsInLobby;
        KickBtn.interactable = IsInLobby;

    }

    public void Invite()
    {
        if (LobbyInfo.Instance.GetPlayers().Exists(p => p.PlayerID == friend.DisplayName)) return;

        FriendManager.Instance.InviteFriend(friend);

        KickBtn.interactable = true;
        InviteBtn.interactable = false;
    }

    public void Kick()
    {
        LobbyInfo.Instance.RemoveTestPlayer(friend.DisplayName);

        Debug.Log($"{friend.DisplayName} has been kicked.");

        KickBtn.interactable = false;
        InviteBtn.interactable = friend.isOnline;

    }

    public void Remove()
    {
        LobbyInfo.Instance.RemoveTestPlayer(friend.DisplayName);

        FriendManager.Instance.RemoveFriend(friend);

        FindFirstObjectByType<FriendListUI>()?.Refresh();
    }
}
