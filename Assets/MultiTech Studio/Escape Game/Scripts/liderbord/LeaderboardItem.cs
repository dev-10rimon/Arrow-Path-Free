using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardItem : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text _rankText;
    [SerializeField] private Image _avatarImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _scoreText;

    [Header("Default Inspector Data (For Fake Players)")]
    [SerializeField] private string _playerName = "Player";
    [SerializeField] private int _initialScore = 100;
    [SerializeField] private bool _isLocalPlayer = false;

    public string PlayerName => _playerName;
    public int Score { get; private set; }
    public bool IsLocalPlayer => _isLocalPlayer;

    private void Awake()
    {
        InitializeData();
    }

    private void OnEnable()
    {
        // লিডারবোর্ড পপআপ যখনই একটিভ/ওপেন হবে, রিয়েল প্লেয়ারের ডাটা রিফ্রেশ হবে
        if (_isLocalPlayer)
        {
            RefreshLocalPlayerData();
        }
    }

    private void InitializeData()
    {
        if (_isLocalPlayer)
        {
            RefreshLocalPlayerData();
        }
        else
        {
            // ফেক প্লেয়ারদের জন্য ইনস্পেক্টরের ডাটা বসবে
            Score = _initialScore;
            UpdateUI();
        }
    }

    /// <summary>
    /// রিয়েল প্লেয়ারের নাম এবং মোট কয়েন (স্কোর) লোড করে আপডেট করে
    /// </summary>
    public void RefreshLocalPlayerData()
    {
        // ১. PlayerPrefs বা ProfileManager থেকে নাম নেওয়া
        _playerName = PlayerPrefs.GetString("PlayerName", "Player");

        // ২. PlayerPrefs থেকে সেভ হওয়া টোটাল কয়েনকে স্কোর হিসেবে নেওয়া
        Score = PlayerPrefs.GetInt("TotalCoins", 0);

        UpdateUI();
    }

    /// <summary>
    /// কোড থেকে ডাইনামিক স্কোর আপডেট করার জন্য
    /// </summary>
    public void UpdateScore(int newScore)
    {
        Score = newScore;
        if (_scoreText != null) _scoreText.text = Score.ToString();
    }

    /// <summary>
    /// র‍্যাংক পজিশন UI-তে আপডেট করার জন্য
    /// </summary>
    public void UpdateRankUI(int rank)
    {
        if (_rankText != null)
        {
            _rankText.text = rank.ToString();
        }
    }

    private void UpdateUI()
    {
        if (_nameText != null) _nameText.text = _playerName;
        if (_scoreText != null) _scoreText.text = Score.ToString();
    }
}