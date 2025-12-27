using UnityEngine;
using TMPro;

/// <summary>
/// Player scoring system with singleton access
/// Attach to: Player Prefab
/// </summary>
public class PlayerScore : MonoBehaviour
{
    public static PlayerScore Instance; // ✅ Added singleton

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
        // ✅ Singleton setup
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
        // Auto-find UI if not already assigned
        if (ScoreText == null)
            ScoreText = GameObject.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();

        if (CollectibleText == null)
            CollectibleText = GameObject.Find("CollectibleText")?.GetComponent<TextMeshProUGUI>();

        if (CoinsEarnedText == null)
            CoinsEarnedText = GameObject.Find("CoinsEarnedText")?.GetComponent<TextMeshProUGUI>();

        // Find collectibles
        GameObject[] collectibles = GameObject.FindGameObjectsWithTag("Collectible");
        MaxCollectibles = collectibles.Length;

        // Scale collectibles in multiplayer
        if (GameSessionData.Instance != null && GameSessionData.Instance.players != null)
        {
            int playerCount = GameSessionData.Instance.players.Count;
            if (playerCount > 1)
            {
                int additionalCollectibles = (playerCount - 1) * 5;
                MaxCollectibles += additionalCollectibles;
                Debug.Log($"Multiplayer: Added {additionalCollectibles} collectibles for {playerCount} players");
            }
        }

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

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Collectible"))
        {
            Score += pointsPerCollectible;
            Collected++;

            UpdateScoreUI();
            UpdateCollectedUI();

            Destroy(other.gameObject);

            if (Collected >= MaxCollectibles)
            {
                CompleteLevel();
            }
        }
    }

    void UpdateScoreUI()
    {
        if (ScoreText != null)
            ScoreText.text = "Score: " + Score;
    }

    void UpdateCollectedUI()
    {
        if (CollectibleText != null)
            CollectibleText.text = $"Collected: {Collected} / {MaxCollectibles}";
    }

    public void CompleteLevel()
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
            Debug.Log($"Multiplayer: Splitting {coinsEarned} coins among {playerCount} players");
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

        Debug.Log($"✅ Level Complete! Score: {Score}, Time Bonus: {finalTimeBonus}, Coins Earned: {coinsEarned}");

        if (CoinsEarnedText != null)
        {
            CoinsEarnedText.text = $"+{coinsEarned} Coins!";
        }

        UpdateScoreUI();

        Invoke(nameof(ReturnToLobby), 3f);
    }

    void ReturnToLobby()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
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