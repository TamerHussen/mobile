using System;
using System.Collections.Generic;
using Unity.Services.Friends.Models;
using UnityEngine;

namespace Unity.Services.Samples.Friends.UGUI
{
    public class FriendsViewUGUI : ListViewUGUI, IFriendsListView
    {
        [SerializeField] RectTransform m_ParentTransform = null;
        [SerializeField] FriendEntryViewUGUI m_FriendEntryViewPrefab = null;

        List<FriendEntryViewUGUI> m_FriendEntries = new List<FriendEntryViewUGUI>();
        List<FriendsEntryData> m_FriendsEntryDatas = new List<FriendsEntryData>();

        public Action<string> onRemove { get; set; }
        public Action<string> onBlock { get; set; }
        public Action<string> onInvite { get; set; }

        public void BindList(List<FriendsEntryData> friendEntryDatas)
        {
            m_FriendsEntryDatas = friendEntryDatas;
        }

        public override void Refresh()
        {
            m_FriendEntries.ForEach(entry => Destroy(entry.gameObject));
            m_FriendEntries.Clear();

            foreach (var data in m_FriendsEntryDatas)
            {
                var entry = Instantiate(m_FriendEntryViewPrefab, m_ParentTransform);
                entry.Init(data.Name, data.Availability, data.Activity);
                entry.removeFriendButton.onClick.AddListener(() =>
                {
                    onRemove?.Invoke(data.Id);
                    entry.gameObject.SetActive(false);
                });
                entry.blockFriendButton.onClick.AddListener(() =>
                {
                    onBlock?.Invoke(data.Id);
                    entry.gameObject.SetActive(false);
                });
                entry.inviteFriendButton.onClick.AddListener(() =>
                {
                    onInvite?.Invoke(data.Id);
                });
                entry.inviteFriendButton.interactable =
                    data.Availability == Availability.Online;

                m_FriendEntries.Add(entry);
            }
        }
    }
}