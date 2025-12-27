using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the Game Over panel UI and interactions
/// Attach to: GameOverPanel GameObject
/// </summary>
public class GameOverPanel : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI coinsEarnedText;
    public TextMeshProUGUI messageText;
    public Button lobbyButton;
    public Button retryButton;

    [Header("Settings")]
    public string gameOverTitle = "Game Over";
    public string victoryTitle = "Victory!";
    public float autoReturnDelay = 5f;

    private bool isVictory = false;
    private float returnTimer;

    void Awake()
    {
        // Auto-find UI elements if not assigned
        if (titleText == null)
            titleText = transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();

        if (scoreText == null)
            scoreText = transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();

        if (coinsEarnedText == null)
            coinsEarnedText = transform.Find("CoinsEarnedText")?.GetComponent<TextMeshProUGUI>();

        if (messageText == null)
            messageText = transform.Find("MessageText")?.GetComponent<TextMeshProUGUI>();

        if (lobbyButton == null)
            lobbyButton = transform.Find("LobbyButton")?.GetComponent<Button>();

        if (retryButton == null)
            retryButton = transform.Find("RetryButton")?.GetComponent<Button>();

        // Bind button events
        if (lobbyButton != null)
            lobbyButton.onClick.AddListener(OnLobbyButtonClicked);

        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryButtonClicked);
    }

    void OnEnable()
    {
        // ✅ PAUSE THE GAME when panel opens
        Time.timeScale = 0f;
        Debug.Log("⏸️ Game paused - Game Over panel shown");

        // Start auto-return timer
        returnTimer = autoReturnDelay;
    }

    void Update()
    {
        // Countdown timer (uses unscaled time since game is paused)
        if (returnTimer > 0)
        {
            returnTimer -= Time.unscaledDeltaTime;

            if (messageText != null)
            {
                messageText.text = $"Returning to lobby in {Mathf.Ceil(returnTimer)}s...";
            }

            if (returnTimer <= 0)
            {
                OnLobbyButtonClicked();
            }
        }
    }

    /// <summary>
    /// Show game over panel with defeat state
    /// </summary>
    public void ShowDefeat(int finalScore, int coinsEarned)
    {
        isVictory = false;

        if (titleText != null)
            titleText.text = gameOverTitle;

        if (scoreText != null)
            scoreText.text = $"Final Score: {finalScore}";

        if (coinsEarnedText != null)
            coinsEarnedText.text = $"+{coinsEarned} Coins";

        // Show retry button for defeat
        if (retryButton != null)
            retryButton.gameObject.SetActive(true);

        gameObject.SetActive(true);
    }

    /// <summary>
    /// Show game over panel with victory state
    /// </summary>
    public void ShowVictory(int finalScore, int coinsEarned)
    {
        isVictory = true;

        if (titleText != null)
            titleText.text = victoryTitle;

        if (scoreText != null)
            scoreText.text = $"Final Score: {finalScore}";

        if (coinsEarnedText != null)
            coinsEarnedText.text = $"+{coinsEarned} Coins";

        // Hide retry button for victory
        if (retryButton != null)
            retryButton.gameObject.SetActive(false);

        gameObject.SetActive(true);
    }

    void OnLobbyButtonClicked()
    {
        Debug.Log("🏠 Returning to lobby...");

        // ✅ UNPAUSE before scene change
        Time.timeScale = 1f;

        // Save data
        if (SaveManager.Instance != null)
            SaveManager.Instance.Save();

        // Return to lobby
        SceneManager.LoadScene("Lobby");
    }

    void OnRetryButtonClicked()
    {
        Debug.Log("🔄 Retrying level...");

        // ✅ UNPAUSE before scene change
        Time.timeScale = 1f;

        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnDisable()
    {
        // Safety: ensure game is unpaused when panel closes
        Time.timeScale = 1f;
    }
}