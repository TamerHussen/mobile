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

        string authName = await AuthenticationService.Instance.GetPlayerNameAsync();
        string savedDisplayName = SaveManager.Instance.data.playerName;
        string savedUniqueName = SaveManager.Instance.data.uniquePlayerName;

        Debug.Log($"Syncing names - Auth: '{authName}', SavedDisplay: '{savedDisplayName}', SavedUnique: '{savedUniqueName}'");

        // CRITICAL FIX: Handle name sync properly
        if (string.IsNullOrEmpty(savedUniqueName) || savedUniqueName == "Player")
        {
            // First time setup - use auth name
            if (authName.Contains("#"))
            {
                // Auth has unique name - split it
                SaveManager.Instance.data.uniquePlayerName = authName;
                SaveManager.Instance.data.playerName = authName.Split('#')[0];
            }
            else
            {
                // Generate unique name
                string playerId = AuthenticationService.Instance.PlayerId;
                string uniqueSuffix = playerId.Substring(playerId.Length - 4);
                string uniqueName = $"{savedDisplayName}#{uniqueSuffix}";

                SaveManager.Instance.data.uniquePlayerName = uniqueName;
                SaveManager.Instance.data.playerName = savedDisplayName;

                // Update auth
                await UpdateAuthenticationName(uniqueName);
            }

            SaveManager.Instance.Save();
        }
        else
        {
            // Already have unique name - ensure auth matches
            if (authName != savedUniqueName)
            {
                await UpdateAuthenticationName(savedUniqueName);
            }
        }

        Debug.Log($"✅ Name sync complete: Display='{SaveManager.Instance.data.playerName}', Unique='{SaveManager.Instance.data.uniquePlayerName}'");
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

        // Generate unique name with # suffix
        string playerId = AuthenticationService.Instance.PlayerId;
        string uniqueSuffix = playerId.Substring(playerId.Length - 4);
        string uniqueName = $"{newDisplayName}#{uniqueSuffix}";

        // Update SaveManager
        if (SaveManager.Instance?.data != null)
        {
            SaveManager.Instance.data.playerName = newDisplayName; // Display name only
            SaveManager.Instance.data.uniquePlayerName = uniqueName; // Full unique name
            SaveManager.Instance.Save();
            Debug.Log($"Updated SaveManager: Display='{newDisplayName}', Unique='{uniqueName}'");
        }

        // Update Unity Authentication with unique name
        await UpdateAuthenticationName(uniqueName);

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