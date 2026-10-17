using UnityEngine;
using UnityEngine.UI;

namespace MultiTechStudio.EscapeGame
{
    public class LevelUnlockManager : MonoBehaviour
    {
        [Header("Target References")]
        [Tooltip("যে Game Object টি হাইড (false) করতে চান তা এখানে ড্র্যাগ করুন")]
        [SerializeField] private GameObject _objectToHide;

        [Tooltip("যে Button টির interactable true করতে চান তা এখানে ড্র্যাগ করুন")]
        [SerializeField] private Button _buttonToEnable;

        [Header("Condition Settings")]
        [Tooltip("কত নম্বর লেভেল পার হলে বা পৌঁছালে এটি আনলক হবে (যেমন: 20)")]
        [SerializeField] private int _targetLevelIndex = 20;

        private void Start()
        {
            CheckUnlockCondition();
        }

        /// <summary>
        /// লেভেল প্রোগ্রেস চেক করে আনলক বা লক স্টেট বজায় রাখে
        /// </summary>
        public void CheckUnlockCondition()
        {
            // LevelManager বা PlayerPrefs থেকে সেভ হওয়া লেভেল ইনডেক্স নেওয়া হচ্ছে
            int savedLevelIndex = PlayerPrefs.GetInt("LevelIndex", 0);

            // যদি বর্তমান লেভেল বা সেভ হওয়া লেভেল টার্গেট লেভেলের সমান বা বেশি হয়
            if (savedLevelIndex >= _targetLevelIndex)
            {
                // শর্ত পূরণ হলে: অবজেক্ট হাইড হবে এবং বাটন ইন্টারেক্টিভ হবে
                if (_objectToHide != null)
                {
                    _objectToHide.SetActive(false);
                }

                if (_buttonToEnable != null)
                {
                    _buttonToEnable.interactable = true;
                }
            }
            else
            {
                // শর্ত পূরণ না হলে: অবজেক্ট শো করবে এবং বাটন ডিসেবল থাকবে
                if (_objectToHide != null)
                {
                    _objectToHide.SetActive(true);
                }

                if (_buttonToEnable != null)
                {
                    _buttonToEnable.interactable = false;
                }
            }
        }
    }
}