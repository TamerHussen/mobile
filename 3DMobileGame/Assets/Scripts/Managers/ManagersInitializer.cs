using UnityEngine;

public class ManagersInitializer : MonoBehaviour
{
    [Header("Required Managers (Drag from Hierarchy)")]
    public SaveManager saveManager;
    public CoinsManager coinsManager;
    public UnityLobbyManager lobbyManager;
    public GameSessionData gameSessionData;
    public PlayerNameSynchronizer playerNameSync;
    public GoogleAdsManager adsManager;
    public AdFrequencyManager adFrequencyManager;
    public AudioSettingsManager audioManager;

    void Awake()
    {
        // This runs BEFORE anything else
        Debug.Log("=== INITIALIZING MANAGERS ===");

        // Check each manager and warn if missing
        if (saveManager == null)
            Debug.LogError("❌ SaveManager not assigned!");
        else
            Debug.Log("✅ SaveManager assigned");

        if (coinsManager == null)
            Debug.LogError("❌ CoinsManager not assigned!");
        else
            Debug.Log("✅ CoinsManager assigned");

        if (lobbyManager == null)
            Debug.LogError("❌ UnityLobbyManager not assigned!");
        else
            Debug.Log("✅ UnityLobbyManager assigned");

        if (gameSessionData == null)
            Debug.LogError("❌ GameSessionData not assigned!");
        else
            Debug.Log("✅ GameSessionData assigned");

        if (playerNameSync == null)
            Debug.LogError("❌ PlayerNameSynchronizer not assigned!");
        else
            Debug.Log("✅ PlayerNameSynchronizer assigned");

        if (adsManager == null)
            Debug.LogWarning("⚠️ GoogleAdsManager not assigned (optional)");
        else
            Debug.Log("✅ GoogleAdsManager assigned");

        if (adFrequencyManager == null)
            Debug.LogWarning("⚠️ AdFrequencyManager not assigned (optional)");
        else
            Debug.Log("✅ AdFrequencyManager assigned");

        if (audioManager == null)
            Debug.LogWarning("⚠️ AudioSettingsManager not assigned (optional)");
        else
            Debug.Log("✅ AudioSettingsManager assigned");

        Debug.Log("=== MANAGERS INITIALIZATION COMPLETE ===");
    }
}