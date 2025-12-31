using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
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
        public Action<string> onKick { get; set; }

        public void BindList(List<FriendsEntryData> friendEntryDatas)
        {
            m_FriendsEntryDatas = friendEntryDatas;
        }

        public override void Refresh()
        {
            foreach (var entry in m_FriendEntries)
            {
                if (entry != null && entry.gameObject != null)
                {
                    Destroy(entry.gameObject);
                }
            }
            m_FriendEntries.Clear();

            var lobbyPlayers = LobbyInfo.Instance?.GetPlayers() ?? new List<LobbyPlayer>();

            foreach (var data in m_FriendsEntryDatas)
            {
                var entry = Instantiate(m_FriendEntryViewPrefab, m_ParentTransform);
                entry.Init(data.Id, data.Name, data.Availability, data.Activity);

                bool isInLobby = lobbyPlayers.Exists(p => p.PlayerID == data.Id);

                //entry.onInvite = (id) => onInvite?.Invoke(id);
                //entry.onKick = (id) => onKick?.Invoke(id);

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

                /**
                if (isInLobby)
                {
                    entry.inviteFriendButton.gameObject.SetActive(false);
                    entry.kickPlayerButton.gameObject.SetActive(true);

                    // only host can kick
                    entry.kickPlayerButton.interactable = UnityLobbyManager.Instance.CurrentLobby?.HostId == AuthenticationService.Instance.PlayerId;

                }
                else
                {
                    entry.inviteFriendButton.gameObject.SetActive(true);
                    entry.kickPlayerButton.gameObject.SetActive(false);

                    entry.inviteFriendButton.interactable = data.Availability == Availability.Online;

                }
                **/
                m_FriendEntries.Add(entry);
            }
        }
    }
}