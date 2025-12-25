using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CosmeticButtonManager : MonoBehaviour
{
    public static CosmeticButtonManager Instance;

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

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Lobby")
        {
            Debug.Log("Rebinding cosmetic buttons...");
            RebindCosmeticButtons();
        }
    }

    void RebindCosmeticButtons()
    {
        var allButtons = FindObjectsByType<CosmeticButton>(FindObjectsSortMode.None);

        Debug.Log($"Found {allButtons.Length} cosmetic buttons to rebind");

        foreach (var cosmeticButton in allButtons)
        {
            cosmeticButton.RebindButton();
        }
    }
}