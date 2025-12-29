using UnityEngine;
using TMPro;

public class PlayerScore : MonoBehaviour
{
    public static PlayerScore Instance;

    [Header("Score Settings")]
    public int Score = 0;
    public int Collected = 0;
    public int MaxCollectibles;
    public int pointsPerCollectible = 10;

    [Header("Coin Conversion")]
    public float coinConversionRate = 0.1f;
    public int timeBonus = 100;
    public float timeBonusMultiplier = 1.0f;

    [Header("UI - AUTO-ASSIGNED")]
    public TextMeshProUGUI ScoreText;
    public TextMeshProUGUI CollectibleText;
    public TextMeshProUGUI CoinsEarnedText;

    [Header("Level Timer")]
    public float levelStartTime;
    public float timeBonusDecayRate = 0.01f;

    private bool levelCompleted = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (ScoreText == null)
            ScoreText = GameObject.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();

        if (CollectibleText == null)
            CollectibleText = GameObject.Find("CollectibleText")?.GetComponent<TextMeshProUGUI>();

        if (CoinsEarnedText == null)
            CoinsEarnedText = GameObject.Find("CoinsEarnedText")?.GetComponent<TextMeshProUGUI>();

        levelStartTime = Time.time;
        UpdateScoreUI();
        UpdateCollectedUI();
    }

    void Update()
    {
        if (!levelCompleted)
        {
            timeBonusMultiplier = Mathf.Max(0.1f, timeBonusMultiplier - (timeBonusDecayRate * Time.deltaTime));
        }
    }
    public void SetMaxCollectibles(int count)
    {
        MaxCollectibles = count;
        UpdateCollectedUI();
    }

    public void AddCollectible()
    {
        if (levelCompleted) return;
        Collected = Mathf.Min(Collected + 1, MaxCollectibles);
        UpdateCollectedUI();

        if (Collected >= MaxCollectibles)
            CompleteLevel(true);
    }


    public void AddScore(int amount)
    {
        Score += amount;
        UpdateScoreUI();
    }


    void UpdateScoreUI()
    {
        if (ScoreText != null)
            ScoreText.text = "Score: " + Score;
    }

    public void UpdateCollectedUI()
    {
        if (CollectibleText != null)
            CollectibleText.text = $"Collected: {Collected} / {MaxCollectibles}";
    }

    public void CompleteLevel(bool victory = true)
    {
        if (levelCompleted) return;
        levelCompleted = true;

        float levelTime = Time.time - levelStartTime;
        int finalTimeBonus = Mathf.RoundToInt(timeBonus * timeBonusMultiplier);
        Score += finalTimeBonus;

        int coinsEarned = Mathf.FloorToInt(Score * coinConversionRate);

        bool isMultiplayer = GameSessionData.Instance != null &&
                            GameSessionData.Instance.players != null &&
                            GameSessionData.Instance.players.Count > 1;

        if (isMultiplayer)
        {
            int playerCount = GameSessionData.Instance.players.Count;
            coinsEarned = Mathf.FloorToInt(coinsEarned / (float)playerCount);
        }

        if (CoinsManager.Instance != null)
        {
            CoinsManager.Instance.AddCoins(coinsEarned);
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Save();
        }

        if (AdFrequencyManager.Instance != null)
        {
            AdFrequencyManager.Instance.OnLevelComplete();
        }

        Debug.Log($"Level Complete! Score: {Score}, Coins: {coinsEarned}");

        if (CoinsEarnedText != null)
        {
            CoinsEarnedText.text = $"+{coinsEarned} Coins!";
        }

        UpdateScoreUI();

        if (LevelUIManager.Instance != null)
        {
            if (victory)
                LevelUIManager.Instance.gameOverPanel.ShowVictory(Score, coinsEarned);
            else
                LevelUIManager.Instance.ShowGameOver(Score, coinsEarned);
        }

    }

    public int GetCurrentScore()
    {
        return Score;
    }

    public bool IsLevelComplete()
    {
        return levelCompleted;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}