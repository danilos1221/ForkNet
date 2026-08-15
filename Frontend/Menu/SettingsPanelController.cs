using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Управляет UI панели настроек:
/// - громкость музыки
/// - fullscreen on/off
/// - выбор разрешения из списка кнопок
/// - скорость быстрого пропуска (удержание пробела)
/// </summary>
public class SettingsPanelController : MonoBehaviour
{
    [System.Serializable]
    public struct ResolutionOption
    {
        public int width;
        public int height;
        public string label;
    }

    [Header("Music")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.8f;

    [Header("Fast Forward")]
    [SerializeField] private Slider fastForwardSpeedSlider;
    [SerializeField, Range(1f, 50f)] private float defaultFastForwardSpeed = 8f;

    [Header("Fullscreen")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Image fullscreenStateImage;
    [SerializeField] private Sprite fullscreenOnSprite;
    [SerializeField] private Sprite fullscreenOffSprite;

    [Header("Resolution")]
    [SerializeField] private Button resolutionMenuButton;
    [SerializeField] private GameObject resolutionPanel;
    [SerializeField] private List<Button> resolutionButtons = new();
    [SerializeField] private TMP_Text resolutionLabel;
    [SerializeField] private List<ResolutionOption> resolutionOptions = new()
    {
        new ResolutionOption { width = 1280, height = 720, label = "1280x720" },
        new ResolutionOption { width = 1600, height = 900, label = "1600x900" },
        new ResolutionOption { width = 1920, height = 1080, label = "1920x1080" },
        new ResolutionOption { width = 2560, height = 1440, label = "2560x1440" }
    };

    private const string MusicVolumeKey = "settings_music_volume";
    private const string FullscreenKey = "settings_fullscreen";
    private const string ResolutionWidthKey = "settings_resolution_w";
    private const string ResolutionHeightKey = "settings_resolution_h";
    private const string FastForwardSpeedKey = "settings_fast_forward_speed";

    private bool suppressUiCallbacks;
    private ScenarioManager cachedScenarioManager;

    private void Awake()
    {
        BindUi();
        EnsureDefaultPrefs();
        LoadAndApplySettings();
    }

    private void OnDestroy()
    {
        UnbindUi();
    }

    private void BindUi()
    {
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeSliderChanged);

        if (fastForwardSpeedSlider != null)
            fastForwardSpeedSlider.onValueChanged.AddListener(OnFastForwardSpeedChanged);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);

        if (resolutionMenuButton != null)
            resolutionMenuButton.onClick.AddListener(ToggleResolutionPanel);

        int count = Mathf.Min(resolutionButtons.Count, resolutionOptions.Count);
        for (int i = 0; i < count; i++)
        {
            int captured = i;
            if (resolutionButtons[captured] != null)
                resolutionButtons[captured].onClick.AddListener(() => SetResolutionByIndex(captured));
        }
    }

    private void UnbindUi()
    {
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeSliderChanged);

