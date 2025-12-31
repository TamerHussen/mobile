using UnityEngine;

public class StartupDebugger : MonoBehaviour
{
    void Awake()
    {
        Debug.Log("=== GAME STARTING ===");
        Debug.Log($"Platform: {Application.platform}");
        Debug.Log($"Unity Version: {Application.unityVersion}");
    }

    void Start()
    {
        Debug.Log("=== START() REACHED ===");
        CheckManagers();
    }

    void CheckManagers()
    {
        Debug.Log($"SaveManager: {(SaveManager.Instance != null ? "OK" : "NULL")}");
        Debug.Log($"GoogleAdsManager: {(GoogleAdsManager.Instance != null ? "OK" : "NULL")}");
        Debug.Log($"CoinsManager: {(CoinsManager.Instance != null ? "OK" : "NULL")}");
    }
}