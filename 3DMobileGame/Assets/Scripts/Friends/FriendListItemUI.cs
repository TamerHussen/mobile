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

    private FriendData friend;

    public void Setup(FriendData data)
    {
        friend = data;
        NameText.text = data.DisplayName;

        ProfileImage.color = data.isOnline ? Color.green : Color.grey;
        InviteBtn.interactable = data.isOnline;
    }

    public void Invite()
    {
        FriendManager.Instance.InviteFriend(friend);
    }

    public void Remove()
    {
        FriendManager.Instance.RemoveFriend(friend);

        FindFirstObjectByType<FriendListUI>()?.Refresh();
    }
}
