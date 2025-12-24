using Unity.Services.Authentication;
using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// Ensures SaveManager playerName is synced with Unity Authentication
/// Attach to PersistentManagers in Main Menu
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

        string savedName = SaveManager.Instance.data.playerName;
        string authName = await AuthenticationService.Instance.GetPlayerNameAsync();

        Debug.Log($"Syncing names - SaveManager: '{savedName}', Auth: '{authName}'");

        // Priority: Use SaveManager name (user's custom name)
        if (!string.IsNullOrEmpty(savedName) && !savedName.Contains("#"))
        {
            // SaveManager has a valid custom name - use it
            await UpdateAuthenticationName(savedName);
        }
        else
        {
            // SaveManager has default/empty name - use Auth name
            SaveManager.Instance.data.playerName = authName;
            SaveManager.Instance.Save();
            Debug.Log($"Saved Auth name to SaveManager: {authName}");
        }
    }

    private async Task UpdateAuthenticationName(string name)
    {
        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(name);
            Debug.Log($"✅ Synced to Unity Authentication: {name}");
        }
        catch (AuthenticationException e)
        {
            Debug.LogWarning($"Could not update Auth name: {e.Message}");
        }
    }

    /// <summary>
    /// Updates both SaveManager and Unity Authentication with new name
    /// </summary>
    public async Task UpdatePlayerName(string newName)
    {
        if (string.IsNullOrEmpty(newName))
        {
            Debug.LogError("Cannot update to empty name");
            return;
        }

        // Update SaveManager
        if (SaveManager.Instance?.data != null)
        {
            SaveManager.Instance.data.playerName = newName;
            SaveManager.Instance.Save();
            Debug.Log($"Updated SaveManager: {newName}");
        }

        // Update Unity Authentication
        await UpdateAuthenticationName(newName);

        // Sync to lobby if in one
        if (UnityLobbyManager.Instance?.CurrentLobby != null)
        {
            await UnityLobbyManager.Instance.SyncSaveDataToLobby();
            Debug.Log("Synced name to lobby");
        }

        // Update LobbyInfo display
        if (LobbyInfo.Instance != null)
        {
            LobbyInfo.Instance.UpdateHostName(newName);
        }
    }
}