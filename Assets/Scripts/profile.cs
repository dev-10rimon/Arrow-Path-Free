using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileManager : MonoBehaviour
{
    public static ProfileManager Instance { get; private set; }

    [Header("UI Displays")]
    [SerializeField] private Image _primaryAvatarImage;
    [SerializeField] private Image _secondaryAvatarImage;
    [SerializeField] private TMP_Text _nameLabel;

    [Header("Panels / Popups")]
    [SerializeField] private GameObject _profileWindow;
    [SerializeField] private GameObject _nameEditWindow;

    [Header("Input")]
    [SerializeField] private TMP_InputField _nameInputBox;

    [Header("Avatar Collection")]
    [SerializeField] private Button[] _avatarSelectionButtons;
    [SerializeField] private Image[] _availableAvatars;
    [SerializeField] private GameObject[] _avatarSelectionIndicators;

    private const string NamePrefsKey = "PlayerName";
    private const string AvatarPrefsKey = "SelectedAvatar";
    private int _currentAvatarIdx = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        InitializeProfileData();
        SetupAvatarButtons();
        RefreshSelectionVisuals();
    }

    #region Profile Panel Control
    public void ShowProfilePanel() => _profileWindow.SetActive(true);
    public void HideProfilePanel() => _profileWindow.SetActive(false);
    #endregion

    #region Name Edit Panel Control
    public void ShowNameEditPanel()
    {
        if (_nameEditWindow != null)
            _nameEditWindow.SetActive(true);

        if (_nameInputBox != null && _nameLabel != null)
        {
            _nameInputBox.text = _nameLabel.text;
            _nameInputBox.Select();
            _nameInputBox.ActivateInputField();
        }
    }

    public void HideNameEditPanel()
    {
        if (_nameEditWindow != null)
            _nameEditWindow.SetActive(false);
    }

    public void ApplyPlayerName()
    {
        if (_nameInputBox == null) return;

        string finalName = _nameInputBox.text.Trim();
        if (string.IsNullOrEmpty(finalName))
        {
            finalName = "Player";
        }

        PlayerPrefs.SetString(NamePrefsKey, finalName);
        PlayerPrefs.Save();

        if (_nameLabel != null)
        {
            _nameLabel.text = finalName;
        }

        HideNameEditPanel();
    }
    #endregion

    #region Avatar Management
    private void SetupAvatarButtons()
    {
        if (_avatarSelectionButtons == null) return;

        for (int i = 0; i < _avatarSelectionButtons.Length; i++)
        {
            int index = i;
            if (_avatarSelectionButtons[i] != null)
            {
                _avatarSelectionButtons[i].onClick.AddListener(() => ChangeAvatar(index));
            }
        }
    }

    public void ChangeAvatar(int index)
    {
        if (_availableAvatars == null || _availableAvatars.Length == 0) return;
        if (index < 0 || index >= _availableAvatars.Length) return;

        _currentAvatarIdx = index;

        PlayerPrefs.SetInt(AvatarPrefsKey, _currentAvatarIdx);
        PlayerPrefs.Save();

        RefreshActiveAvatars();
        RefreshSelectionVisuals();
    }

    private void RefreshSelectionVisuals()
    {
        if (_avatarSelectionIndicators == null) return;

        for (int i = 0; i < _avatarSelectionIndicators.Length; i++)
        {
            if (_avatarSelectionIndicators[i] != null)
            {
                _avatarSelectionIndicators[i].SetActive(i == _currentAvatarIdx);
            }
        }
    }

    private void RefreshActiveAvatars()
    {
        if (_availableAvatars == null || _currentAvatarIdx < 0 || _currentAvatarIdx >= _availableAvatars.Length) return;
        if (_availableAvatars[_currentAvatarIdx] == null) return;

        Sprite selectedSprite = _availableAvatars[_currentAvatarIdx].sprite;

        if (_primaryAvatarImage != null)
            _primaryAvatarImage.sprite = selectedSprite;

        if (_secondaryAvatarImage != null)
            _secondaryAvatarImage.sprite = selectedSprite;
    }
    #endregion

    #region Data Loading
    private void InitializeProfileData()
    {
        // Load Name
        string savedName = PlayerPrefs.GetString(NamePrefsKey, "Player");
        if (_nameLabel != null)
        {
            _nameLabel.text = savedName;
        }

        // Load Avatar Index
        _currentAvatarIdx = PlayerPrefs.GetInt(AvatarPrefsKey, 0);

        if (_availableAvatars != null && (_currentAvatarIdx < 0 || _currentAvatarIdx >= _availableAvatars.Length))
        {
            _currentAvatarIdx = 0;
        }

        RefreshActiveAvatars();
    }
    #endregion
}