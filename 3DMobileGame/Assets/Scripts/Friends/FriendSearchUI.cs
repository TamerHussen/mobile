using UnityEngine;
using TMPro;

public class FriendSearchUI : MonoBehaviour
{
    public TMP_InputField SearchInput;
    public FriendListItemUI ResultDisplay;

    private FriendData CurrentResult;

    public void Search()
    {
        CurrentResult = FakeFriendData.Find(SearchInput.text);

        if (CurrentResult != null )
        {
            ResultDisplay.gameObject.SetActive(true);
            ResultDisplay.Setup(CurrentResult);
        }
        else
        {
            ResultDisplay.gameObject.SetActive(false);
        }
    }

    public void AddFriend()
    {
        if (CurrentResult == null) return;

        FriendManager.Instance.AddFriend(CurrentResult);
        FindFirstObjectByType<FriendListUI>()?.Refresh();
    }
}
