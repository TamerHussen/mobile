using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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
        Debug.Log(" ReviveAdPanel Awake()");

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

        if (reviveButton != null)
        {
            reviveButton.onClick.AddListener(OnReviveClicked);
            Debug.Log(" Revive button listener added");
        }
        else
        {
            Debug.LogError(" Revive button not found!");
        }

        if (declineButton != null)
        {
            declineButton.onClick.AddListener(OnDeclineClicked);
            Debug.Log(" Decline button listener added");
        }
        else
        {
            Debug.LogError(" Decline button not found!");
        }
    }

    void OnEnable()
    {
        Debug.Log("ReviveAdPanel OnEnable() called");
        Debug.Log($" Current Time.timeScale: {Time.timeScale}");

        Time.timeScale = 0f;
        Debug.Log(" Game paused - Revive Ad panel shown");

        if (titleText != null)
        {
            titleText.text = titleMessage;
            Debug.Log($" Title set to: {titleMessage}");
        }

        if (descriptionText != null)
        {
            descriptionText.text = descriptionMessage;
            Debug.Log($" Description set");
        }

        timeRemaining = timeoutDuration;
        isWaitingForResponse = true;

        StartCoroutine(CountdownTimer());
    }

    IEnumerator CountdownTimer()
    {
        Debug.Log($" Starting countdown timer ({timeoutDuration} seconds)");

        while (timeRemaining > 0 && isWaitingForResponse)
        {
            if (timerText != null)
            {
                timerText.text = $"Time Remaining: {Mathf.Ceil(timeRemaining)}s";
            }

            timeRemaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (isWaitingForResponse)
        {
            Debug.Log(" Countdown expired - declining automatically");
            OnDeclineClicked();
        }
    }

    void OnReviveClicked()
    {
        Debug.Log("========== REVIVE BUTTON CLICKED ==========");

        isWaitingForResponse = false;

        if (GoogleAdsManager.Instance == null)
        {
            Debug.LogError(" GoogleAdsManager not found!");
            OnReviveFailed();
            return;
        }

        if (!GoogleAdsManager.Instance.IsRewardedAdReady())
        {
            Debug.LogWarning(" Rewarded ad not ready!");
            OnReviveFailed();
            return;
        }

        Debug.Log(" Hiding panel and showing ad...");
        gameObject.SetActive(false);

        GoogleAdsManager.Instance.ShowRewardedAd(OnReviveSuccess, OnReviveFailed);
    }

    void OnReviveSuccess()
    {
        Debug.Log("========== REVIVE AD COMPLETED ==========");

        if (PlayerLivesSystem.Instance != null)
        {
            PlayerLivesSystem.Instance.OnReviveAdSuccess();
            Debug.Log(" Called PlayerLivesSystem.OnReviveAdSuccess()");
        }
        else
        {
            Debug.LogError(" PlayerLivesSystem.Instance not found!");
            Time.timeScale = 1f;
        }
    }

    void OnReviveFailed()
    {
        Debug.Log("========== REVIVE AD FAILED/CANCELLED ==========");

        gameObject.SetActive(false);

        var gameOverPanel = GameObject.Find("GameOverPanel")?.GetComponent<GameOverPanel>();
        if (gameOverPanel != null)
        {
            int finalScore = 0;
            int coinsEarned = 0;

            if (PlayerLivesSystem.Instance != null)
            {
                var playerScore = PlayerLivesSystem.Instance.GetComponent<PlayerScore>();
                if (playerScore != null)
                {
                    finalScore = playerScore.Score;
                    coinsEarned = Mathf.FloorToInt(finalScore * 0.05f);
                }
            }

            Debug.Log($" Showing game over - Score: {finalScore}, Coins: {coinsEarned}");
            gameOverPanel.gameObject.SetActive(true);
            gameOverPanel.ShowDefeat(finalScore, coinsEarned);
        }
        else
        {
            Debug.LogError(" GameOverPanel not found! Returning to lobby.");
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }
    }

    void OnDeclineClicked()
    {
        Debug.Log("========== DECLINE BUTTON CLICKED ==========");

        isWaitingForResponse = false;

        gameObject.SetActive(false);

        var gameOverPanel = GameObject.Find("GameOverPanel")?.GetComponent<GameOverPanel>();
        if (gameOverPanel != null)
        {
            int finalScore = 0;
            int coinsEarned = 0;

            if (PlayerLivesSystem.Instance != null)
            {
                var playerScore = PlayerLivesSystem.Instance.GetComponent<PlayerScore>();
                if (playerScore != null)
                {
                    finalScore = playerScore.Score;
                    coinsEarned = Mathf.FloorToInt(finalScore * 0.05f);
                }
            }

            Debug.Log($" Showing game over - Score: {finalScore}, Coins: {coinsEarned}");
            gameOverPanel.gameObject.SetActive(true);
            gameOverPanel.ShowDefeat(finalScore, coinsEarned);
        }
        else
        {
            Debug.LogError(" GameOverPanel not found! Returning to lobby.");
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }
    }

    void OnDisable()
    {
        Debug.Log(" ReviveAdPanel OnDisable() called");

        isWaitingForResponse = false;

        if (!gameObject.activeInHierarchy)
        {
            var gameOverPanel = GameObject.Find("GameOverPanel");
            if (gameOverPanel == null || !gameOverPanel.activeSelf)
            {
                Time.timeScale = 1f;
                Debug.Log("Game unpaused (no other panels active)");
            }
        }
    }
}