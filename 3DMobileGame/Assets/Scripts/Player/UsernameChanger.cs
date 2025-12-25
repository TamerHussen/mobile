using System.Text.RegularExpressions;
using TMPro;
using Unity.Services.Authentication;
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
        // Pre-fill with current name (remove the # part if present)
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

        // CRITICAL FIX: Unity requires unique names with # suffix
        // Generate a unique identifier
        string playerId = AuthenticationService.Instance.PlayerId;
        string uniqueSuffix = playerId.Substring(playerId.Length - 4); // Last 4 chars of player ID
        string uniqueName = $"{displayName}#{uniqueSuffix}";

        try
        {
            feedbackText.text = "Updating name...";

            // Update Unity Authentication with unique name
            await AuthenticationService.Instance.UpdatePlayerNameAsync(uniqueName);
            Debug.Log($"Updated Unity Authentication to: {uniqueName}");

            // CRITICAL FIX: Save both display name and full unique name
            if (SaveManager.Instance?.data != null)
            {
                SaveManager.Instance.data.playerName = displayName; // Store display name only
                SaveManager.Instance.data.uniquePlayerName = uniqueName; // Store full name with #
                SaveManager.Instance.Save();
            }

            // Sync to lobby with display name
            if (UnityLobbyManager.Instance?.CurrentLobby != null)
            {
                await UnityLobbyManager.Instance.UpdatePlayerDataAsync(displayName, SaveManager.Instance.data.selectedCosmetic);
            }

            // Update UI displays
            var relationships = FindFirstObjectByType<RelationshipsManager>();
            relationships?.RefreshLocalPlayerName();
            relationships?.RefreshFriends();

            if (LobbyInfo.Instance != null)
            {
                LobbyInfo.Instance.UpdateHostName(displayName);
                LobbyInfo.Instance.UpdatePlayerName(AuthenticationService.Instance.PlayerId, displayName);
            }

            LobbyPlayerSpawner.Instance?.SpawnPlayers();

            feedbackText.text = $"Name changed to: {displayName}";
            Debug.Log($"Username changed to: {displayName} (auth: {uniqueName})");

            // Close after 1 second
            Invoke(nameof(Close), 1f);
        }
        catch (AuthenticationException e)
        {
            // Handle duplicate name error
            // Check error code or message for name taken
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
        catch (System.Exception e)
        {
            feedbackText.text = "Failed to update name";
            Debug.LogError($"Unexpected error: {e.Message}");
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