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

    private float checkTimer = 0f;
    private int adsWatchedThisSession = 0;
    private const int MAX_ADS_PER_SESSION = 5;
    private bool isProcessingAd = false;

    void Start()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnWatchAdClicked);

        LoadAdWatchCount();
        UpdateButtonState();
    }

    void OnEnable()
    {
        isProcessingAd = false;
        checkTimer = 0f;
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
        if (isProcessingAd)
        {
            Debug.Log("Already processing an ad, wait");
            return;
        }

        if (adsWatchedThisSession >= MAX_ADS_PER_SESSION)
        {
            Debug.Log("Max ads reached for this session");
            if (buttonText != null)
                buttonText.text = "Max Ads Reached";
            return;
        }

        if (GoogleAdsManager.Instance != null && GoogleAdsManager.Instance.IsRewardedAdReady())
        {
            isProcessingAd = true;

            if (buttonText != null)
                buttonText.text = "Loading Ad...";

            GoogleAdsManager.Instance.ShowRewardedAd(
                () => OnAdRewarded(),
                () => OnAdFailed()
            );
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
        if (this == null || !gameObject.activeInHierarchy)
            return;

        isProcessingAd = false;
        adsWatchedThisSession++;
        SaveAdWatchCount();

        Debug.Log($"Ad watched! ({adsWatchedThisSession}/{MAX_ADS_PER_SESSION})");

        UpdateButtonState();
    }

    void OnAdFailed()
    {
        if (this == null || !gameObject.activeInHierarchy)
            return;

        isProcessingAd = false;
        Debug.LogWarning("Ad failed to load or was closed");
        UpdateButtonState();
    }

    void UpdateButtonState()
    {
        if (button == null || buttonText == null)
            return;

        if (GoogleAdsManager.Instance == null)
        {
            button.interactable = false;
            buttonText.text = "Ads Not Available";
            return;
        }

        if (adsWatchedThisSession >= MAX_ADS_PER_SESSION)
        {
            button.interactable = false;
            buttonText.text = $"Max Ads ({MAX_ADS_PER_SESSION}/{MAX_ADS_PER_SESSION})";
            if (rewardText != null)
                rewardText.text = "Come back later!";
            return;
        }

        bool isReady = GoogleAdsManager.Instance.IsRewardedAdReady();

        // don't allow clicking while processing
        button.interactable = isReady && !isProcessingAd;

        if (isProcessingAd)
        {
            buttonText.text = "Loading...";
        }
        else
        {
            buttonText.text = isReady ? "Watch Ad" : "Loading...";
        }

        if (rewardText != null && isReady && !isProcessingAd)
        {
            int remaining = MAX_ADS_PER_SESSION - adsWatchedThisSession;
            int rewardAmount = GoogleAdsManager.Instance.coinsPerAd;
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
        PlayerPrefs.SetInt("AdsWatchedThisSession", 0);
        PlayerPrefs.Save();
    }

    void OnDisable()
    {
        isProcessingAd = false;
    }

    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveAllListeners();
    }
}