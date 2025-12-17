using UnityEngine;

public class FriendListUI : MonoBehaviour
{
    public Transform ContentParent;
    public GameObject FriendItemPrefab;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        foreach (Transform child in ContentParent) Destroy(child.gameObject);

        foreach (var friend in FriendManager.Instance.friends)
        {
            var item = Instantiate(FriendItemPrefab, ContentParent);
            item.GetComponent<FriendListItemUI>().Setup(friend);
        }
    }
}