        if (fastForwardSpeedSlider != null)
            fastForwardSpeedSlider.onValueChanged.RemoveListener(OnFastForwardSpeedChanged);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenToggleChanged);

        if (resolutionMenuButton != null)
            resolutionMenuButton.onClick.RemoveListener(ToggleResolutionPanel);
    }

    private void EnsureDefaultPrefs()
    {
        if (!PlayerPrefs.HasKey(MusicVolumeKey))
            PlayerPrefs.SetFloat(MusicVolumeKey, defaultMusicVolume);

        if (!PlayerPrefs.HasKey(FullscreenKey))
            PlayerPrefs.SetInt(FullscreenKey, Screen.fullScreen ? 1 : 0);

        if (!PlayerPrefs.HasKey(ResolutionWidthKey))
            PlayerPrefs.SetInt(ResolutionWidthKey, Screen.currentResolution.width);

        if (!PlayerPrefs.HasKey(ResolutionHeightKey))
            PlayerPrefs.SetInt(ResolutionHeightKey, Screen.currentResolution.height);

        if (!PlayerPrefs.HasKey(FastForwardSpeedKey))
            PlayerPrefs.SetFloat(FastForwardSpeedKey, defaultFastForwardSpeed);

        PlayerPrefs.Save();
    }

    private void LoadAndApplySettings()
    {
        suppressUiCallbacks = true;

        float musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume);
        bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        int width = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.currentResolution.width);
        int height = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.currentResolution.height);
        float fastForwardSpeed = PlayerPrefs.GetFloat(FastForwardSpeedKey, defaultFastForwardSpeed);

        ApplyMusicVolume(musicVolume);
        ApplyFastForwardSpeed(fastForwardSpeed);

        if (width > 0 && height > 0)
            Screen.SetResolution(width, height, fullscreen);
        else
            Screen.fullScreen = fullscreen;

        if (musicVolumeSlider != null)
            musicVolumeSlider.SetValueWithoutNotify(musicVolume);

        if (fastForwardSpeedSlider != null)
            fastForwardSpeedSlider.SetValueWithoutNotify(fastForwardSpeed);

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);

        UpdateFullscreenIcon(Screen.fullScreen);
        UpdateResolutionLabel(width, height);

        if (resolutionPanel != null)
            resolutionPanel.SetActive(false);

        suppressUiCallbacks = false;
    }

    private void OnMusicVolumeSliderChanged(float value)
    {
        if (suppressUiCallbacks) return;

        ApplyMusicVolume(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value));
        PlayerPrefs.Save();
    }

    private void OnFastForwardSpeedChanged(float value)
    {
        if (suppressUiCallbacks) return;

        ApplyFastForwardSpeed(value);
        PlayerPrefs.SetFloat(FastForwardSpeedKey, Mathf.Clamp(value, 1f, 50f));
        PlayerPrefs.Save();
    }

    private void ApplyMusicVolume(float volume)
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.SetMusicVolume(volume);
    }

    private void ApplyFastForwardSpeed(float value)
    {
        ScenarioManager scenario = GetScenarioManager();
        if (scenario != null)
            scenario.SetSkipSpeedMultiplier(value);
    }

    private ScenarioManager GetScenarioManager()
    {
        if (cachedScenarioManager == null)
            cachedScenarioManager = FindAnyObjectByType<ScenarioManager>();

        return cachedScenarioManager;
    }

    private void OnFullscreenToggleChanged(bool isFullscreen)
    {
        if (suppressUiCallbacks) return;

        Screen.fullScreen = isFullscreen;

        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();

        UpdateFullscreenIcon(isFullscreen);
    }

    private void UpdateFullscreenIcon(bool isFullscreen)
    {
        if (fullscreenStateImage == null) return;

        if (isFullscreen && fullscreenOnSprite != null)
            fullscreenStateImage.sprite = fullscreenOnSprite;
        else if (!isFullscreen && fullscreenOffSprite != null)
            fullscreenStateImage.sprite = fullscreenOffSprite;
    }

    public void ToggleResolutionPanel()
    {
        if (resolutionPanel == null) return;
        resolutionPanel.SetActive(!resolutionPanel.activeSelf);
    }

    public void SetResolutionByIndex(int index)
    {
        if (index < 0 || index >= resolutionOptions.Count)
            return;

        ResolutionOption option = resolutionOptions[index];
        if (option.width <= 0 || option.height <= 0)
            return;

        Screen.SetResolution(option.width, option.height, Screen.fullScreen);

        PlayerPrefs.SetInt(ResolutionWidthKey, option.width);
        PlayerPrefs.SetInt(ResolutionHeightKey, option.height);
        PlayerPrefs.Save();

        UpdateResolutionLabel(option.width, option.height);

        if (resolutionPanel != null)
            resolutionPanel.SetActive(false);
    }

    private void UpdateResolutionLabel(int width, int height)
    {
        if (resolutionLabel == null) return;

        string resolutionName = GetResolutionName(width, height);
        resolutionLabel.text = $"Screen Resolution:\n      {resolutionName}";
    }

    private string GetResolutionName(int width, int height)
    {
        for (int i = 0; i < resolutionOptions.Count; i++)
        {
            ResolutionOption option = resolutionOptions[i];
            if (option.width == width && option.height == height)
            {
                if (!string.IsNullOrWhiteSpace(option.label))
                    return option.label;

                return $"{width}x{height}";
            }
        }

        return $"{width}x{height}";
    }
}
