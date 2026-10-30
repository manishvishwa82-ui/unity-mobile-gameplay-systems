using System;
using UnityEngine;
using GoogleMobileAds.Api;

public class AdMobManager : MonoBehaviour
{
    public static AdMobManager instance;

    // Real AdMob Unit IDs (Android)
    private readonly string bannerAdUnitId = "ca-app-pub-9333282110704779/3176913317";
    private readonly string interstitialAdUnitId = "ca-app-pub-9333282110704779/2689470330";
    private readonly string rewardedAdUnitId = "ca-app-pub-9333282110704779/8237668306";

    private BannerView bannerView;
    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;

    private Action onRewardedSuccessCallback;
    private Action onRewardedFailCallback;
    private bool rewardEarned = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Input.multiTouchEnabled = false; // Phase 6: Multi-touch glitch guard
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("Google Mobile Ads Initialized Successfully.");
            LoadBannerAd();
            LoadInterstitialAd();
            LoadRewardedAd();
        });
    }

    #region BANNER ADS
    public void LoadBannerAd()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }

        bannerView = new BannerView(bannerAdUnitId, AdSize.Banner, AdPosition.Bottom);
        AdRequest request = new AdRequest();
        bannerView.LoadAd(request);
    }

    public void HideBanner()
    {
        if (bannerView != null) bannerView.Hide();
    }

    public void ShowBanner()
    {
        if (bannerView != null) bannerView.Show();
    }
    #endregion

    #region INTERSTITIAL ADS
    public void LoadInterstitialAd()
    {
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        AdRequest request = new AdRequest();
        InterstitialAd.Load(interstitialAdUnitId, request, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("Interstitial ad load failed: " + error);
                return;
            }

            interstitialAd = ad;

            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                LoadInterstitialAd();
            };
            interstitialAd.OnAdFullScreenContentFailed += (AdError adError) =>
            {
                LoadInterstitialAd();
            };
        });
    }

    public void ShowInterstitialAd()
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.Show();
        }
        else
        {
            LoadInterstitialAd();
        }
    }
    #endregion

    #region REWARDED ADS
    public void LoadRewardedAd()
    {
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        AdRequest request = new AdRequest();
        RewardedAd.Load(rewardedAdUnitId, request, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("Rewarded ad load failed: " + error);
                return;
            }

            rewardedAd = ad;

            rewardedAd.OnAdFullScreenContentClosed += () =>
            {
                if (rewardEarned)
                {
                    onRewardedSuccessCallback?.Invoke();
                }
                else
                {
                    onRewardedFailCallback?.Invoke();
                }

                rewardEarned = false;
                onRewardedSuccessCallback = null;
                onRewardedFailCallback = null;
                LoadRewardedAd();
            };

            rewardedAd.OnAdFullScreenContentFailed += (AdError adError) =>
            {
                rewardEarned = false;
                onRewardedFailCallback?.Invoke();
                onRewardedSuccessCallback = null;
                onRewardedFailCallback = null;
                LoadRewardedAd();
            };
        });
    }

    public void ShowRewardedAd(Action onSuccess, Action onFail = null)
    {
        onRewardedSuccessCallback = onSuccess;
        onRewardedFailCallback = onFail;
        rewardEarned = false;

        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.Show((Reward reward) =>
            {
                // Callback tab mark hoga jab ad actual user ne pura dekha
                rewardEarned = true;
            });
        }
        else
        {
            Debug.LogWarning("Rewarded ad not ready yet. Retrying fetch...");
            onRewardedFailCallback?.Invoke();
            onRewardedSuccessCallback = null;
            onRewardedFailCallback = null;
            LoadRewardedAd();
        }
    }

    public bool IsRewardedAdReady()
    {
        return rewardedAd != null && rewardedAd.CanShowAd();
    }
    #endregion
}