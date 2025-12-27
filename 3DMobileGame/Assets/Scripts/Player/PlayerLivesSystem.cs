using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Player lives system with panel integration
/// Attach to: Player Prefab
/// </summary>
public class PlayerLivesSystem : MonoBehaviour
{
    public static PlayerLivesSystem Instance;

    [Header("Lives Settings")]
    public int maxLives = 3;
    public int currentLives = 3;

    [Header("Respawn")]
    public Transform[] respawnPoints;
    public float respawnInvincibilityTime = 3f;
    public float respawnDelay = 1f;

    [Header("UI - AUTO-ASSIGNED")]
    private TextMeshProUGUI livesText;
    private Image[] lifeIcons;
    private GameOverPanel gameOverPanel;
    private ReviveAdPanel reviveAdPanel;

    [Header("Revive Ad Settings")]
    public float reviveAdTimeout = 10f;

    private bool hasUsedReviveAd = false;
    private bool isInvincible = false;
    private bool isDead = false;

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
        AutoAssignUIReferences();

        currentLives = maxLives;
        UpdateLivesUI();

        // Panels are controlled by their own scripts
        if (gameOverPanel != null)
            gameOverPanel.gameObject.SetActive(false);

        if (reviveAdPanel != null)
            reviveAdPanel.gameObject.SetActive(false);
    }

    void AutoAssignUIReferences()
    {
        livesText = GameObject.Find("LivesText")?.GetComponent<TextMeshProUGUI>();
        if (livesText != null) Debug.Log("✅ Auto-found LivesText");

        // Find panel scripts
        var gameOverObj = GameObject.Find("GameOverPanel");
        if (gameOverObj != null)
        {
            gameOverPanel = gameOverObj.GetComponent<GameOverPanel>();
            if (gameOverPanel != null)
                Debug.Log("✅ Auto-found GameOverPanel script");
        }

        var reviveAdObj = GameObject.Find("ReviveAdPanel");
        if (reviveAdObj != null)
        {
            reviveAdPanel = reviveAdObj.GetComponent<ReviveAdPanel>();
            if (reviveAdPanel != null)
                Debug.Log("✅ Auto-found ReviveAdPanel script");
        }

        // Find life icons
        lifeIcons = new Image[maxLives];
        for (int i = 0; i < maxLives; i++)
        {
            string iconName = $"LifeIcon{i + 1}";
            GameObject iconObj = GameObject.Find(iconName);
            if (iconObj != null)
            {
                lifeIcons[i] = iconObj.GetComponent<Image>();
                Debug.Log($"✅ Auto-found {iconName}");
            }
        }
    }

    public void LoseLife()
    {
        if (isInvincible || isDead) return;

        currentLives--;
        UpdateLivesUI();

        Debug.Log($"Life lost! Remaining: {currentLives}");

        // ✅ NOTIFY AD FREQUENCY MANAGER
        if (AdFrequencyManager.Instance != null)
        {
            AdFrequencyManager.Instance.OnPlayerDeath();
        }

        if (currentLives <= 0)
        {
            OnAllLivesLost();
        }
        else
        {
            StartCoroutine(RespawnPlayer());
        }
    }

    IEnumerator RespawnPlayer()
    {
        var controller = GetComponent<PlayerController>();
        if (controller != null)
            controller.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        if (respawnPoints != null && respawnPoints.Length > 0)
        {
            Transform spawnPoint = respawnPoints[Random.Range(0, respawnPoints.Length)];
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }

        if (controller != null)
            controller.enabled = true;

        StartCoroutine(GrantInvincibility());
    }

    IEnumerator GrantInvincibility()
    {
        isInvincible = true;
        Debug.Log("Player is invincible for " + respawnInvincibilityTime + " seconds");

        float elapsed = 0f;
        var renderers = GetComponentsInChildren<Renderer>();

        while (elapsed < respawnInvincibilityTime)
        {
            bool visible = Mathf.Sin(elapsed * 10f) > 0;
            foreach (var r in renderers)
            {
                if (r != null)
                    r.enabled = visible;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var r in renderers)
        {
            if (r != null)
                r.enabled = true;
        }

        isInvincible = false;
        Debug.Log("Invincibility ended");
    }

    void OnAllLivesLost()
    {
        isDead = true;
        Debug.Log("All lives lost!");

        bool isSolo = GameSessionData.Instance == null ||
                      GameSessionData.Instance.players == null ||
                      GameSessionData.Instance.players.Count <= 1;

        // ✅ SHOW REVIVE AD OFFER (Solo only, one-time)
        if (isSolo && !hasUsedReviveAd && GoogleAdsManager.Instance != null &&
            GoogleAdsManager.Instance.IsRewardedAdReady())
        {
            ShowReviveAdOffer();
        }
        else
        {
            ShowGameOver();
        }
    }

    void ShowReviveAdOffer()
    {
        if (reviveAdPanel != null)
        {
            // ReviveAdPanel script handles:
            // - Pausing game
            // - Countdown timer
            // - Ad display
            reviveAdPanel.gameObject.SetActive(true);
            Debug.Log("💰 Showing revive ad offer");
        }
        else
        {
            Debug.LogWarning("ReviveAdPanel not found! Showing game over instead.");
            ShowGameOver();
        }
    }

    /// <summary>
    /// Called by ReviveAdPanel when ad is successfully watched
    /// </summary>
    public void OnReviveAdSuccess()
    {
        hasUsedReviveAd = true;
        currentLives = 1;
        isDead = false;

        UpdateLivesUI();
        Time.timeScale = 1f; // Unpause

        StartCoroutine(RespawnPlayer());
        StartCoroutine(GrantInvincibility());

        Debug.Log("✅ Player revived with ad!");
    }

    void ShowGameOver()
    {
        // Get final score
        int finalScore = 0;
        int coinsEarned = 0;

        var scoreSystem = GetComponent<PlayerScore>();
        if (scoreSystem != null)
        {
            finalScore = scoreSystem.Score;
            coinsEarned = Mathf.FloorToInt(finalScore * 0.05f); // 50% penalty for death

            if (CoinsManager.Instance != null)
            {
                CoinsManager.Instance.AddCoins(coinsEarned);
            }

            Debug.Log($"Game Over - Score: {finalScore}, Earned {coinsEarned} coins");
        }

        // Show game over panel
        if (gameOverPanel != null)
        {
            // GameOverPanel script handles:
            // - Pausing game
            // - Auto-return timer
            // - Button interactions
            gameOverPanel.ShowDefeat(finalScore, coinsEarned);
        }
        else
        {
            Debug.LogWarning("GameOverPanel not found! Returning to lobby directly.");
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }
    }

    void UpdateLivesUI()
    {
        if (livesText != null)
            livesText.text = $"Lives: {currentLives}";

        if (lifeIcons != null)
        {
            for (int i = 0; i < lifeIcons.Length; i++)
            {
                if (lifeIcons[i] != null)
                    lifeIcons[i].enabled = i < currentLives;
            }
        }
    }

    public bool IsInvincible()
    {
        return isInvincible;
    }

    public bool IsDead()
    {
        return isDead;
    }

    public int GetCurrentLives()
    {
        return currentLives;
    }
}