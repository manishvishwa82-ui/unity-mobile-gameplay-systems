using System;
using UnityEngine;
using TMPro;

public class LifeManager : MonoBehaviour
{
    public static LifeManager instance;

    [Header("UI Reference")]
    public TextMeshProUGUI heartText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI coinText;

    [Header("Life Settings")]
    public int maxHearts = 5;
    public int refillMinutes = 15;
    public int heartCostInCoins = 100;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnEnable()
    {
        PlayerDataManager.OnCoinsChanged += OnCoinsUpdated;
        PlayerDataManager.OnHeartsChanged += OnHeartsUpdated;
    }

    void OnDisable()
    {
        PlayerDataManager.OnCoinsChanged -= OnCoinsUpdated;
        PlayerDataManager.OnHeartsChanged -= OnHeartsUpdated;
    }

    void Start()
    {
        CheckOfflineRefill();
        UpdateUI();
    }

    void Update()
    {
        int currentHearts = PlayerDataManager.GetHearts();
        if (currentHearts < maxHearts)
        {
            UpdateRefillTimer();
        }
        else
        {
            if (timerText != null) timerText.text = "FULL";
        }
    }

    public int GetHearts() => PlayerDataManager.GetHearts();

    public bool ConsumeHeart()
    {
        int hearts = PlayerDataManager.GetHearts();
        if (hearts > 0)
        {
            PlayerDataManager.ConsumeHeart();

            if (hearts == maxHearts)
            {
                DateTime nextTime = DateTime.UtcNow.AddMinutes(refillMinutes);
                PlayerDataManager.SetRefillTimeString(nextTime.ToBinary().ToString());
            }

            UpdateUI();
            return true;
        }
        return false;
    }

    public bool ConsumeHeartOnGameOver() => ConsumeHeart();

    private void CheckOfflineRefill()
    {
        int hearts = PlayerDataManager.GetHearts();
        if (hearts >= maxHearts) return;

        string savedTimeStr = PlayerDataManager.GetRefillTimeString();
        if (string.IsNullOrEmpty(savedTimeStr)) return;

        // क्रैश से बचने के लिए TryParse
        if (long.TryParse(savedTimeStr, out long temp))
        {
            DateTime nextRefillTime = DateTime.FromBinary(temp);

            while (DateTime.UtcNow >= nextRefillTime && hearts < maxHearts)
            {
                hearts++;
                nextRefillTime = nextRefillTime.AddMinutes(refillMinutes);
            }

            PlayerDataManager.SetHearts(hearts);

            if (hearts < maxHearts)
            {
                PlayerDataManager.SetRefillTimeString(nextRefillTime.ToBinary().ToString());
            }
            else
            {
                PlayerDataManager.ClearRefillTime();
            }
        }
        else
        {
            PlayerDataManager.ClearRefillTime();
        }
    }

    private void UpdateRefillTimer()
    {
        string savedTimeStr = PlayerDataManager.GetRefillTimeString();
        if (string.IsNullOrEmpty(savedTimeStr)) return;

        if (long.TryParse(savedTimeStr, out long temp))
        {
            DateTime nextRefillTime = DateTime.FromBinary(temp);
            TimeSpan remaining = nextRefillTime - DateTime.UtcNow;

            if (remaining.TotalSeconds <= 0)
            {
                int hearts = PlayerDataManager.GetHearts() + 1;
                PlayerDataManager.SetHearts(hearts);

                if (hearts < maxHearts)
                {
                    DateTime nextTime = DateTime.UtcNow.AddMinutes(refillMinutes);
                    PlayerDataManager.SetRefillTimeString(nextTime.ToBinary().ToString());
                }
                else
                {
                    PlayerDataManager.ClearRefillTime();
                }

                UpdateUI();
            }
            else
            {
                if (timerText != null)
                {
                    timerText.text = string.Format("{0:D2}:{1:D2}", remaining.Minutes, remaining.Seconds);
                }
            }
        }
        else
        {
            PlayerDataManager.ClearRefillTime();
        }
    }

    public bool BuyHeartWithCoins()
    {
        if (PlayerDataManager.GetHearts() >= maxHearts) return false;

        if (PlayerDataManager.SpendCoins(heartCostInCoins))
        {
            PlayerDataManager.AddHeart(1);
            if (PlayerDataManager.GetHearts() >= maxHearts)
            {
                PlayerDataManager.ClearRefillTime();
            }
            UpdateUI();
            return true;
        }
        return false;
    }

    private void OnCoinsUpdated(int c) => UpdateUI();
    private void OnHeartsUpdated(int h) => UpdateUI();

    public void UpdateUI()
    {
        if (heartText != null) heartText.text = "x" + PlayerDataManager.GetHearts();
        if (coinText != null) coinText.text = "x" + PlayerDataManager.GetCoins();
    }
}