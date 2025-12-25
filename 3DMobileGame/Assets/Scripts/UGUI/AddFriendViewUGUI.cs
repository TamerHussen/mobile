using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Unity.Services.Samples.Friends.UGUI
{
    public class AddFriendViewUGUI : MonoBehaviour, IAddFriendView
    {
        [SerializeField] Button m_AddFriendButton = null;
        [SerializeField] Button m_CloseButton = null;
        [SerializeField] Button m_BackgroundButton = null;
        [SerializeField] TMP_InputField m_NameInputField = null;
        [SerializeField] TextMeshProUGUI m_RequestResultText = null;
        public Action<string> onFriendRequestSent { get; set; }

        public void Init()
        {
            m_NameInputField.onValueChanged.AddListener((value) => { });

            m_AddFriendButton.onClick.AddListener(() =>
            {
                m_RequestResultText.text = string.Empty;

                string inputName = m_NameInputField.text.Trim();

                if (string.IsNullOrEmpty(inputName))
                {
                    m_RequestResultText.text = "Please enter a username";
                    return;
                }

                if (!inputName.Contains("#"))
                {
                    m_RequestResultText.text = "Please include # tag (e.g. Player#1234)";
                    return;
                }

                Debug.Log($"Sending friend request to: {inputName}");
                onFriendRequestSent?.Invoke(inputName);
            });

            m_BackgroundButton.onClick.AddListener(Hide);
            m_CloseButton.onClick.AddListener(Hide);
            Hide();
        }
        public void FriendRequestSuccess()
        {
            m_RequestResultText.text = "Friend request sent!";
            Hide();
        }

        public void FriendRequestFailed()
        {
            m_RequestResultText.text = "Friend request failed!";
        }

        public void Show()
        {
            m_RequestResultText.text = string.Empty;
            m_NameInputField.SetTextWithoutNotify("");
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
