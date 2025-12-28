using System;
using UnityEngine;
using GoogleMobileAds.Api;

public class GoogleAdsManager : MonoBehaviour
{
    public static GoogleAdsManager Instance;

    [Header("Ad Settings")]
    [Tooltip("Coins awarded for watching a rewarded ad")]
    public int coinsPerAd = 50;

    [Header("Ad Unit IDs - Android")]
#if UNITY_ANDROID
    public string androidRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
    public string androidInterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
    public string androidBannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";
#endif

    [Header("Ad Unit IDs - iOS")]
#if UNITY_IOS
    public string iosRewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";
    public string iosInterstitialAdUnitId = "ca-app-pub-3940256099942544/4411468910";
    public string iosBannerAdUnitId = "ca-app-pub-3940256099942544/2934735716";
#endif

    [Header("Ad Behavior")]
    public bool autoReloadAds = true;
    public bool useBannerAds = false;
    public AdPosition bannerPosition = AdPosition.Bottom;

    private RewardedAd rewardedAd;
    private InterstitialAd interstitialAd;
    private BannerView bannerView;

    private bool isRewardedAdLoaded = false;
    private bool isInterstitialAdLoaded = false;
    private bool isBannerAdLoaded = false;
    private bool isLoadingRewardedAd = false;
    private bool isLoadingInterstitialAd = false;
    private bool isInitialized = false;

    private bool rewardAlreadyGiven = false;

    private Action onRewardedSuccess;
    private Action onRewardedFailed;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Screen.orientation = ScreenOrientation.LandscapeRight;

    }

    void Start()
    {
        InitializeAds();
    }

    void InitializeAds()
    {
        if (isInitialized)
        {
            Debug.LogWarning("Google Ads already initialized");
            return;
        }

        Debug.Log("Initializing Google Mobile Ads SDK...");

        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            if (initStatus == null)
            {
                Debug.LogError("Google Mobile Ads initialization failed!");
                return;
            }

            isInitialized = true;
            Debug.Log(" Google Mobile Ads initialized successfully");

            LoadRewardedAd();
            LoadInterstitialAd();

            if (useBannerAds)
            {
                CreateBannerView();
            }
        });
    }

    #region REWARDED ADS

    public void LoadRewardedAd()
    {
        if (isLoadingRewardedAd)
        {
            Debug.Log("Already loading a rewarded ad");
            return;
        }

        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        isLoadingRewardedAd = true;
        isRewardedAdLoaded = false;
        rewardAlreadyGiven = false; 

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

            Debug.Log(" Rewarded ad loaded successfully");
            rewardedAd = ad;
            isRewardedAdLoaded = true;

            RegisterRewardedAdEvents(ad);
        });
    }

    public void ShowRewardedAd(Action onSuccess, Action onFail)
    {
        onRewardedSuccess = onSuccess;
        onRewardedFailed = onFail;
        rewardAlreadyGiven = false;

        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            Debug.Log("Showing rewarded ad...");

            rewardedAd.Show((Reward reward) =>
            {
                if (!rewardAlreadyGiven)
                {
                    rewardAlreadyGiven = true;

                    Debug.Log($" User earned reward: {reward.Amount} {reward.Type}");

                    if (CoinsManager.Instance != null)
                    {
                        CoinsManager.Instance.AddCoins(coinsPerAd);
                        Debug.Log($" Awarded {coinsPerAd} coins via GoogleAdsManager!");
                    }
                    else
                    {
                        Debug.LogError("CoinsManager not found! Cannot award coins.");
                    }

                    onRewardedSuccess?.Invoke();
                }
                else
                {
                    Debug.LogWarning(" Reward already given for this ad!");
                }
            });
        }
        else
        {
            Debug.LogWarning("Rewarded ad not ready yet!");
            onRewardedFailed?.Invoke();

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

            if (autoReloadAds)
            {
                LoadRewardedAd();
            }

            onRewardedSuccess = null;
            onRewardedFailed = null;
        };

        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError($"Rewarded ad failed to show: {error}");
            isRewardedAdLoaded = false;

            onRewardedFailed?.Invoke();

            if (autoReloadAds)
            {
                LoadRewardedAd();
            }

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

    public void LoadInterstitialAd()
    {
        if (isLoadingInterstitialAd)
        {
            Debug.Log("Already loading an interstitial ad");
            return;
        }

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

            Debug.Log(" Interstitial ad loaded successfully");
            interstitialAd = ad;
            isInterstitialAdLoaded = true;

            RegisterInterstitialAdEvents(ad);
        });
    }

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

    public void CreateBannerView()
    {
        Debug.Log("Creating banner view...");

        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }

        string adUnitId = GetBannerAdUnitId();
        bannerView = new BannerView(adUnitId, AdSize.Banner, bannerPosition);

        RegisterBannerAdEvents();
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
            Debug.Log(" Banner ad loaded");
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

    public bool IsAdReady()
    {
        return IsRewardedAdReady();
    }

    #endregion

    void OnDestroy()
    {
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
