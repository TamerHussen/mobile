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

        [SerializeField] TextMeshProUGUI m_NameText = null;
        [SerializeField] TMP_InputField m_Activity = null;
        [SerializeField] TMP_Dropdown m_PresenceSelector = null;
        [SerializeField] Image m_PresenceColor = null;
        [SerializeField] Button m_CopyButton = null;

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
            m_CopyButton.onClick.AddListener(() => { GUIUtility.systemCopyBuffer = m_NameText.text; });
        }

        void Start()
        {
            // CRITICAL FIX: Display SaveManager name, not Authentication name
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
            // CRITICAL FIX: If name is Unity auth name (contains #), use SaveManager name instead
            if (name.Contains("#") && SaveManager.Instance?.data != null)
            {
                name = SaveManager.Instance.data.playerName;
                Debug.Log($"Using SaveManager name instead: {name}");
            }

            m_NameText.text = name;

            //Presence
            var index = (int)availability - 1;
            m_PresenceSelector.SetValueWithoutNotify(index);
            var presenceColor = ColorUtils.GetPresenceColor(index);
            m_PresenceColor.color = presenceColor;

            m_Activity.text = activity;
        }

        // NEW METHOD: Refresh display from SaveManager
        public void RefreshFromSaveManager()
        {
            if (SaveManager.Instance?.data != null)
            {
                m_NameText.text = SaveManager.Instance.data.playerName;
                Debug.Log($"LocalPlayerView refreshed with SaveManager name: {SaveManager.Instance.data.playerName}");
            }
        }

        public async void OnPlayerSettingsChanged(string newName, string newCosmetic)
        {
            SaveManager.Instance.data.playerName = newName;
            SaveManager.Instance.data.selectedCosmetic = newCosmetic;
            SaveManager.Instance.Save();

            try
            {
                await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
                Debug.Log($"Updated Unity Authentication PlayerName to: {newName}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to update Authentication PlayerName: {e.Message}");
            }

            m_NameText.text = newName;

            if (UnityLobbyManager.Instance?.CurrentLobby != null)
            {
                await UnityLobbyManager.Instance.SyncSaveDataToLobby();
            }

            var relationshipsManager = FindFirstObjectByType<Unity.Services.Samples.Friends.RelationshipsManager>();
            relationshipsManager?.RefreshLocalPlayerName();
        }
    }
}