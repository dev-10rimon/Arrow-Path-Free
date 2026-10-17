using UnityEngine;
using UnityEngine.UI;

namespace MultiTechStudio.EscapeGame
{
    /// <summary>
    /// Manages the settings UI panel by toggling visibility between separate ON and OFF buttons for Sound, Music, and Vibration.
    /// </summary>
    public class SettingsUIManager : MonoBehaviour
    {
        private SoundManager SoundManager => SoundManager.Instance;

        [Header("Settings Panel")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button closeButton;

        [Header("Sound Settings (ON & OFF Buttons)")]
        [SerializeField] private GameObject soundOnButton;
        [SerializeField] private GameObject soundOffButton;

        [Header("Music Settings (ON & OFF Buttons)")]
        [SerializeField] private GameObject musicOnButton;
        [SerializeField] private GameObject musicOffButton;

        [Header("Vibration Settings (ON & OFF Buttons)")]
        [SerializeField] private GameObject vibrationOnButton;
        [SerializeField] private GameObject vibrationOffButton;

        private LevelState previousLevelState;

        // Vibration state property using PlayerPrefs
        private bool VibrationEnabled
        {
            get => PlayerPrefs.GetInt("VibrationEnabled", 1) == 1;
            set => PlayerPrefs.SetInt("VibrationEnabled", value ? 1 : 0);
        }

        void Start()
        {
            SetupButtonListeners();
            RefreshSettingsStates();
        }

        private void SetupButtonListeners()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseSettings);

            // Sound Button Click Listeners
            if (soundOnButton != null && soundOnButton.TryGetComponent<Button>(out var soundOnBtn))
                soundOnBtn.onClick.AddListener(ToggleSound);

            if (soundOffButton != null && soundOffButton.TryGetComponent<Button>(out var soundOffBtn))
                soundOffBtn.onClick.AddListener(ToggleSound);

            // Music Button Click Listeners
            if (musicOnButton != null && musicOnButton.TryGetComponent<Button>(out var musicOnBtn))
                musicOnBtn.onClick.AddListener(ToggleMusic);

            if (musicOffButton != null && musicOffButton.TryGetComponent<Button>(out var musicOffBtn))
                musicOffBtn.onClick.AddListener(ToggleMusic);

            // Vibration Button Click Listeners
            if (vibrationOnButton != null && vibrationOnButton.TryGetComponent<Button>(out var vibOnBtn))
                vibOnBtn.onClick.AddListener(ToggleVibration);

            if (vibrationOffButton != null && vibrationOffButton.TryGetComponent<Button>(out var vibOffBtn))
                vibOffBtn.onClick.AddListener(ToggleVibration);
        }

        private void ToggleSound()
        {
            if (SoundManager != null)
            {
                SoundManager.ToggleSound();
                UpdateSoundVisual(SoundManager.SoundEnabled);

                if (SoundManager.SoundEnabled)
                {
                    SoundManager.PlaySound("button");
                }
            }
        }

        private void ToggleMusic()
        {
            if (SoundManager != null)
            {
                SoundManager.ToggleMusic();
                UpdateMusicVisual(SoundManager.MusicEnabled);

                if (SoundManager.SoundEnabled)
                {
                    SoundManager.PlaySound("button");
                }
            }
        }

        private void ToggleVibration()
        {
            VibrationEnabled = !VibrationEnabled;
            UpdateVibrationVisual(VibrationEnabled);

            if (VibrationEnabled)
            {
                Handheld.Vibrate(); // Test vibration when enabled
            }

            if (SoundManager != null && SoundManager.SoundEnabled)
            {
                SoundManager.PlaySound("button");
            }
        }

        private void UpdateSoundVisual(bool isOn)
        {
            if (soundOnButton != null) soundOnButton.SetActive(isOn);
            if (soundOffButton != null) soundOffButton.SetActive(!isOn);
        }

        private void UpdateMusicVisual(bool isOn)
        {
            if (musicOnButton != null) musicOnButton.SetActive(isOn);
            if (musicOffButton != null) musicOffButton.SetActive(!isOn);
        }

        private void UpdateVibrationVisual(bool isOn)
        {
            if (vibrationOnButton != null) vibrationOnButton.SetActive(isOn);
            if (vibrationOffButton != null) vibrationOffButton.SetActive(!isOn);
        }

        public void OpenSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }

            if (SoundManager != null)
                SoundManager.PlaySound("button");

            if (LevelManager.Instance != null)
            {
                previousLevelState = LevelManager.Instance.levelState;
                LevelManager.Instance.levelState = LevelState.NotStarted;
            }

            RefreshSettingsStates();
        }

        public void CloseSettings()
        {
            if (SoundManager != null)
                SoundManager.PlaySound("close");

            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.levelState = previousLevelState;
            }
        }

        private void RefreshSettingsStates()
        {
            bool soundState = SoundManager != null && SoundManager.SoundEnabled;
            bool musicState = SoundManager != null && SoundManager.MusicEnabled;
            bool vibrationState = VibrationEnabled;

            UpdateSoundVisual(soundState);
            UpdateMusicVisual(musicState);
            UpdateVibrationVisual(vibrationState);
        }

        void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();

            if (soundOnButton != null && soundOnButton.TryGetComponent<Button>(out var soundOnBtn)) soundOnBtn.onClick.RemoveAllListeners();
            if (soundOffButton != null && soundOffButton.TryGetComponent<Button>(out var soundOffBtn)) soundOffBtn.onClick.RemoveAllListeners();

            if (musicOnButton != null && musicOnButton.TryGetComponent<Button>(out var musicOnBtn)) musicOnBtn.onClick.RemoveAllListeners();
            if (musicOffButton != null && musicOffButton.TryGetComponent<Button>(out var musicOffBtn)) musicOffBtn.onClick.RemoveAllListeners();

            if (vibrationOnButton != null && vibrationOnButton.TryGetComponent<Button>(out var vibOnBtn)) vibOnBtn.onClick.RemoveAllListeners();
            if (vibrationOffButton != null && vibrationOffButton.TryGetComponent<Button>(out var vibOffBtn)) vibOffBtn.onClick.RemoveAllListeners();
        }
    }
}