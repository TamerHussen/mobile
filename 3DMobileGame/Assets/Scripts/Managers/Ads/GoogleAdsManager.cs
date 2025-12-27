using System;
using UnityEngine;
using GoogleMobileAds.Api;

/// <summary>
/// Comprehensive Google Ads Manager for Banner, Interstitial, and Rewarded ads.
/// FIXED VERSION - Resolves callback issues and adds proper reward handling
/// Place on a DontDestroyOnLoad GameObject in your MainMenu scene.
/// </summary>
public class GoogleAdsManager : MonoBehaviour
{
    public static GoogleAdsManager Instance;

    [Header("Ad Settings")]
    [Tooltip("Coins awarded for watching a rewarded ad")]
    public int coinsPerAd = 50;

    [Header("Ad Unit IDs - Android")]
#if UNITY_ANDROID
    [Tooltip("Android Rewarded Ad Unit ID")]
    public string androidRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917"; // Test ID
    [Tooltip("Android Interstitial Ad Unit ID")]
    public string androidInterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712"; // Test ID
    [Tooltip("Android Banner Ad Unit ID")]
    public string androidBannerAdUnitId = "ca-app-pub-3940256099942544/6300978111"; // Test ID
#endif

    [Header("Ad Unit IDs - iOS")]
#if UNITY_IOS
    [Tooltip("iOS Rewarded Ad Unit ID")]
    public string iosRewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313"; // Test ID
    [Tooltip("iOS Interstitial Ad Unit ID")]
    public string iosInterstitialAdUnitId = "ca-app-pub-3940256099942544/4411468910"; // Test ID
    [Tooltip("iOS Banner Ad Unit ID")]
    public string iosBannerAdUnitId = "ca-app-pub-3940256099942544/2934735716"; // Test ID
#endif

    [Header("Ad Behavior")]
    [Tooltip("Automatically load next ad after showing one")]
    public bool autoReloadAds = true;
    [Tooltip("Show banner ads")]
    public bool useBannerAds = false;
    [Tooltip("Banner position")]
    public AdPosition bannerPosition = AdPosition.Bottom;

    // Ad instances
    private RewardedAd rewardedAd;
    private InterstitialAd interstitialAd;
    private BannerView bannerView;

    // Ad state tracking
    private bool isRewardedAdLoaded = false;
    private bool isInterstitialAdLoaded = false;
    private bool isBannerAdLoaded = false;
    private bool isLoadingRewardedAd = false;
    private bool isLoadingInterstitialAd = false;
    private bool isInitialized = false;

    // CRITICAL FIX: Store callbacks for rewarded ads
    private Action onRewardedSuccess;
    private Action onRewardedFailed;

