using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

        if (gameOverPanel != null)
        {
            gameOverPanel.gameObject.SetActive(false);
            Debug.Log(" GameOverPanel hidden at start");
        }

        if (reviveAdPanel != null)
        {
            reviveAdPanel.gameObject.SetActive(false);
            Debug.Log(" ReviveAdPanel hidden at start");
        }
    }

    void AutoAssignUIReferences()
    {
        livesText = GameObject.Find("LivesText")?.GetComponent<TextMeshProUGUI>();
        if (livesText != null)
            Debug.Log(" Auto-found LivesText");
        else
            Debug.LogWarning(" LivesText not found");

        var gameOverObj = GameObject.Find("GameOverPanel");
        if (gameOverObj != null)
        {
            gameOverPanel = gameOverObj.GetComponent<GameOverPanel>();
            if (gameOverPanel != null)
                Debug.Log($" Auto-found GameOverPanel script (currently {(gameOverObj.activeSelf ? "active" : "inactive")})");
            else
                Debug.LogError(" GameOverPanel GameObject found but no GameOverPanel script attached!");
        }
        else
        {
            Debug.LogError(" GameOverPanel GameObject not found! Check hierarchy name is exactly 'GameOverPanel'");
        }

        var reviveAdObj = GameObject.Find("ReviveAdPanel");
        if (reviveAdObj != null)
        {
            reviveAdPanel = reviveAdObj.GetComponent<ReviveAdPanel>();
            if (reviveAdPanel != null)
                Debug.Log($" Auto-found ReviveAdPanel script (currently {(reviveAdObj.activeSelf ? "active" : "inactive")})");
            else
                Debug.LogError(" ReviveAdPanel GameObject found but no ReviveAdPanel script attached!");
        }
        else
        {
            Debug.LogError("ReviveAdPanel GameObject not found! Check hierarchy name is exactly 'ReviveAdPanel'");
        }

        lifeIcons = new Image[maxLives];
        for (int i = 0; i < maxLives; i++)
        {
            string iconName = $"LivesIcon{i + 1}";
            GameObject iconObj = GameObject.Find(iconName);
            if (iconObj != null)
            {
                lifeIcons[i] = iconObj.GetComponent<Image>();
                Debug.Log($" Auto-found {iconName}");
            }
            else
            {
                Debug.LogWarning($" {iconName} not found");
            }
        }
    }

    public void LoseLife()
    {
        if (isInvincible || isDead)
        {
            Debug.Log($"LoseLife ignored - Invincible: {isInvincible}, Dead: {isDead}");
            return;
        }

        currentLives--;
        UpdateLivesUI();

        Debug.Log($" Life lost! Remaining: {currentLives}/{maxLives}");

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
        Debug.Log("Starting respawn sequence...");

        var controller = GetComponent<PlayerController>();
        if (controller != null)
            controller.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        if (respawnPoints != null && respawnPoints.Length > 0)
        {
            Transform spawnPoint = respawnPoints[Random.Range(0, respawnPoints.Length)];
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
            Debug.Log($" Respawned at {spawnPoint.name}");
        }
        else
        {
            Debug.LogWarning(" No respawn points assigned!");
        }

        if (controller != null)
            controller.enabled = true;

        StartCoroutine(GrantInvincibility());
    }

    IEnumerator GrantInvincibility()
    {
        isInvincible = true;
        Debug.Log($" Player is invincible for {respawnInvincibilityTime} seconds");

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
        Debug.Log(" Invincibility ended");
    }

    void OnAllLivesLost()
    {
        isDead = true;
        Debug.Log(" ========== ALL LIVES LOST ==========");

        var controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.enabled = false;
            Debug.Log(" Player controller disabled");
        }

        bool isSolo = GameSessionData.Instance == null ||
                      GameSessionData.Instance.players == null ||
                      GameSessionData.Instance.players.Count <= 1;

        Debug.Log($" Game mode: {(isSolo ? "SOLO" : "MULTIPLAYER")}");
        Debug.Log($" Has used revive ad: {hasUsedReviveAd}");

        if (isSolo && !hasUsedReviveAd)
        {
            bool adsManagerExists = GoogleAdsManager.Instance != null;
            bool adsReady = adsManagerExists && GoogleAdsManager.Instance.IsRewardedAdReady();

            Debug.Log($" GoogleAdsManager exists: {adsManagerExists}");
            Debug.Log($" Rewarded ad ready: {adsReady}");

            if (adsReady)
            {
                ShowReviveAdOffer();
            }
            else
            {
                Debug.LogWarning("⚠️ No ads available, proceeding to game over");
                ShowGameOver();
            }
        }
        else
        {
            if (!isSolo)
                Debug.Log(" Multiplayer mode - skipping revive ad");
            else
                Debug.Log(" Already used revive ad - skipping to game over");

            ShowGameOver();
        }
    }

    void ShowReviveAdOffer()
    {
        Debug.Log("========== SHOWING REVIVE AD OFFER ==========");

        if (reviveAdPanel == null)
        {
            Debug.LogError(" ReviveAdPanel reference is NULL! Cannot show revive offer.");
            Debug.LogError(" This means GameObject.Find('ReviveAdPanel') failed during Start()");
            ShowGameOver();
            return;
        }

        Debug.Log($" ReviveAdPanel reference found: {reviveAdPanel.name}");
        Debug.Log($"Current active state: {reviveAdPanel.gameObject.activeSelf}");

        // Activate the panel
        reviveAdPanel.gameObject.SetActive(true);

        if (reviveAdPanel.gameObject.activeSelf)
        {
            Debug.Log(" ReviveAdPanel successfully activated!");
        }
        else
        {
            Debug.LogError(" Failed to activate ReviveAdPanel! Something is preventing activation.");
            Debug.LogError(" Check if parent Canvas is active and enabled.");

            Transform parent = reviveAdPanel.transform.parent;
            while (parent != null)
            {
                Debug.Log($" Checking parent: {parent.name} - Active: {parent.gameObject.activeSelf}");
                if (!parent.gameObject.activeSelf)
                {
                    Debug.LogWarning($" Parent {parent.name} is inactive! This might be the issue.");
                }
                parent = parent.parent;
            }

            ShowGameOver();
        }
    }

    public void OnReviveAdSuccess()
    {
        Debug.Log("========== REVIVE AD SUCCESS ==========");

        hasUsedReviveAd = true;
        currentLives = 1;
        isDead = false;

        UpdateLivesUI();

        Time.timeScale = 1f;
        Debug.Log(" Game unpaused");

        var controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.enabled = true;
            Debug.Log(" Player controller re-enabled");
        }

        StartCoroutine(RespawnPlayer());

        Debug.Log(" Player revived with 1 life remaining!");
    }

    void ShowGameOver()
    {
        Debug.Log("========== SHOWING GAME OVER ==========");

        if (gameOverPanel == null)
        {
            Debug.LogError(" GameOverPanel reference is NULL! Forcing scene reload.");
            Debug.LogError(" This means GameObject.Find('GameOverPanel') failed during Start()");
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
            return;
        }

        Debug.Log($" GameOverPanel reference found: {gameOverPanel.name}");
        Debug.Log($" Current active state: {gameOverPanel.gameObject.activeSelf}");

        int finalScore = 0;
        int coinsEarned = 0;

        var scoreSystem = GetComponent<PlayerScore>();
        if (scoreSystem != null)
        {
            finalScore = scoreSystem.Score;
            coinsEarned = Mathf.FloorToInt(finalScore * 0.05f);

            if (CoinsManager.Instance != null)
            {
                CoinsManager.Instance.AddCoins(coinsEarned);
                Debug.Log($" Awarded {coinsEarned} coins");
            }

            Debug.Log($" Final Score: {finalScore}");
        }
        else
        {
            Debug.LogWarning(" PlayerScore component not found on player!");
        }

        gameOverPanel.gameObject.SetActive(true);

        if (gameOverPanel.gameObject.activeSelf)
        {
            Debug.Log(" GameOverPanel successfully activated!");
        }
        else
        {
            Debug.LogError(" Failed to activate GameOverPanel!");

            Transform parent = gameOverPanel.transform.parent;
            while (parent != null)
            {
                Debug.Log($" Checking parent: {parent.name} - Active: {parent.gameObject.activeSelf}");
                if (!parent.gameObject.activeSelf)
                {
                    Debug.LogWarning($" Parent {parent.name} is inactive!");
                }
                parent = parent.parent;
            }
        }

        gameOverPanel.ShowDefeat(finalScore, coinsEarned);

        Debug.Log(" GameOverPanel.ShowDefeat() called");
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