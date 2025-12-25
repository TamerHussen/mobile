using System.Text.RegularExpressions;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Samples.Friends;
using UnityEngine;

public class UsernameChanger : MonoBehaviour
{
    public TMP_InputField input;
    public TextMeshProUGUI feedbackText;
    public GameObject rootPanel;

    const int MinLength = 3;
    const int MaxLength = 16;

    // Letters, numbers, underscore
    static readonly Regex ValidNameRegex = new Regex(@"^[A-Za-z0-9_]+$");

    void OnEnable()
    {
        if (SaveManager.Instance?.data != null)
        {
            string currentName = SaveManager.Instance.data.playerName;
            // Remove the #xxxx part to show only the display name
            if (currentName.Contains("#"))
            {
                currentName = currentName.Split('#')[0];
            }
            input.text = currentName;
        }
    }

    public async void ChangeName()
    {
        feedbackText.text = "";

        string displayName = input.text.Trim();

        // Validation
        if (string.IsNullOrEmpty(displayName))
        {
            feedbackText.text = "Name cannot be empty";
            return;
        }

        if (displayName.Length < MinLength)
        {
            feedbackText.text = $"Name must be at least {MinLength} characters";
            return;
        }

        if (displayName.Length > MaxLength)
        {
            feedbackText.text = $"Name must be at most {MaxLength} characters";
            return;
        }

        if (!ValidNameRegex.IsMatch(displayName))
        {
            feedbackText.text = "Only letters, numbers, and _ are allowed";
            return;
        }

        try
        {
            feedbackText.text = "Updating name...";

            await AuthenticationService.Instance.UpdatePlayerNameAsync(displayName);

            string actualUniqueName = await AuthenticationService.Instance.GetPlayerNameAsync();

            Debug.Log($"Unity assigned unique name: {actualUniqueName}");

            if (SaveManager.Instance?.data != null)
            {
                SaveManager.Instance.data.playerName = displayName;
                SaveManager.Instance.data.uniquePlayerName = actualUniqueName;
                SaveManager.Instance.Save();
            }

            // Sync to lobby
            if (UnityLobbyManager.Instance?.CurrentLobby != null)
            {
                await UnityLobbyManager.Instance.UpdatePlayerDataAsync(displayName, SaveManager.Instance.data.selectedCosmetic);
            }

            var relationshipsManager = FindFirstObjectByType<RelationshipsManager>();
            if (relationshipsManager != null)
            {
                // Refresh local player name
                relationshipsManager.RefreshLocalPlayerName();

                await FriendsService.Instance.SetPresenceAsync(
                    Availability.Online,
                    new Activity { Status = "In Lobby" }
                );

                // Refresh friends list
                relationshipsManager.RefreshFriends();
            }

            if (LobbyInfo.Instance != null)
            {
                LobbyInfo.Instance.UpdateHostName(displayName);
                LobbyInfo.Instance.UpdatePlayerName(AuthenticationService.Instance.PlayerId, displayName);
            }

            LobbyPlayerSpawner.Instance?.SpawnPlayers();

            feedbackText.text = $"Name changed to: {displayName}";
            Debug.Log($"✅ Username changed - Display: '{displayName}', Unique: '{actualUniqueName}'");

            Invoke(nameof(Close), 1f);
        }
        catch (AuthenticationException e)
        {
            if (e.ErrorCode == 10012 || e.Message.Contains("name") || e.Message.Contains("taken"))
            {
                feedbackText.text = "Name unavailable, try another";
            }
            else
            {
                feedbackText.text = "Failed to update name";
            }
            Debug.LogWarning($"Name change failed: Code={e.ErrorCode}, Message={e.Message}");
        }
    }

    public void Open()
    {
        if (SaveManager.Instance?.data != null)
        {
            string name = SaveManager.Instance.data.playerName;
            if (name.Contains("#"))
            {
                name = name.Split('#')[0];
            }
            input.text = name;
        }
        else
        {
            input.text = "";
        }

        feedbackText.text = "";
        rootPanel.SetActive(true);
    }

    public void Close()
    {
        rootPanel.SetActive(false);
    }
}