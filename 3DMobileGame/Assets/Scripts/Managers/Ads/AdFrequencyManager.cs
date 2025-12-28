using UnityEngine;

public class AdFrequencyManager : MonoBehaviour
{
    public static AdFrequencyManager Instance;

    [Header("Interstitial Ad Settings")]
    [Tooltip("Minimum seconds between interstitial ads")]
    public float minTimeBetweenInterstitials = 180f;

    [Tooltip("Show interstitial every X deaths")]
    public int deathsPerInterstitial = 3;

    [Tooltip("Show interstitial every X levels completed")]
    public int levelsPerInterstitial = 2;

    [Header("Tracking")]
    private float lastInterstitialTime = 0f;
    private int deathCount = 0;
    private int levelCompletionCount = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnPlayerDeath()
    {
        deathCount++;
        Debug.Log($"Player deaths: {deathCount}");

        if (ShouldShowInterstitialAfterDeath())
        {
            ShowInterstitialAd();
        }
    }

    public void OnLevelComplete()
    {
        levelCompletionCount++;
        Debug.Log($"Levels completed: {levelCompletionCount}");

        if (ShouldShowInterstitialAfterLevel())
        {
            ShowInterstitialAd();
        }
    }

    bool EnoughTimePassed()
    {
        return (Time.time - lastInterstitialTime) >= minTimeBetweenInterstitials;
    }

    bool ShouldShowInterstitialAfterDeath()
    {
        if (!EnoughTimePassed())
        {
            Debug.Log("Not enough time passed since last ad");
            return false;
        }

        if (deathCount % deathsPerInterstitial == 0)
        {
            Debug.Log("Death threshold reached - showing ad");
            return true;
        }

        return false;
    }

    bool ShouldShowInterstitialAfterLevel()
    {
        if (!EnoughTimePassed())
        {
            Debug.Log("Not enough time passed since last ad");
            return false;
        }

        if (levelCompletionCount % levelsPerInterstitial == 0)
        {
            Debug.Log("Level threshold reached - showing ad");
            return true;
        }

        return false;
    }

    void ShowInterstitialAd()
    {
        if (GoogleAdsManager.Instance == null)
        {
            Debug.LogError("GoogleAdsManager not found!");
            return;
        }

        if (GoogleAdsManager.Instance.IsInterstitialAdReady())
        {
            GoogleAdsManager.Instance.ShowInterstitialAd();
            lastInterstitialTime = Time.time;
            Debug.Log($" Showed interstitial ad. Next possible in {minTimeBetweenInterstitials}s");
        }
        else
        {
            Debug.Log("Interstitial ad not ready yet");
        }
    }

    public void ForceShowInterstitial()
    {
        if (GoogleAdsManager.Instance != null)
        {
            GoogleAdsManager.Instance.ShowInterstitialAd();
            lastInterstitialTime = Time.time;
        }
    }

    public void ResetCounters()
    {
        deathCount = 0;
        levelCompletionCount = 0;
        lastInterstitialTime = 0f;
        Debug.Log("Ad frequency counters reset");
    }

    public float TimeUntilNextInterstitial()
    {
        float elapsed = Time.time - lastInterstitialTime;
        float remaining = Mathf.Max(0, minTimeBetweenInterstitials - elapsed);
        return remaining;
    }
    public bool CanShowInterstitialNow()
    {
        return EnoughTimePassed();
    }
}