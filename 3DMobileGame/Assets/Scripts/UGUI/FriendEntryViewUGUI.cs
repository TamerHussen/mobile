using System;
using TMPro;
using Unity.Services.Friends.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Services.Samples.Friends.UGUI
{
    public class FriendEntryViewUGUI : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI m_NameText = null;
        [SerializeField] TextMeshProUGUI m_ActivityText = null;
        [SerializeField] Image m_PresenceColorImage = null;

        public Button removeFriendButton = null;
        public Button blockFriendButton = null;
        //public Button inviteFriendButton = null;
        //public Button kickPlayerButton = null;

        public string Id {  get; private set; }
        //public Action<string> onInvite;
        //public Action<string> onKick;


        public void Init(string friendId, string playerName, Availability presenceAvailabilityOptions, string activity)
        {
            this.Id = friendId;
            m_NameText.text = playerName;

            var index = (int)presenceAvailabilityOptions - 1;
            var presenceColor = ColorUtils.GetPresenceColor(index);
            m_PresenceColorImage.color = presenceColor;
            m_ActivityText.text = activity;

            /**
            if (inviteFriendButton != null )
            {
                inviteFriendButton.onClick.RemoveAllListeners();
                inviteFriendButton.onClick.AddListener(()=> onInvite?.Invoke(Id));
            }

            if (kickPlayerButton != null)
            {
                kickPlayerButton.onClick.RemoveAllListeners();
                kickPlayerButton.onClick.AddListener(() => onKick?.Invoke(Id));
            }
            **/
        }
    }
}