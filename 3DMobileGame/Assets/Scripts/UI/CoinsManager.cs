using UnityEngine;
using TMPro;
using System;

public class CoinsManager : MonoBehaviour
{
    public static CoinsManager Instance;

    [Header("UI")]
    public TextMeshProUGUI coinsText;

    [Header("Settings")]
    public int startingCoins = 100;
    public int adRewardAmount = 50;

    public event Action<int> OnCoinsChanged;

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

    void Start()
    {
        // Initialize coins if new player
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

        UpdateUI();
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

        OnCoinsChanged?.Invoke(SaveManager.Instance.data.coins);
        UpdateUI();
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
        if (coinsText != null)
        {
            int coins = GetCoins();
            coinsText.text = $"Coins: {coins}";
        }
    }

    // Call this when player watches an ad
    public void RewardAdCoins()
    {
        AddCoins(adRewardAmount);
        Debug.Log($"✅ Rewarded {adRewardAmount} coins for watching ad!");
    }

    // Rebind UI after scene loads
    public void RebindUI(TextMeshProUGUI newCoinsText)
    {
        coinsText = newCoinsText;
        UpdateUI();
    }
}