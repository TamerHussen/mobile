using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

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
        Debug.Log(" GameOverPanel Awake()");

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

        if (lobbyButton != null)
            lobbyButton.onClick.AddListener(OnLobbyButtonClicked);

        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryButtonClicked);

        Debug.Log($" GameOverPanel UI references assigned - Title: {titleText != null}, Score: {scoreText != null}, Coins: {coinsEarnedText != null}");
    }

    void OnEnable()
    {
        Debug.Log("GameOverPanel OnEnable() called");
        Debug.Log($" Current Time.timeScale: {Time.timeScale}");

        Time.timeScale = 0f;
        Debug.Log("Game paused - Game Over panel shown");

        returnTimer = autoReturnDelay;
    }

    void Update()
    {
        if (returnTimer > 0)
        {
            returnTimer -= Time.unscaledDeltaTime;

            if (messageText != null)
            {
                messageText.text = $"Returning to lobby in {Mathf.Ceil(returnTimer)}s...";
            }

            if (returnTimer <= 0)
            {
                Debug.Log(" Auto-return timer expired, returning to lobby");
                OnLobbyButtonClicked();
            }
        }
    }

    public void ShowDefeat(int finalScore, int coinsEarned)
    {
        Debug.Log($" ShowDefeat() called - Score: {finalScore}, Coins: {coinsEarned}");

        isVictory = false;

        if (titleText != null)
        {
            titleText.text = gameOverTitle;
            Debug.Log($" Title set to: {gameOverTitle}");
        }
        else
        {
            Debug.LogError(" titleText is NULL!");
        }

        if (scoreText != null)
        {
            scoreText.text = $"Final Score: {finalScore}";
            Debug.Log($" Score text set to: {finalScore}");
        }
        else
        {
            Debug.LogError(" scoreText is NULL!");
        }

        if (coinsEarnedText != null)
        {
            coinsEarnedText.text = $"+{coinsEarned} Coins";
            Debug.Log($" Coins text set to: {coinsEarned}");
        }
        else
        {
            Debug.LogError(" coinsEarnedText is NULL!");
        }

        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(true);
            Debug.Log(" Retry button shown");
        }

        if (!gameObject.activeSelf)
        {
            Debug.LogWarning(" Panel was inactive during ShowDefeat, activating now!");
            gameObject.SetActive(true);
        }

        Debug.Log($"ShowDefeat() complete - Panel active: {gameObject.activeSelf}");
    }

    public void ShowVictory(int finalScore, int coinsEarned)
    {
        Debug.Log($"ShowVictory() called - Score: {finalScore}, Coins: {coinsEarned}");

        isVictory = true;

        if (titleText != null)
            titleText.text = victoryTitle;

        if (scoreText != null)
            scoreText.text = $"Final Score: {finalScore}";

        if (coinsEarnedText != null)
            coinsEarnedText.text = $"+{coinsEarned} Coins";

        if (retryButton != null)
            retryButton.gameObject.SetActive(false);

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }

    void OnLobbyButtonClicked()
    {
        Debug.Log("Lobby button clicked");

        Time.timeScale = 1f;

        if (SaveManager.Instance != null)
            SaveManager.Instance.Save();

        SceneManager.LoadScene("Lobby");
    }

    void OnRetryButtonClicked()
    {
        Debug.Log("Retry button clicked");

        Time.timeScale = 1f;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnDisable()
    {
        Debug.Log(" GameOverPanel OnDisable() called");
        Time.timeScale = 1f;
    }
}