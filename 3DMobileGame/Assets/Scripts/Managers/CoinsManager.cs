using UnityEngine;
using TMPro;
using System;
using UnityEngine.SceneManagement;

public class CoinsManager : MonoBehaviour
{
    public static CoinsManager Instance;

    [Header("UI - AUTO-LINKED")]
    private TextMeshProUGUI coinsText;

    [Header("Settings")]
    public int startingCoins = 100;
    public int adRewardAmount = 50;

    [Header("UI Auto-Find Names")]
    [Tooltip("Name of the TextMeshProUGUI GameObject to find in each scene")]
    public string coinsTextName = "CoinText";

    public event Action<int> OnCoinsChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("CoinsManager initialized");
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

    void Start()
    {
        if (SaveManager.Instance != null)
        {
            if (SaveManager.Instance.data.coins == 0 && PlayerPrefs.GetInt("FirstTimePlaying", 1) == 1)
            {
                AddCoins(startingCoins);
                PlayerPrefs.SetInt("FirstTimePlaying", 0);
                PlayerPrefs.Save();
                Debug.Log($"New player! Awarded {startingCoins} starting coins");
            }
        }

        FindCoinsUI();
        UpdateUI();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name} - Finding coins UI...");
        Invoke(nameof(FindCoinsUI), 0.1f);
    }

    void FindCoinsUI()
    {
        GameObject coinsObj = GameObject.Find(coinsTextName);

        if (coinsObj != null)
        {
            coinsText = coinsObj.GetComponent<TextMeshProUGUI>();

            if (coinsText != null)
            {
                Debug.Log($"Auto-linked coins UI: {coinsTextName}");
                UpdateUI();
            }
            else
            {
                Debug.LogWarning($"Found '{coinsTextName}' but no TextMeshProUGUI component!");
            }
        }
        else
        {
            // fallback search for common names
            coinsObj = GameObject.Find("Coins");
            if (coinsObj == null)
                coinsObj = GameObject.Find("CoinsWallet");
            if (coinsObj == null)
                coinsObj = GameObject.Find("CoinText");

            if (coinsObj != null)
            {
                coinsText = coinsObj.GetComponent<TextMeshProUGUI>();
                if (coinsText != null)
                {
                    Debug.Log($"Auto-linked coins UI: {coinsObj.name}");
                    UpdateUI();
                }
            }
            else
            {
                Debug.LogWarning($"Coins UI not found in scene! Looking for '{coinsTextName}'");
                coinsText = null;
            }
        }
    }

    public int GetCoins()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.data == null)
        {
            Debug.LogWarning("SaveManager not available, returning 0 coins");
            return 0;
        }

        return SaveManager.Instance.data.coins;
    }

    public void AddCoins(int amount)
    {
        if (SaveManager.Instance == null || SaveManager.Instance.data == null)
        {
            Debug.LogError("Cannot add coins: SaveManager not available");
            return;
        }

        SaveManager.Instance.data.coins += amount;
        SaveManager.Instance.Save();

        Debug.Log($"Added {amount} coins. Total: {SaveManager.Instance.data.coins}");

        // force immediate updates
        OnCoinsChanged?.Invoke(SaveManager.Instance.data.coins);

        // make sure UI updates happen this frame
        if (coinsText == null)
            FindCoinsUI();

        UpdateUI();

        // backup delayed update in case UI wasn't ready
        StartCoroutine(DelayedUIUpdate());
    }

    public bool SpendCoins(int amount)
    {
        if (SaveManager.Instance == null || SaveManager.Instance.data == null)
        {
            Debug.LogError("Cannot spend coins: SaveManager not available");
            return false;
        }

        if (SaveManager.Instance.data.coins < amount)
        {
            Debug.LogWarning($"Not enough coins! Need {amount}, have {SaveManager.Instance.data.coins}");
            return false;
        }

        SaveManager.Instance.data.coins -= amount;
        SaveManager.Instance.Save();

        Debug.Log($"Spent {amount} coins. Remaining: {SaveManager.Instance.data.coins}");

        OnCoinsChanged?.Invoke(SaveManager.Instance.data.coins);
        UpdateUI();

        return true;
    }

    public void SetCoins(int amount)
    {
        if (SaveManager.Instance == null || SaveManager.Instance.data == null)
        {
            Debug.LogError("Cannot set coins: SaveManager not available");
            return;
        }

        SaveManager.Instance.data.coins = amount;
        SaveManager.Instance.Save();

        Debug.Log($"Set coins to: {amount}");

        OnCoinsChanged?.Invoke(SaveManager.Instance.data.coins);
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (coinsText == null)
        {
            FindCoinsUI();
            if (coinsText == null)
                return;
        }

        // direct update on main thread
        int currentCoins = GetCoins();
        coinsText.text = $"{currentCoins}";

        Debug.Log($"UI updated to show {currentCoins} coins");
    }

    // backup method to ensure UI catches up
    private System.Collections.IEnumerator DelayedUIUpdate()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        if (coinsText != null)
        {
            UpdateUI();
        }
    }

    public void RewardAdCoins()
    {
        AddCoins(adRewardAmount);
        Debug.Log($"Rewarded {adRewardAmount} coins for watching ad!");
    }

    public void RebindUI(TextMeshProUGUI newCoinsText)
    {
        coinsText = newCoinsText;
        UpdateUI();
        Debug.Log("Manually rebound coins UI");
    }

    public void ForceRefreshUI()
    {
        coinsText = null;
        FindCoinsUI();
    }
}