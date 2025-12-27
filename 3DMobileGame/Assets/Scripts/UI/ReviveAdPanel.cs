using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Manages the Revive Ad panel UI and interactions
/// Attach to: ReviveAdPanel GameObject
/// </summary>
public class ReviveAdPanel : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI descriptionText;
    public Button reviveButton;
    public Button declineButton;

    [Header("Settings")]
    public float timeoutDuration = 10f;
    public string titleMessage = "Continue Playing?";
    public string descriptionMessage = "Watch an ad to revive with 1 life!";

    private float timeRemaining;
    private bool isWaitingForResponse = false;

    void Awake()
    {
        // Auto-find UI elements if not assigned
        if (titleText == null)
            titleText = transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();

        if (timerText == null)
            timerText = transform.Find("TimerText")?.GetComponent<TextMeshProUGUI>();

        if (descriptionText == null)
            descriptionText = transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();

        if (reviveButton == null)
            reviveButton = transform.Find("ReviveButton")?.GetComponent<Button>();

        if (declineButton == null)
            declineButton = transform.Find("DeclineButton")?.GetComponent<Button>();

        // Bind button events
        if (reviveButton != null)
            reviveButton.onClick.AddListener(OnReviveClicked);

        if (declineButton != null)
            declineButton.onClick.AddListener(OnDeclineClicked);
    }

    void OnEnable()
    {
        // ✅ PAUSE THE GAME when panel opens
        Time.timeScale = 0f;
        Debug.Log("⏸️ Game paused - Revive Ad panel shown");

        // Set initial text
        if (titleText != null)
            titleText.text = titleMessage;

        if (descriptionText != null)
            descriptionText.text = descriptionMessage;

        // Start timeout countdown
        timeRemaining = timeoutDuration;
        isWaitingForResponse = true;

        StartCoroutine(CountdownTimer());
    }

    IEnumerator CountdownTimer()
    {
        while (timeRemaining > 0 && isWaitingForResponse)
        {
            if (timerText != null)
            {
                timerText.text = $"Time Remaining: {Mathf.Ceil(timeRemaining)}s";
            }

            // Use unscaled time since game is paused
            timeRemaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        // Auto-decline if time runs out
        if (isWaitingForResponse)
        {
            OnDeclineClicked();
        }
    }

    void OnReviveClicked()
    {
        isWaitingForResponse = false;

        Debug.Log("💰 Player accepted revive ad offer");

        // Check if ad is ready
        if (GoogleAdsManager.Instance == null)
        {
            Debug.LogError("GoogleAdsManager not found!");
            OnReviveFailed();
            return;
        }

        if (!GoogleAdsManager.Instance.IsRewardedAdReady())
        {
            Debug.LogWarning("Rewarded ad not ready!");
            OnReviveFailed();
            return;
        }

        // Hide panel while ad plays
        gameObject.SetActive(false);

        // Show the ad
        GoogleAdsManager.Instance.ShowRewardedAd(OnReviveSuccess, OnReviveFailed);
    }

    void OnReviveSuccess()
    {
        Debug.Log("✅ Revive ad completed successfully!");

        // Notify PlayerLivesSystem to revive player
        if (PlayerLivesSystem.Instance != null)
        {
            PlayerLivesSystem.Instance.OnReviveAdSuccess();
        }
        else
        {
            Debug.LogError("PlayerLivesSystem.Instance not found!");
            Time.timeScale = 1f; // Emergency unpause
        }
    }

    void OnReviveFailed()
    {
        Debug.LogWarning("❌ Revive ad failed or was cancelled");

        // Close revive panel
        gameObject.SetActive(false);

        // Show game over panel instead
        var gameOverPanel = GameObject.Find("GameOverPanel")?.GetComponent<GameOverPanel>();
        if (gameOverPanel != null)
        {
            // Get final score
            int finalScore = 0;
            int coinsEarned = 0;

            if (PlayerLivesSystem.Instance != null)
            {
                var playerScore = PlayerLivesSystem.Instance.GetComponent<PlayerScore>();
                if (playerScore != null)
                {
                    finalScore = playerScore.Score;
                    coinsEarned = Mathf.FloorToInt(finalScore * 0.05f); // 50% penalty
                }
            }

            gameOverPanel.ShowDefeat(finalScore, coinsEarned);
        }
        else
        {
            // Fallback: just unpause and return to lobby
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }
    }

    void OnDeclineClicked()
    {
        isWaitingForResponse = false;

        Debug.Log("❌ Player declined revive ad offer");

        // Close revive panel
        gameObject.SetActive(false);

        // Show game over panel
        var gameOverPanel = GameObject.Find("GameOverPanel")?.GetComponent<GameOverPanel>();
        if (gameOverPanel != null)
        {
            // Get final score
            int finalScore = 0;
            int coinsEarned = 0;

            if (PlayerLivesSystem.Instance != null)
            {
                var playerScore = PlayerLivesSystem.Instance.GetComponent<PlayerScore>();
                if (playerScore != null)
                {
                    finalScore = playerScore.Score;
                    coinsEarned = Mathf.FloorToInt(finalScore * 0.05f); // 50% penalty
                }
            }

            gameOverPanel.ShowDefeat(finalScore, coinsEarned);
        }
        else
        {
            // Fallback: just unpause and return to lobby
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }
    }

    void OnDisable()
    {
        // Stop countdown
        isWaitingForResponse = false;

        // Safety: ensure game is unpaused when panel closes
        // (Only if we're not showing another panel)
        if (!gameObject.activeInHierarchy)
        {
            var gameOverPanel = GameObject.Find("GameOverPanel");
            if (gameOverPanel == null || !gameOverPanel.activeSelf)
            {
                Time.timeScale = 1f;
            }
        }
    }
}