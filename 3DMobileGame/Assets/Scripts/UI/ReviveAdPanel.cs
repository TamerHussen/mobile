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
        Time.timeScale = 0f;
        isWaitingForResponse = true;
        timeRemaining = timeoutDuration;

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
        gameObject.SetActive(false);

        if (GoogleAdsManager.Instance == null || !GoogleAdsManager.Instance.IsRewardedAdReady())
        {
            Debug.LogError(" GoogleAdsManager not found!");
            OnReviveFailed();
            return;
        }

        Debug.Log(" Hiding panel and showing ad...");
        Time.timeScale = 1.0f;

        if (!GoogleAdsManager.Instance.IsRewardedAdReady())
        {
            GoogleAdsManager.Instance.LoadRewardedAd();
            StartCoroutine(WaitAndShowAd());
        }
        else
        {
            GoogleAdsManager.Instance.ShowRewardedAd(PlayerLivesSystem.Instance.OnReviveAdSuccess, OnReviveFailed);
        }

        IEnumerator WaitAndShowAd()
        {
            while (!GoogleAdsManager.Instance.IsRewardedAdReady())
                yield return new WaitForSeconds(0.5f);

            GoogleAdsManager.Instance.ShowRewardedAd(PlayerLivesSystem.Instance.OnReviveAdSuccess, OnReviveFailed);
        }

    }

    void OnReviveFailed()
    {
        Debug.Log("========== REVIVE AD FAILED/CANCELLED ==========");

        isWaitingForResponse = false;
        gameObject.SetActive(false);

        Time.timeScale = 1f;

        if (PlayerLivesSystem.Instance != null)
            PlayerLivesSystem.Instance.ForceGameOver();
        else
            Debug.LogError("PlayerLivesSystem missing");
    }

    void OnDeclineClicked()
    {
        Debug.Log("========== DECLINE BUTTON CLICKED ==========");

        isWaitingForResponse = false;

        gameObject.SetActive(false);

        Time.timeScale = 1f;

        if (PlayerLivesSystem.Instance != null)
            PlayerLivesSystem.Instance.ForceGameOver();
        else
            Debug.LogError("PlayerLivesSystem missing");
    }

    void OnDisable()
    {
        Debug.Log(" ReviveAdPanel OnDisable() called");

        isWaitingForResponse = false;

        if (Time.timeScale == 0f)
            Time.timeScale = 1f;
    }
}