    void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        InitializeAds();
    }

    /// <summary>
    /// Initialize Google Mobile Ads SDK
    /// </summary>
    void InitializeAds()
    {
        if (isInitialized)
        {
            Debug.LogWarning("Google Ads already initialized");
            return;
        }

        Debug.Log("Initializing Google Mobile Ads SDK...");

        // Initialize the Google Mobile Ads SDK
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            if (initStatus == null)
            {
                Debug.LogError("Google Mobile Ads initialization failed!");
                return;
            }

            // Log adapter statuses
            var adapterStatusMap = initStatus.getAdapterStatusMap();
            if (adapterStatusMap != null)
            {
                foreach (var adapter in adapterStatusMap)
                {
                    Debug.Log($"Adapter: {adapter.Key}, State: {adapter.Value.InitializationState}");
                }
            }

            isInitialized = true;
            Debug.Log("✅ Google Mobile Ads initialized successfully");

            // Load initial ads
            LoadRewardedAd();
            LoadInterstitialAd();

            if (useBannerAds)
            {
                CreateBannerView();
            }
        });
    }

    #region REWARDED ADS

    /// <summary>
    /// Load a rewarded ad
    /// </summary>
    public void LoadRewardedAd()
    {
        if (isLoadingRewardedAd)
        {
            Debug.Log("Already loading a rewarded ad");
            return;
        }

        // Clean up old ad
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        isLoadingRewardedAd = true;
        isRewardedAdLoaded = false;

        Debug.Log("Loading rewarded ad...");

        string adUnitId = GetRewardedAdUnitId();
        var adRequest = new AdRequest();

        RewardedAd.Load(adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            isLoadingRewardedAd = false;

            if (error != null || ad == null)
            {
                Debug.LogError($"Rewarded ad failed to load: {error}");
                return;
            }

            Debug.Log("✅ Rewarded ad loaded successfully");
            rewardedAd = ad;
            isRewardedAdLoaded = true;

            RegisterRewardedAdEvents(ad);
        });
    }

    /// <summary>
    /// Show the rewarded ad (LEGACY - for backward compatibility)
    /// Use ShowRewardedAd(onSuccess, onFail) for better control
    /// </summary>
    public void ShowRewardedAd()
    {
        ShowRewardedAd(null, null);
    }

    /// <summary>
    /// Show the rewarded ad with callbacks (RECOMMENDED)
    /// </summary>
    /// <param name="onSuccess">Called when user completes the ad</param>
    /// <param name="onFail">Called if ad fails to show</param>
    public void ShowRewardedAd(Action onSuccess, Action onFail)
    {
        // CRITICAL FIX: Store callbacks
        onRewardedSuccess = onSuccess;
        onRewardedFailed = onFail;

        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            Debug.Log("Showing rewarded ad...");

            rewardedAd.Show((Reward reward) =>
            {
                Debug.Log($"✅ User earned reward: {reward.Amount} {reward.Type}");

                // Award coins to player
                if (CoinsManager.Instance != null)
                {
                    CoinsManager.Instance.AddCoins(coinsPerAd);
                    Debug.Log($"Awarded {coinsPerAd} coins!");
                }
                else
                {
                    Debug.LogError("CoinsManager not found! Cannot award coins.");
                }

                // CRITICAL FIX: Call success callback
                onRewardedSuccess?.Invoke();
            });
        }
        else
        {
            Debug.LogWarning("Rewarded ad not ready yet!");

            // CRITICAL FIX: Call failure callback
            onRewardedFailed?.Invoke();

            // Try to load if not already loading
            if (!isLoadingRewardedAd)
            {
                LoadRewardedAd();
            }
        }
    }

    void RegisterRewardedAdEvents(RewardedAd ad)
    {
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log($"Rewarded ad paid: {adValue.Value} {adValue.CurrencyCode}");
        };

        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Rewarded ad impression recorded");
        };

        ad.OnAdClicked += () =>
        {
            Debug.Log("Rewarded ad clicked");
        };

        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Rewarded ad opened");
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Rewarded ad closed");
            isRewardedAdLoaded = false;

            // Reload next ad
            if (autoReloadAds)
            {
                LoadRewardedAd();
            }

            // CRITICAL FIX: Clear callbacks after use
            onRewardedSuccess = null;
            onRewardedFailed = null;
        };

        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError($"Rewarded ad failed to show: {error}");
            isRewardedAdLoaded = false;

            // CRITICAL FIX: Call failure callback
            onRewardedFailed?.Invoke();

            // Reload ad
            if (autoReloadAds)
            {
                LoadRewardedAd();
            }

            // Clear callbacks
            onRewardedSuccess = null;
            onRewardedFailed = null;
        };
    }

    public bool IsRewardedAdReady()
    {
        return isRewardedAdLoaded && rewardedAd != null && rewardedAd.CanShowAd();
    }

    #endregion

    #region INTERSTITIAL ADS

    /// <summary>
    /// Load an interstitial ad
    /// </summary>
    public void LoadInterstitialAd()
    {
        if (isLoadingInterstitialAd)
        {
            Debug.Log("Already loading an interstitial ad");
            return;
        }

        // Clean up old ad
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        isLoadingInterstitialAd = true;
        isInterstitialAdLoaded = false;

        Debug.Log("Loading interstitial ad...");

        string adUnitId = GetInterstitialAdUnitId();
        var adRequest = new AdRequest();

        InterstitialAd.Load(adUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
        {
            isLoadingInterstitialAd = false;

            if (error != null || ad == null)
            {
                Debug.LogError($"Interstitial ad failed to load: {error}");
                return;
            }

            Debug.Log("✅ Interstitial ad loaded successfully");
            interstitialAd = ad;
            isInterstitialAdLoaded = true;

            RegisterInterstitialAdEvents(ad);
        });
    }

    /// <summary>
    /// Show the interstitial ad
    /// </summary>
    public void ShowInterstitialAd()
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            Debug.Log("Showing interstitial ad...");
            interstitialAd.Show();
        }
        else
        {
            Debug.LogWarning("Interstitial ad not ready yet!");

            if (!isLoadingInterstitialAd)
            {
                LoadInterstitialAd();
            }
        }
    }

    void RegisterInterstitialAdEvents(InterstitialAd ad)
    {
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log($"Interstitial ad paid: {adValue.Value} {adValue.CurrencyCode}");
        };

        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Interstitial ad impression recorded");
        };

        ad.OnAdClicked += () =>
        {
            Debug.Log("Interstitial ad clicked");
        };

        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Interstitial ad opened");
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Interstitial ad closed");
            isInterstitialAdLoaded = false;

            if (autoReloadAds)
            {
                LoadInterstitialAd();
            }
        };

        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError($"Interstitial ad failed to show: {error}");
            isInterstitialAdLoaded = false;

            if (autoReloadAds)
            {
                LoadInterstitialAd();
            }
        };
    }

    public bool IsInterstitialAdReady()
    {
        return isInterstitialAdLoaded && interstitialAd != null && interstitialAd.CanShowAd();
    }

    #endregion

    #region BANNER ADS

    /// <summary>
    /// Create and load a banner ad
    /// </summary>
    public void CreateBannerView()
    {
        Debug.Log("Creating banner view...");

        // Destroy old banner if exists
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }

        string adUnitId = GetBannerAdUnitId();

        // Create banner at specified position
        bannerView = new BannerView(adUnitId, AdSize.Banner, bannerPosition);

        RegisterBannerAdEvents();

        // Load the banner
        LoadBannerAd();
    }

    void LoadBannerAd()
    {
        if (bannerView == null)
        {
            CreateBannerView();
            return;
        }

        Debug.Log("Loading banner ad...");

        var adRequest = new AdRequest();
        bannerView.LoadAd(adRequest);
    }

    void RegisterBannerAdEvents()
    {
        bannerView.OnBannerAdLoaded += () =>
        {
            Debug.Log("✅ Banner ad loaded");
            isBannerAdLoaded = true;
            ShowBannerAd();
        };

        bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            Debug.LogError($"Banner ad failed to load: {error}");
            isBannerAdLoaded = false;
        };

        bannerView.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log($"Banner ad paid: {adValue.Value} {adValue.CurrencyCode}");
        };

        bannerView.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Banner ad impression recorded");
        };

        bannerView.OnAdClicked += () =>
        {
            Debug.Log("Banner ad clicked");
        };

        bannerView.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Banner ad full screen opened");
        };

        bannerView.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Banner ad full screen closed");
        };
    }

    public void ShowBannerAd()
    {
        if (bannerView != null)
        {
            Debug.Log("Showing banner ad");
            bannerView.Show();
        }
    }

    public void HideBannerAd()
    {
        if (bannerView != null)
        {
            Debug.Log("Hiding banner ad");
            bannerView.Hide();
        }
    }

    public void DestroyBannerAd()
    {
        if (bannerView != null)
        {
            Debug.Log("Destroying banner ad");
            bannerView.Destroy();
            bannerView = null;
            isBannerAdLoaded = false;
        }
    }

    #endregion

    #region HELPER METHODS

    string GetRewardedAdUnitId()
    {
#if UNITY_ANDROID
        return androidRewardedAdUnitId;
#elif UNITY_IOS
        return iosRewardedAdUnitId;
#else
        return "unused";
#endif
    }

    string GetInterstitialAdUnitId()
    {
#if UNITY_ANDROID
        return androidInterstitialAdUnitId;
#elif UNITY_IOS
        return iosInterstitialAdUnitId;
#else
        return "unused";
#endif
    }

    string GetBannerAdUnitId()
    {
#if UNITY_ANDROID
        return androidBannerAdUnitId;
#elif UNITY_IOS
        return iosBannerAdUnitId;
#else
        return "unused";
#endif
    }

    // Legacy compatibility with old method name
    public bool IsAdReady()
    {
        return IsRewardedAdReady();
    }

    #endregion

    void OnDestroy()
    {
        // Clean up all ads
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
        }

        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
        }

        if (bannerView != null)
        {
            bannerView.Destroy();
        }

        Debug.Log("GoogleAdsManager destroyed and ads cleaned up");
    }
}