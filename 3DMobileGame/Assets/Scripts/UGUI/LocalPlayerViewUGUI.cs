using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Friends.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Services.Samples.Friends.UGUI
{
    public class LocalPlayerViewUGUI : MonoBehaviour, ILocalPlayerView
    {
        public Action<(Availability, string)> onPresenceChanged { get; set; }

        [Header("Player Info")]
        [SerializeField] TextMeshProUGUI m_NameText = null;
        [SerializeField] TextMeshProUGUI m_UniqueIdText = null;
        [SerializeField] Button m_CopyButton = null;

        [Header("Presence")]
        [SerializeField] TMP_InputField m_Activity = null;
        [SerializeField] TMP_Dropdown m_PresenceSelector = null;
        [SerializeField] Image m_PresenceColor = null;

        [Header("Copy Feedback")]
        [SerializeField] TextMeshProUGUI m_CopiedFeedback = null;

        void Awake()
        {
            var names = new List<string>
            {
                "Online",
                "Busy",
                "Away",
                "Invisible"
            };

            m_PresenceSelector.AddOptions(names);
            m_PresenceSelector.onValueChanged.AddListener((value) => { OnStatusChanged(value, m_Activity.text); });
            m_Activity.onEndEdit.AddListener((value) => { OnStatusChanged(m_PresenceSelector.value, value); });

            m_CopyButton.onClick.AddListener(CopyUniqueIdToClipboard);

            // Hide feedback initially
            if (m_CopiedFeedback != null)
            {
                m_CopiedFeedback.gameObject.SetActive(false);
            }
        }

        void Start()
        {
            RefreshFromSaveManager();
        }

        void OnStatusChanged(int value, string activity)
        {
            var presence = (Availability)Enum.Parse(typeof(Availability),
                m_PresenceSelector.options[value].text, true);

            onPresenceChanged?.Invoke((presence, activity));
        }

        public void Refresh(string name, string activity, Availability availability)
        {
            if (name.Contains("#"))
            {
                name = name.Split('#')[0];
            }

            m_NameText.text = name;

            RefreshUniqueId();

            var index = (int)availability - 1;
            m_PresenceSelector.SetValueWithoutNotify(index);
            var presenceColor = ColorUtils.GetPresenceColor(index);
            m_PresenceColor.color = presenceColor;

            m_Activity.text = activity;
        }

        public void RefreshFromSaveManager()
        {
            if (SaveManager.Instance?.data != null)
            {
                string displayName = SaveManager.Instance.data.playerName;
                if (displayName.Contains("#"))
                {
                    displayName = displayName.Split('#')[0];
                }
                m_NameText.text = displayName;

                RefreshUniqueId();

                Debug.Log($"LocalPlayerView refreshed - Display: {displayName}");
            }
        }

        private void RefreshUniqueId()
        {
            if (m_UniqueIdText == null) return;

            string uniqueId = "";

            // Try to get from SaveManager first
            if (SaveManager.Instance?.data != null && !string.IsNullOrEmpty(SaveManager.Instance.data.uniquePlayerName))
            {
                uniqueId = SaveManager.Instance.data.uniquePlayerName;
            }
            // Fallback to Authentication service
            else if (AuthenticationService.Instance.IsSignedIn)
            {
                uniqueId = AuthenticationService.Instance.PlayerName;
            }

            m_UniqueIdText.text = uniqueId;
        }

        public void CopyUniqueIdToClipboard()
        {
            string uniqueId = "";

            if (SaveManager.Instance?.data != null && !string.IsNullOrEmpty(SaveManager.Instance.data.uniquePlayerName))
            {
                uniqueId = SaveManager.Instance.data.uniquePlayerName;
            }
            else if (AuthenticationService.Instance.IsSignedIn)
            {
                uniqueId = AuthenticationService.Instance.PlayerName;
            }

            if (string.IsNullOrEmpty(uniqueId))
            {
                Debug.LogWarning("No unique ID to copy!");
                return;
            }

            GUIUtility.systemCopyBuffer = uniqueId;
            Debug.Log($"Copied to clipboard: {uniqueId}");

            ShowCopyFeedback();
        }

        private void ShowCopyFeedback()
        {
            if (m_CopiedFeedback != null)
            {
                m_CopiedFeedback.gameObject.SetActive(true);
                m_CopiedFeedback.text = "Copied!";

                // Hide after 2 seconds
                CancelInvoke(nameof(HideCopyFeedback));
                Invoke(nameof(HideCopyFeedback), 2f);
            }
        }

        private void HideCopyFeedback()
        {
            if (m_CopiedFeedback != null)
            {
                m_CopiedFeedback.gameObject.SetActive(false);
            }
        }

        public async void OnPlayerSettingsChanged(string newName, string newCosmetic)
        {
            // Generate unique name with # suffix
            string playerId = AuthenticationService.Instance.PlayerId;
            string uniqueSuffix = playerId.Substring(playerId.Length - 4);
            string uniqueName = $"{newName}#{uniqueSuffix}";

            // Update SaveManager with both names
            SaveManager.Instance.data.playerName = newName;
            SaveManager.Instance.data.uniquePlayerName = uniqueName;
            SaveManager.Instance.data.selectedCosmetic = newCosmetic;
            SaveManager.Instance.Save();

            try
            {
                // Update Unity Authentication with unique name
                await AuthenticationService.Instance.UpdatePlayerNameAsync(uniqueName);
                Debug.Log($"Updated Unity Authentication PlayerName to: {uniqueName}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to update Authentication PlayerName: {e.Message}");
            }

            // Update display
            m_NameText.text = newName;
            RefreshUniqueId();

            // Sync to lobby with display name
            if (UnityLobbyManager.Instance?.CurrentLobby != null)
            {
                await UnityLobbyManager.Instance.UpdatePlayerDataAsync(newName, newCosmetic);
            }

            // Refresh friends manager
            var relationshipsManager = FindFirstObjectByType<Unity.Services.Samples.Friends.RelationshipsManager>();
            relationshipsManager?.RefreshLocalPlayerName();
        }
    }
}