using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Friends.Exceptions;
using Unity.Services.Friends.Models;
using Unity.Services.Friends.Notifications;
using Unity.Services.Samples.Friends.UGUI;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace Unity.Services.Samples.Friends
{
    public class RelationshipsManager : MonoBehaviour
    {
        // REMOVED: No longer using Inspector reference
        // [Tooltip("Reference a GameObject that has a component extending from IRelationshipsUIController."), SerializeField]
        // GameObject m_RelationshipsViewGameObject;

        GameObject m_RelationshipsViewGameObject; // Found dynamically
        IRelationshipsView m_RelationshipsView;

        List<FriendsEntryData> m_FriendsEntryDatas = new List<FriendsEntryData>();
        List<PlayerProfile> m_RequestsEntryDatas = new List<PlayerProfile>();
        List<PlayerProfile> m_BlockEntryDatas = new List<PlayerProfile>();

        ILocalPlayerView m_LocalPlayerView;
        IAddFriendView m_AddFriendView;
        IFriendsListView m_FriendsListView;
        IRequestListView m_RequestListView;
        IBlockedListView m_BlockListView;

        PlayerProfile m_LoggedPlayerProfile;

        private FriendsEventConnectionState m_current_state;
        private bool m_IsInitialized = false;

        // Track which scenes should have friends UI
        private readonly string[] scenesWithFriendsUI = { "MainMenu", "Lobby" };

        void Awake()
        {
            // Ensure only one instance exists
            var existing = FindObjectsByType<RelationshipsManager>(FindObjectsSortMode.None);
            if (existing.Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        async void Start()
        {
#if UNITY_EDITOR
            await UnityServiceAuthenticator.SignIn("EditorPlayer_" + Random.Range(1, 9999));
#else
            await UnityServiceAuthenticator.SignIn();
#endif
            await Init();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"Scene loaded: {scene.name}, IsInitialized: {m_IsInitialized}");

            bool shouldHaveUI = System.Array.Exists(scenesWithFriendsUI, s => s == scene.name);

            if (shouldHaveUI && m_IsInitialized)
            {
                Debug.Log("Scene has friends UI, rebinding with delay...");
                StartCoroutine(DelayedRebindUI());
            }
            else if (shouldHaveUI && !m_IsInitialized)
            {
                Debug.Log("Scene has friends UI but not initialized yet, initializing UI with delay...");
                StartCoroutine(DelayedUIInit());
            }
            else
            {
                Debug.Log($"Scene {scene.name} doesn't need friends UI, clearing references");
                ClearUIReferences();
            }
        }

        IEnumerator DelayedRebindUI()
        {
            yield return new WaitForEndOfFrame();
            yield return null; // Wait one more frame to be safe

            RebindUI();
        }

        IEnumerator DelayedUIInit()
        {
            yield return new WaitForEndOfFrame();
            yield return null;

            UIInit();
        }

        async Task Init()
        {
            if (m_IsInitialized)
            {
                Debug.LogWarning("RelationshipsManager already initialized");
                return;
            }

            Debug.Log("Initializing RelationshipsManager...");

            RegisterFriendsEventCallbacks();
            await FriendsService.Instance.InitializeAsync();
            RegisterMessageReceived();

            var presenceListener = FindFirstObjectByType<LobbyPresenceListener>();
            presenceListener?.Initialize();

            UIInit();
            await LogInAsync();
            RefreshAll();

            m_IsInitialized = true;
            Debug.Log("✅ RelationshipsManager initialized");
        }

        void UIInit()
        {
            Debug.Log("Initializing UI...");

            var viewObjects = FindObjectsByType<RelationshipsView>(FindObjectsSortMode.None);

            Debug.Log($"Found {viewObjects.Length} RelationshipsView components in scene");

            if (viewObjects.Length > 0)
            {
                Debug.Log($"RelationshipsView found on GameObject: '{viewObjects[0].gameObject.name}' at path: {GetGameObjectPath(viewObjects[0].gameObject)}");
            }

            if (viewObjects.Length == 0)
            {
                Debug.LogWarning($"RelationshipsView not found in current scene.");
                return;
            }


            m_RelationshipsViewGameObject = viewObjects[0].gameObject;
            m_RelationshipsView = viewObjects[0];

            if (m_RelationshipsView == null)
            {
                Debug.LogError($"RelationshipsView component not found");
                return;
            }

            m_RelationshipsView.Init();
            BindUIReferences();
        }
        string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = obj.name + "/" + path;
            }
            return path;
        }

        void BindUIReferences()
        {
            if (m_RelationshipsView == null)
            {
                Debug.LogError("Cannot bind UI: m_RelationshipsView is null");
                return;
            }

            Debug.Log("Binding UI references...");

            m_LocalPlayerView = m_RelationshipsView.LocalPlayerView;
            m_AddFriendView = m_RelationshipsView.AddFriendView;

            // Bind Lists
            m_FriendsListView = m_RelationshipsView.FriendsListView;
            m_FriendsListView?.BindList(m_FriendsEntryDatas);

            m_RequestListView = m_RelationshipsView.RequestListView;
            m_RequestListView?.BindList(m_RequestsEntryDatas);

            m_BlockListView = m_RelationshipsView.BlockListView;
            m_BlockListView?.BindList(m_BlockEntryDatas);

            // Unbind old callbacks to prevent duplicates
            UnbindCallbacks();

            // Bind Friends SDK Callbacks
            if (m_AddFriendView != null)
                m_AddFriendView.onFriendRequestSent += AddFriendAsync;

            if (m_FriendsListView != null)
            {
                m_FriendsListView.onRemove += RemoveFriendAsync;
                m_FriendsListView.onBlock += BlockFriendAsync;
                m_FriendsListView.onInvite += (friendId) =>
                {
                    Debug.Log($"Invite button clicked for friend: {friendId}");
                    var bridge = FindFirstObjectByType<FriendsLobbyBridge>();
                    if (bridge != null)
                    {
                        bridge.InviteFriendToLobby(friendId);
                    }
                    else
                    {
                        Debug.LogError("FriendsLobbyBridge not found in scene!");
                    }
                };
                m_FriendsListView.onKick += (targetId) =>
                {
                    LobbyInfo.Instance?.KickPlayer(targetId);
                };
            }

            if (m_RequestListView != null)
            {
                m_RequestListView.onAccept += AcceptRequestAsync;
                m_RequestListView.onDecline += DeclineRequestAsync;
                m_RequestListView.onBlock += BlockFriendAsync;
            }

            if (m_BlockListView != null)
                m_BlockListView.onUnblock += UnblockFriendAsync;

            if (m_LocalPlayerView != null)
                m_LocalPlayerView.onPresenceChanged += SetPresenceAsync;

            Debug.Log("✅ UI references bound");
        }

        void UnbindCallbacks()
        {
            if (m_AddFriendView != null)
                m_AddFriendView.onFriendRequestSent -= AddFriendAsync;

            if (m_FriendsListView != null)
            {
                m_FriendsListView.onRemove -= RemoveFriendAsync;
                m_FriendsListView.onBlock -= BlockFriendAsync;
            }

            if (m_RequestListView != null)
            {
                m_RequestListView.onAccept -= AcceptRequestAsync;
                m_RequestListView.onDecline -= DeclineRequestAsync;
                m_RequestListView.onBlock -= BlockFriendAsync;
            }

            if (m_BlockListView != null)
                m_BlockListView.onUnblock -= UnblockFriendAsync;

            if (m_LocalPlayerView != null)
                m_LocalPlayerView.onPresenceChanged -= SetPresenceAsync;
        }

        void ClearUIReferences()
        {
            Debug.Log("Clearing UI references (no friends UI in this scene)");

            UnbindCallbacks();

            m_RelationshipsViewGameObject = null;
            m_RelationshipsView = null;
            m_LocalPlayerView = null;
            m_AddFriendView = null;
            m_FriendsListView = null;
            m_RequestListView = null;
            m_BlockListView = null;
        }

        void RebindUI()
        {
            Debug.Log("Rebinding UI after scene reload...");

            var viewObjects = FindObjectsByType<RelationshipsView>(FindObjectsSortMode.None);

            if (viewObjects.Length == 0)
            {
                Debug.LogError("RelationshipsView not found in scene after reload!");
                ClearUIReferences();
                return;
            }

            m_RelationshipsViewGameObject = viewObjects[0].gameObject;
            m_RelationshipsView = viewObjects[0];

            if (m_RelationshipsView == null)
            {
                Debug.LogError("Failed to get IRelationshipsView after scene reload");
                return;
            }

            // Re-initialize the view
            m_RelationshipsView.Init();

            // Re-bind all references
            BindUIReferences();

            // Refresh all data to repopulate UI
            RefreshAll();

            // Refresh local player display
            if (m_LocalPlayerView != null && m_LoggedPlayerProfile != null)
            {
                if (m_LocalPlayerView is LocalPlayerViewUGUI localPlayerView)
                {
                    localPlayerView.RefreshFromSaveManager();
                }
            }

            Debug.Log("✅ RelationshipsManager UI re-bound after scene reload");
        }

        async Task LogInAsync()
        {
            var playerID = AuthenticationService.Instance.PlayerId;

            string displayName;
            string uniqueName;

            if (SaveManager.Instance?.data != null)
            {
                displayName = SaveManager.Instance.data.playerName;
                uniqueName = SaveManager.Instance.data.uniquePlayerName;

                if (string.IsNullOrEmpty(uniqueName))
                {
                    uniqueName = await AuthenticationService.Instance.GetPlayerNameAsync();
                    displayName = uniqueName.Contains("#") ? uniqueName.Split('#')[0] : uniqueName;

                    SaveManager.Instance.data.playerName = displayName;
                    SaveManager.Instance.data.uniquePlayerName = uniqueName;
                    SaveManager.Instance.Save();
                }

                Debug.Log($"Using SaveManager - Display: {displayName}, Unique: {uniqueName}");

                try
                {
                    await AuthenticationService.Instance.UpdatePlayerNameAsync(uniqueName);
                    Debug.Log($"Synced to Unity Authentication: {uniqueName}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Could not update Authentication name: {e.Message}");
                }
            }
            else
            {
                uniqueName = await AuthenticationService.Instance.GetPlayerNameAsync();

                if (uniqueName.Contains("#"))
                {
                    displayName = uniqueName.Split('#')[0];
                }
                else
                {
                    displayName = uniqueName;
                }

                Debug.Log($"Using Auth name - Display: {displayName}, Unique: {uniqueName}");

                if (SaveManager.Instance?.data != null)
                {
                    SaveManager.Instance.data.playerName = displayName;
                    SaveManager.Instance.data.uniquePlayerName = uniqueName;
                    SaveManager.Instance.Save();
                }
            }

            m_LoggedPlayerProfile = new PlayerProfile(displayName, playerID);

            await SetPresence(Availability.Online, "In Friends Menu");

            if (m_LocalPlayerView != null)
            {
                m_LocalPlayerView.Refresh(
                    displayName,
                    "In Friends Menu",
                    Availability.Online);
            }

            RefreshAll();
            Debug.Log($"Logged in as {displayName} (unique: {uniqueName})");
        }

        [System.Serializable]
        public class InviteMessage
        {
            public string LobbyCode;
        }

        void RegisterMessageReceived()
        {
            FriendsService.Instance.MessageReceived += OnInviteReceived;
        }

        private void OnInviteReceived(IMessageReceivedEvent args)
        {
            var data = args.GetAs<InviteMessage>();
            string joinCode = data.LobbyCode;
            string senderId = args.UserId;

            Debug.Log($"Received lobby invite {joinCode} from {senderId}");
            InvitePopupUI.Instance?.Show(senderId, joinCode);
        }

        public void RefreshAll()
        {
            // Safety check before refreshing
            if (m_RelationshipsView == null || m_FriendsListView == null)
            {
                Debug.LogWarning("Cannot refresh: UI references are null (probably not in a scene with friends UI)");
                return;
            }

            RefreshFriends();
            RefreshRequests();
            RefreshBlocks();

            // Refresh local player view
            if (m_LocalPlayerView is LocalPlayerViewUGUI localPlayerView)
            {
                localPlayerView.RefreshFromSaveManager();
            }

            Debug.Log("✅ Friends data refreshed");
        }

        // ... rest of your methods remain the same ...
        // (All the async methods, RefreshFriends, RefreshRequests, etc.)

        async void BlockFriendAsync(string id)
        {
            await BlockFriend(id);
            RefreshAll();
        }

        async void UnblockFriendAsync(string id)
        {
            await UnblockFriend(id);
            RefreshBlocks();
            RefreshFriends();
        }

        async void RemoveFriendAsync(string id)
        {
            await RemoveFriend(id);
            RefreshFriends();
        }

        async void AcceptRequestAsync(string name)
        {
            await AcceptRequest(name);
            RefreshRequests();
            RefreshFriends();
        }

        async void DeclineRequestAsync(string id)
        {
            await DeclineRequest(id);
            RefreshRequests();
        }

        async void SetPresenceAsync((Availability presence, string activity) status)
        {
            await SetPresence(status.presence, status.activity);

            if (m_LocalPlayerView != null && m_LoggedPlayerProfile != null)
            {
                m_LocalPlayerView.Refresh(m_LoggedPlayerProfile.Name, status.activity, status.presence);
            }
        }

        async void AddFriendAsync(string name)
        {
            var success = await SendFriendRequest(name);
            if (success)
            {
                m_AddFriendView?.FriendRequestSuccess();
                if (m_RequestsEntryDatas.Find(entry => entry.Name == name) != null)
                    RefreshAll();
            }
            else
            {
                m_AddFriendView?.FriendRequestFailed();
            }
        }

        public void RefreshFriends()
        {
            if (m_FriendsListView == null || m_RelationshipsView == null)
            {
                Debug.LogWarning("Cannot refresh friends: UI references are null");
                return;
            }

            m_FriendsEntryDatas.Clear();

            var friends = GetFriends();

            foreach (var friend in friends)
            {
                string activityText;
                if (friend.Presence.Availability == Availability.Offline ||
                    friend.Presence.Availability == Availability.Invisible)
                {
                    activityText = friend.Presence.LastSeen.ToShortDateString() + " " +
                                   friend.Presence.LastSeen.ToLongTimeString();
                }
                else
                {
                    activityText = friend.Presence.GetActivity<Activity>() == null
                        ? ""
                        : friend.Presence.GetActivity<Activity>().Status;
                }

                string displayName = friend.Profile.Name;
                if (displayName.Contains("#"))
                {
                    displayName = displayName.Split('#')[0];
                }

                var info = new FriendsEntryData
                {
                    Name = displayName,
                    Id = friend.Id,
                    Availability = friend.Presence.Availability,
                    Activity = activityText
                };
                m_FriendsEntryDatas.Add(info);
            }

            m_RelationshipsView.RelationshipBarView.Refresh();
        }

        void RefreshRequests()
        {
            if (m_RequestListView == null || m_RelationshipsView == null)
            {
                Debug.LogWarning("Cannot refresh requests: UI references are null");
                return;
            }

            m_RequestsEntryDatas.Clear();
            var requests = GetRequests();

            foreach (var request in requests)
            {
                string fullUniqueName = request.Profile.Name;
                m_RequestsEntryDatas.Add(new PlayerProfile(fullUniqueName, request.Id));
            }

            m_RelationshipsView.RelationshipBarView.Refresh();
        }

        void RefreshBlocks()
        {
            if (m_BlockListView == null || m_RelationshipsView == null)
            {
                Debug.LogWarning("Cannot refresh blocks: UI references are null");
                return;
            }

            m_BlockEntryDatas.Clear();

            foreach (var block in FriendsService.Instance.Blocks)
            {
                string displayName = block.Member.Profile.Name;
                if (displayName.Contains("#"))
                {
                    displayName = displayName.Split('#')[0];
                }

                m_BlockEntryDatas.Add(new PlayerProfile(displayName, block.Member.Id));
            }

            m_RelationshipsView.RelationshipBarView.Refresh();
        }

        async Task<bool> SendFriendRequest(string playerName)
        {
            try
            {
                var relationship = await FriendsService.Instance.AddFriendByNameAsync(playerName);
                Debug.Log($"Friend request sent to {playerName}.");
                return relationship.Type is RelationshipType.FriendRequest or RelationshipType.Friend;
            }
            catch (FriendsServiceException e)
            {
                Debug.Log($"Failed to Request {playerName} - {e}.");
                return false;
            }
        }

        async Task RemoveFriend(string playerId)
        {
            try
            {
                await FriendsService.Instance.DeleteFriendAsync(playerId);
                Debug.Log($"{playerId} was removed from the friends list.");
            }
            catch (FriendsServiceException e)
            {
                Debug.Log($"Failed to remove {playerId}. - {e}");
            }
        }

        async Task BlockFriend(string playerId)
        {
            try
            {
                await FriendsService.Instance.AddBlockAsync(playerId);
                Debug.Log($"{playerId} was blocked.");
            }
            catch (FriendsServiceException e)
            {
                Debug.Log($"Failed to block {playerId}. - {e}");
            }
        }

        async Task UnblockFriend(string playerId)
        {
            try
            {
                await FriendsService.Instance.DeleteBlockAsync(playerId);
                Debug.Log($"{playerId} was unblocked.");
            }
            catch (FriendsServiceException e)
            {
                Debug.Log($"Failed to unblock {playerId} - {e}.");
            }
        }

        async Task AcceptRequest(string playerName)
        {
            try
            {
                await FriendsService.Instance.AddFriendByNameAsync(playerName);
                Debug.Log($"Friend request from {playerName} was accepted.");
            }
            catch (FriendsServiceException e)
            {
                Debug.LogError($"Failed to accept request from {playerName}. Error: {e.Message}, Code: {e.ErrorCode}");
            }
        }

        async Task DeclineRequest(string playerId)
        {
            try
            {
                await FriendsService.Instance.DeleteIncomingFriendRequestAsync(playerId);
                Debug.Log($"Friend request from {playerId} was declined.");
            }
            catch (FriendsServiceException e)
            {
                Debug.Log($"Failed to decline request from {playerId}. - {e}");
            }
        }

        List<Member> GetFriends()
        {
            return GetNonBlockedMembers(FriendsService.Instance.Friends);
        }

        List<Member> GetRequests()
        {
            return GetNonBlockedMembers(FriendsService.Instance.IncomingFriendRequests);
        }

        async Task SetPresence(Availability presenceAvailabilityOptions, string activityStatus = "")
        {
            var activity = new Activity { Status = activityStatus };
            try
            {
                await FriendsService.Instance.SetPresenceAsync(presenceAvailabilityOptions, activity);
                Debug.Log($"Availability changed to {presenceAvailabilityOptions}.");
            }
            catch (FriendsServiceException e)
            {
                Debug.Log($"Failed to set the presence to {presenceAvailabilityOptions} - {e}");
            }
        }

        void RegisterFriendsEventCallbacks()
        {
            try
            {
                FriendsService.Instance.RelationshipAdded += e =>
                {
                    RefreshRequests();
                    RefreshFriends();
                    Debug.Log($"create {e.Relationship} EventReceived");
                };
                FriendsService.Instance.MessageReceived += e =>
                {
                    RefreshRequests();
                    Debug.Log("MessageReceived EventReceived");
                };
                FriendsService.Instance.PresenceUpdated += e =>
                {
                    RefreshFriends();
                    Debug.Log("PresenceUpdated EventReceived");
                };
                FriendsService.Instance.RelationshipDeleted += e =>
                {
                    RefreshFriends();
                    Debug.Log($"Delete {e.Relationship} EventReceived");
                };
                FriendsService.Instance.NotificationsConnectivityChanged += e =>
                {
                    if (m_current_state == FriendsEventConnectionState.Subscribed && m_current_state != e.State)
                    {
                        if (m_LocalPlayerView != null && m_LoggedPlayerProfile != null)
                        {
                            m_LocalPlayerView.Refresh(m_LoggedPlayerProfile.Name,
                                "Connectivity Problems",
                                Availability.Offline);
                        }
                    }

                    if (m_current_state == FriendsEventConnectionState.Unsynced &&
                        e.State == FriendsEventConnectionState.Subscribed)
                    {
                        SetPresenceAsync((Availability.Online, "Back Online"));
                    }
                    Debug.Log($"Change of state in notification system from {m_current_state} to {e.State}");
                    m_current_state = e.State;
                };
            }
            catch (FriendsServiceException e)
            {
                Debug.Log(
                    "An error occurred while performing the action. HttpCode: " + e.StatusCode + ", FriendsErrorCode: " + e.ErrorCode + ", Message: " + e.Message);
            }
        }

        public void RefreshLocalPlayerName()
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.LogWarning("Cannot refresh: Not signed in");
                return;
            }

            var name = AuthenticationService.Instance.PlayerName;
            var displayName = name.Contains("#") ? name.Split('#')[0] : name;

            m_LoggedPlayerProfile = new PlayerProfile(displayName, AuthenticationService.Instance.PlayerId);

            if (m_LocalPlayerView is LocalPlayerViewUGUI localPlayerView)
            {
                localPlayerView.RefreshFromSaveManager();
            }

            Debug.Log($"✅ Local player name refreshed: {displayName}");
        }

        public async void SetGameActivity(string activity, Availability availability)
        {
            await FriendsService.Instance.SetPresenceAsync(
                availability,
                new Activity { Status = activity }
            );

            if (m_LocalPlayerView != null && m_LoggedPlayerProfile != null)
            {
                m_LocalPlayerView.Refresh(
                    m_LoggedPlayerProfile.Name,
                    activity,
                    availability
                );
            }
        }

        public async Task EnsureFriendsConnection()
        {
            try
            {
                if (FriendsService.Instance == null)
                {
                    Debug.LogWarning("Friends Service is null, reinitializing...");
                    await FriendsService.Instance.InitializeAsync();
                }

                await SetPresence(Availability.Online, "In Lobby");
                RefreshAll();

                Debug.Log("✅ Friends connection verified");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to ensure Friends connection: {e.Message}");
            }
        }

        private List<Member> GetNonBlockedMembers(IReadOnlyList<Relationship> relationships)
        {
            var blocks = FriendsService.Instance.Blocks;
            return relationships
                   .Where(relationship =>
                       !blocks.Any(blockedRelationship => blockedRelationship.Member.Id == relationship.Member.Id))
                   .Select(relationship => relationship.Member)
                   .ToList();
        }
    }
}