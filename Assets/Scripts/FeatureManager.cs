using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace MultiTechStudio.EscapeGame
{
    public class FeatureManager : MonoBehaviour
    {
        private UiManager uiManager;
        [SerializeField] private Button hintButton;
        [SerializeField] private TextMeshProUGUI hintCountText;
        [SerializeField] private Button gridButton;
        [SerializeField] private TextMeshProUGUI gridCountText;

        [Header("Buy Hint UI")]
        [SerializeField] private GameObject buyHintUI;
        [SerializeField] private TextMeshProUGUI hintPriceText;
        [SerializeField] private Button buyHintButton;
        [SerializeField] private Button buyHintByAdsButton;
        [SerializeField] private Button closeBuyHintButton;
        [Header("Buy Grid UI")]
        [SerializeField] private GameObject buyGridUI;
        [SerializeField] private TextMeshProUGUI gridPriceText;
        [SerializeField] private Button buyGridButton;
        [SerializeField] private Button buyGridByAdsButton;
        [SerializeField] private Button closeBuyGridButton;
        [SerializeField] private int hintPrice = 50;
        [SerializeField] private int gridPrice = 50;

        private int hintCount = 0;
        private int gridCount = 0;

        private LevelState previousLevelState;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            uiManager = LevelManager.Instance.uiManager;

            hintCount = PlayerPrefs.GetInt("HintCount", 5);
            gridCount = PlayerPrefs.GetInt("GridCount", 5);

            hintCountText.text = hintCount.ToString();
            gridCountText.text = gridCount.ToString();

            hintPriceText.text = hintPrice.ToString();
            gridPriceText.text = gridPrice.ToString();

            hintButton?.onClick.AddListener(HintButtonClicked);
            gridButton?.onClick.AddListener(GridButtonClicked);

            buyHintButton?.onClick.AddListener(BuyHintButtonClicked);
            buyHintByAdsButton?.onClick.AddListener(BuyHintByAdsButtonClicked);
            closeBuyHintButton?.onClick.AddListener(CloseBuyHintUI);
            buyGridButton?.onClick.AddListener(BuyGridButtonClicked);
            buyGridByAdsButton?.onClick.AddListener(BuyGridByAdsButtonClicked);
            closeBuyGridButton?.onClick.AddListener(CloseBuyGridUI);
        }
        public bool CanShowHint()
        {
            return hintCount > 0;
        }
        public bool CanShowGrid()
        {
            return gridCount > 0;
        }
        private void AddHint()
        {
            hintCount++;
            PlayerPrefs.SetInt("HintCount", hintCount);
            PlayerPrefs.Save();
            hintCountText.text = hintCount.ToString();
        }
        private void AddGrid()
        {
            gridCount++;
            PlayerPrefs.SetInt("GridCount", gridCount);
            PlayerPrefs.Save();
            gridCountText.text = gridCount.ToString();
        }

        public void ShowHint()
        {
            Debug.Log("Show Hint");
            hintCount--;
            PlayerPrefs.SetInt("HintCount", hintCount);
            hintCountText.text = hintCount.ToString();
            LevelManager.Instance.FindLineCanBeRemove();
        }
        /*public void ShowGrid()
        {
            Debug.Log("Show Grid");
            gridCount--;
            PlayerPrefs.SetInt("GridCount", gridCount);
            gridCountText.text = gridCount.ToString();
            LevelManager.Instance.ShowGrid();
        }*/
        // Prothom obosthay LevelManager.Instance.HideGrid() likhte paren jodi thake.
        private Coroutine hideGridRoutine;

        public void ShowGrid()
        {
            Debug.Log("Show Grid");

            // যদি গ্রিড শেষ হওয়ার আগেই আবার ক্লিক করা হয়, আগের টাইমার রিসেট করার জন্য
            if (hideGridRoutine != null)
            {
                StopCoroutine(hideGridRoutine);
            }

            gridCount--;
            if (gridCountText != null) gridCountText.text = gridCount.ToString();

            // ১. প্রথমে গ্রিড দেখাবে
            LevelManager.Instance.ShowGrid();

            // ২. ঠিক ৫ সেকেন্ড পর গ্রিড হাইড করার জন্য টাইমার শুরু হবে
            hideGridRoutine = StartCoroutine(HideGridAfterDelay(5f));
        }

        private System.Collections.IEnumerator HideGridAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            // ৫ সেকেন্ড পর LevelManager এর মাধ্যমে সব গ্রিড হাইড করা হবে
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.HideGrid();
            }

            Debug.Log("Grid automatically hidden after 5 seconds.");
        }

        #region UI Things
        private void ShowBuyHintUI(int _hintOrGrid = 0) // 0 for hint, 1 for grid
        {
            // SoundManager Null Check
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySound("button");
            }

            // LevelManager Null Check
            if (LevelManager.Instance != null)
            {
                previousLevelState = LevelManager.Instance.levelState;
                LevelManager.Instance.levelState = LevelState.Paused;
            }

            // AdsManager আপাতত স্কিপ রাখা হয়েছে (Ad Buttons Disabled)
            if (buyHintByAdsButton != null) buyHintByAdsButton.interactable = false;
            if (buyGridByAdsButton != null) buyGridByAdsButton.interactable = false;

            // UI Panel Show
            if (_hintOrGrid == 0)
            {
                if (buyHintUI != null) buyHintUI.SetActive(true);
            }
            else if (_hintOrGrid == 1)
            {
                if (buyGridUI != null) buyGridUI.SetActive(true);
            }
        }


        /*   private void HintButtonClicked()
           {
               if (CanShowHint())
               {
                   ShowHint();
               }
               else
               {
                   ShowBuyHintUI(0);
               }
           }
           private void GridButtonClicked()
           {
               if (CanShowGrid())
               {
                   ShowGrid();
               }
               else
               {
                   ShowBuyHintUI(1);
               }
           }*/
        private void HintButtonClicked()
        {
            if (CanShowHint())
            {
                // Pop-up Chhara sora shori hint dekhabe
                ShowHint();
            }
            else
            {
                // 5-ti shesh hoye gele ekhon apnar preference anujayi pop-up ba notification dite paren
                Debug.Log("Free hints are finished for this level!");
            }
        }

        private void GridButtonClicked()
        {
            if (CanShowGrid())
            {
                // Pop-up Chhara sora shori grid dekhabe
                ShowGrid();
            }
            else
            {
                Debug.Log("Free grids are finished for this level!");
            }
        }

        private void CloseBuyHintUI()
        {
            if (buyHintUI != null) buyHintUI.SetActive(false);
            SoundManager.Instance.PlaySound("close");
            LevelManager.Instance.levelState = previousLevelState;
        }
        private void CloseBuyGridUI()
        {
            if (buyGridUI != null) buyGridUI.SetActive(false);
            SoundManager.Instance.PlaySound("close");
            LevelManager.Instance.levelState = previousLevelState;
        }
        private void BuyHintButtonClicked()
        {
            SoundManager.Instance.PlaySound("button");
            if (uiManager.coins >= hintPrice)
            {
                uiManager.SpendCoins(hintPrice);
                AddHint();
                CloseBuyHintUI();
            }
            else
            {
                uiManager.OpenShopUI();
                CloseBuyHintUI();
            }
        }
        private void BuyHintByAdsButtonClicked()
        {
            AdsManager ads = AdsManager.Instance;
            if (ads == null || !ads.IsRewardVideoReady()) return;

            ads.ShowRewardedAd(() =>
                {
                    AddHint();
                    CloseBuyHintUI();
                });
        }
        private void BuyGridButtonClicked()
        {
            SoundManager.Instance.PlaySound("button");
            if (uiManager.coins >= gridPrice)
            {
                uiManager.SpendCoins(gridPrice);
                AddGrid();
                CloseBuyGridUI();
            }
            else
            {
                uiManager.OpenShopUI();
                CloseBuyGridUI();
            }
        }
        private void BuyGridByAdsButtonClicked()
        {
            AdsManager ads = AdsManager.Instance;
            if (ads == null || !ads.IsRewardVideoReady()) return;

            ads.ShowRewardedAd(() =>
                {
                    AddGrid();
                    CloseBuyGridUI();
                });
        }

        public void ResetFreeItemsForNewLevel()
        {
            hintCount = 5;
            gridCount = 5;
            if (hintCountText != null) hintCountText.text = hintCount.ToString();
            if (gridCountText != null) gridCountText.text = gridCount.ToString();
        }


        #endregion
    }


}
