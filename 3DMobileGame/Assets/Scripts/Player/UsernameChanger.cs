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
        // Pre-fill with current name
        if (SaveManager.Instance?.data != null)
        {
            input.text = SaveManager.Instance.data.playerName;
        }
    }

    public async void ChangeName()
    {
        feedbackText.text = "";

        string name = input.text.Trim();

        // Validation
        if (string.IsNullOrEmpty(name))
        {
            feedbackText.text = "Name cannot be empty";
            return;
        }

        if (name.Length < MinLength)
        {
            feedbackText.text = $"Name must be at least {MinLength} characters";
            return;
        }

        if (name.Length > MaxLength)
        {
            feedbackText.text = $"Name must be at most {MaxLength} characters";
            return;
        }

        if (!ValidNameRegex.IsMatch(name))
        {
            feedbackText.text = "Only letters, numbers, and _ are allowed";
            return;
        }

        try
        {
            feedbackText.text = "Updating name...";

            // CRITICAL FIX: Use PlayerNameSynchronizer to update everywhere
            if (PlayerNameSynchronizer.Instance != null)
            {
                await PlayerNameSynchronizer.Instance.UpdatePlayerName(name);
            }
            else
            {
                // Fallback if synchronizer doesn't exist
                await AuthenticationService.Instance.UpdatePlayerNameAsync(name);

                if (SaveManager.Instance?.data != null)
                {
                    SaveManager.Instance.data.playerName = name;
                    SaveManager.Instance.Save();
                }

                if (UnityLobbyManager.Instance?.CurrentLobby != null)
                {
                    await UnityLobbyManager.Instance.SyncSaveDataToLobby();
                }
            }

            var relationships = FindFirstObjectByType<RelationshipsManager>();
            relationships?.RefreshLocalPlayerName();
            relationships?.RefreshFriends();

            if (LobbyInfo.Instance != null)
            {
                LobbyInfo.Instance.UpdateHostName(name);
                LobbyInfo.Instance.UpdatePlayerName(AuthenticationService.Instance.PlayerId, name);
            }

            LobbyPlayerSpawner.Instance?.SpawnPlayers();

            feedbackText.text = "Name changed successfully!";
            Debug.Log($"Username changed to: {name}");

            Invoke(nameof(Close), 1f);
        }
        catch (AuthenticationException e)
        {
            feedbackText.text = "Name unavailable or invalid";
            Debug.LogWarning($"Name change failed: {e.Message}");
        }
    }

    public void Open()
    {
        input.text = SaveManager.Instance?.data?.playerName ?? "";
        feedbackText.text = "";
        rootPanel.SetActive(true);
    }

    public void Close()
    {
        rootPanel.SetActive(false);
    }
}