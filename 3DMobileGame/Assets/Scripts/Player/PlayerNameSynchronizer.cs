using Unity.Services.Authentication;
using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// Ensures SaveManager playerName is synced with Unity Authentication
/// Handles display names vs unique names (with # suffix)
/// </summary>
public class PlayerNameSynchronizer : MonoBehaviour
{
    public static PlayerNameSynchronizer Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Call this after authentication to sync names
    /// </summary>

    public async Task SyncPlayerName()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("Cannot sync name: Not signed in");
            return;
        }

        if (SaveManager.Instance?.data == null)
        {
            Debug.LogWarning("Cannot sync name: SaveManager not ready");
            return;
        }

        // CRITICAL: Get the source of truth from Unity Authentication
        string authUniqueName = await AuthenticationService.Instance.GetPlayerNameAsync();

        // Extract display name
        string displayName = authUniqueName.Contains("#")
            ? authUniqueName.Split('#')[0]
            : authUniqueName;

        // Update SaveManager to match
        SaveManager.Instance.data.playerName = displayName;
        SaveManager.Instance.data.uniquePlayerName = authUniqueName;
        SaveManager.Instance.Save();

        Debug.Log($"✅ Name sync complete: Display='{displayName}', Unique='{authUniqueName}'");
    }

    private async Task UpdateAuthenticationName(string name)
    {
        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(name);
            Debug.Log($"✅ Updated Unity Authentication to: {name}");
        }
        catch (AuthenticationException e)
        {
            Debug.LogWarning($"Could not update Auth name: {e.Message}");
        }
    }

    /// <summary>
    /// Updates display name and generates new unique name
    /// </summary>
    public async Task UpdatePlayerName(string newDisplayName)
    {
        if (string.IsNullOrEmpty(newDisplayName))
        {
            Debug.LogError("Cannot update to empty name");
            return;
        }

        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(newDisplayName);

            // Get what Unity actually assigned
            string actualUniqueName = await AuthenticationService.Instance.GetPlayerNameAsync();

            Debug.Log($"Unity assigned: {actualUniqueName}");

            // Update SaveManager
            if (SaveManager.Instance?.data != null)
            {
                SaveManager.Instance.data.playerName = newDisplayName;
                SaveManager.Instance.data.uniquePlayerName = actualUniqueName;
                SaveManager.Instance.Save();
                Debug.Log($"Updated SaveManager: Display='{newDisplayName}', Unique='{actualUniqueName}'");
            }

            // Sync to lobby with display name
            if (UnityLobbyManager.Instance?.CurrentLobby != null)
            {
                await UnityLobbyManager.Instance.UpdatePlayerDataAsync(newDisplayName, SaveManager.Instance.data.selectedCosmetic);
                Debug.Log("Synced name to lobby");
            }

            // Update LobbyInfo display
            if (LobbyInfo.Instance != null)
            {
                LobbyInfo.Instance.UpdateHostName(newDisplayName);
            }
        }
        catch (AuthenticationException e)
        {
            Debug.LogError($"Failed to update name: {e.Message}");
        }
    }



    /// <summary>
    /// Get the display name for UI (without # suffix)
    /// </summary>
    public string GetDisplayName()
    {
        if (SaveManager.Instance?.data == null)
            return "Player";

        string name = SaveManager.Instance.data.playerName;

        // Remove # suffix if somehow it got saved
        if (name.Contains("#"))
        {
            return name.Split('#')[0];
        }

        return name;
    }

    /// <summary>
    /// Get the unique name for friends API (with # suffix)
    /// </summary>
    public string GetUniqueName()
    {
        if (SaveManager.Instance?.data == null)
            return "";

        return SaveManager.Instance.data.uniquePlayerName;
    }
}