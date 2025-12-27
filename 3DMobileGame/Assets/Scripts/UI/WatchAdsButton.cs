using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WatchAdButton : MonoBehaviour
{
    [Header("Components")]
    public Button button;
    public TextMeshProUGUI buttonText;
    public TextMeshProUGUI rewardText;

    [Header("Settings")]
    public float checkInterval = 1f;
    public int rewardAmount = 50;

    private float checkTimer = 0f;
    private int adsWatchedThisSession = 0;
    private const int MAX_ADS_PER_SESSION = 5;

    void Start()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(OnWatchAdClicked);
        LoadAdWatchCount();
        UpdateButtonState();
    }

    void Update()
    {
        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            UpdateButtonState();
        }
    }

    void OnWatchAdClicked()
    {
        if (adsWatchedThisSession >= MAX_ADS_PER_SESSION)
        {
            Debug.Log("Max ads reached for this session");
            if (buttonText != null)
                buttonText.text = "Max Ads Reached";
            return;
        }

        if (GoogleAdsManager.Instance != null && GoogleAdsManager.Instance.IsRewardedAdReady())
        {
            GoogleAdsManager.Instance.ShowRewardedAd(OnAdRewarded, OnAdFailed);

            if (buttonText != null)
                buttonText.text = "Loading Ad...";
        }
        else
        {
            Debug.Log("Ad not ready, loading...");
            if (buttonText != null)
                buttonText.text = "Loading Ad...";

            GoogleAdsManager.Instance?.LoadRewardedAd();
        }
    }

    void OnAdRewarded()
    {
        adsWatchedThisSession++;
        SaveAdWatchCount();

        if (CoinsManager.Instance != null)
        {
            CoinsManager.Instance.AddCoins(rewardAmount);
            Debug.Log($"✅ Rewarded {rewardAmount} coins! ({adsWatchedThisSession}/{MAX_ADS_PER_SESSION} ads watched)");
        }

        UpdateButtonState();
    }

    void OnAdFailed()
    {
        Debug.LogWarning("Ad failed to load or was closed");
        UpdateButtonState();
    }

    void UpdateButtonState()
    {
        if (GoogleAdsManager.Instance == null)
        {
            button.interactable = false;
            if (buttonText != null)
                buttonText.text = "Ads Not Available";
            return;
        }

        if (adsWatchedThisSession >= MAX_ADS_PER_SESSION)
        {
            button.interactable = false;
            if (buttonText != null)
                buttonText.text = $"Max Ads ({MAX_ADS_PER_SESSION}/{MAX_ADS_PER_SESSION})";
            if (rewardText != null)
                rewardText.text = "Come back later!";
            return;
        }

        bool isReady = GoogleAdsManager.Instance.IsRewardedAdReady();
        button.interactable = isReady;

        if (buttonText != null)
        {
            buttonText.text = isReady ? "Watch Ad" : "Loading...";
        }

        if (rewardText != null && isReady)
        {
            int remaining = MAX_ADS_PER_SESSION - adsWatchedThisSession;
            rewardText.text = $"+{rewardAmount} Coins ({remaining} left)";
        }
    }

    void LoadAdWatchCount()
    {
        adsWatchedThisSession = PlayerPrefs.GetInt("AdsWatchedThisSession", 0);
    }

    void SaveAdWatchCount()
    {
        PlayerPrefs.SetInt("AdsWatchedThisSession", adsWatchedThisSession);
        PlayerPrefs.Save();
    }

    void OnApplicationQuit()
    {
        // Reset ad counter when game closes
        PlayerPrefs.SetInt("AdsWatchedThisSession", 0);
        PlayerPrefs.Save();
    }
}