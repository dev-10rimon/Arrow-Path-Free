using UnityEngine;
using UnityEngine.UI;

public class FirstTimePopup : MonoBehaviour
{
    [Header("UI Element")]
    [Tooltip("প্রথমবার দেখানোর জন্য পপআপ বা উইন্ডো অবজেক্টটি এখানে দিন")]
    [SerializeField] private GameObject _popupPanel;

    [Header("Accept Button")]
    [Tooltip("Accept বা Agree বাটনটি এখানে দিন")]
    [SerializeField] private Button _acceptButton;

    [Header("Save Settings")]
    [Tooltip("সেভ করে রাখার জন্য একটি ইউনিক Key নাম")]
    [SerializeField] private string _saveKey = "HasAcceptedTerms";

    private void Start()
    {
        // আগে সেভ করা আছে কি না চেক করব (0 মানে আগে Accept করে নাই)
        bool hasAccepted = PlayerPrefs.GetInt(_saveKey, 0) == 1;

        if (hasAccepted)
        {
            // যদি আগেই Accept করা থাকে, তবে পপআপ হাইড থাকবে এবং সাউন্ড অন থাকবে
            if (_popupPanel != null)
            {
                _popupPanel.SetActive(false);
            }
            AudioListener.pause = false;
        }
        else
        {
            // যদি প্রথমবার গেম ওপেন করে, তবে পপআপ শো করবে
            if (_popupPanel != null)
            {
                _popupPanel.SetActive(true);
            }

            // পপআপ স্ক্রিন থাকা অবস্থায় গেমের সব সাউন্ড বন্ধ বা পজ থাকবে
            AudioListener.pause = true;

            // Accept বাটনে লিসেনার যুক্ত করা
            if (_acceptButton != null)
            {
                _acceptButton.onClick.AddListener(OnAcceptButtonClicked);
            }
        }
    }

    /// <summary>
    /// Accept বাটনে চাপ দিলে এই মেথডটি কাজ করবে
    /// </summary>
    private void OnAcceptButtonClicked()
    {
        // PlayerPrefs-এ সেভ করে রাখা হলো যে Accept করা হয়ে গেছে (1 = True)
        PlayerPrefs.SetInt(_saveKey, 1);
        PlayerPrefs.Save();

        // পপআপ অবজেক্টটি হাইড/ফলস করা হলো
        if (_popupPanel != null)
        {
            _popupPanel.SetActive(false);
        }

        // Accept করা মাত্রই গেমের সাউন্ড আবার চালু/আনপজ করে দেওয়া হলো
        AudioListener.pause = false;

        // লিসেনার রিমুভ করে দেওয়া ভালো মেমোরি ক্লিন রাখার জন্য
        if (_acceptButton != null)
        {
            _acceptButton.onClick.RemoveListener(OnAcceptButtonClicked);
        }
    }
}