using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GetALife.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Settings Categories")]
        [SerializeField] private UIPanelController panelController;
        [SerializeField] private GameObject generalPanel;
        [SerializeField] private GameObject audioPanel;
        [SerializeField] private GameObject gameplayPanel;
        [SerializeField] private GameObject graphicsPanel;
        [SerializeField] private GameObject onlinePanel;
        [SerializeField] private GameObject accessibilityPanel;

        [Header("Audio Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle masterMuteToggle;
        [SerializeField] private Toggle muteAllToggle;
        [SerializeField] private Toggle musicMuteToggle;
        [SerializeField] private Toggle sfxMuteToggle;
        [SerializeField] private TMP_Text masterValueLabel;
        [SerializeField] private TMP_Text musicValueLabel;
        [SerializeField] private TMP_Text sfxValueLabel;

        [Header("General Settings")]
        [SerializeField] private TMP_Dropdown languageDropdown;
        [SerializeField] private TMP_Dropdown timeFormatDropdown;
        [SerializeField] private Toggle profanityFilterToggle;
        [SerializeField] private Toggle tutorialToggle;
        [SerializeField] private Button resetTutorialsButton;
        [SerializeField] private List<string> profanityList = new List<string>();

        [Header("Online Settings")]
        [SerializeField] private Toggle onlineServicesToggle;
        [SerializeField] private Button blockedListButton;
        [SerializeField] private GameObject blockedListPanel;
        [SerializeField] private GameObject settingsRootPanel;

        [Header("Accessibility Settings")]
        [SerializeField] private TMP_Dropdown colorBlindDropdown;

        private const string LanguageKey = "Settings.Language";
        private const string TimeFormatKey = "Settings.TimeFormat";
        private const string ProfanityFilterKey = "Settings.ProfanityFilter";
        private const string TutorialEnabledKey = "Settings.TutorialsEnabled";
        private const string TutorialsResetCounterKey = "Settings.TutorialsResetCounter";
        private const string OnlineServicesEnabledKey = "Settings.OnlineServicesEnabled";
        private const string ColorBlindModeKey = "Settings.ColorBlindMode";

        private static readonly List<string> DefaultLanguageOptions = new List<string>
        {
            "English",
            "French (France)",
            "Spanish"
        };
        private static readonly List<string> DefaultTimeFormatOptions = new List<string> { "12h", "24h" };
        private static readonly List<string> DefaultColorBlindOptions = new List<string>
        {
            "None",
            "Protanopia",
            "Deuteranopia",
            "Tritanopia"
        };

        private readonly List<GameObject> categoryPanels = new List<GameObject>();
        private readonly List<GameObject> panelsToShow = new List<GameObject>();
        private readonly List<GameObject> panelsToHide = new List<GameObject>();
        private Coroutine waitForAudioManagerRoutine;
        private bool audioControlsBound;
        private bool generalControlsBound;
        private bool onlineControlsBound;
        private bool accessibilityControlsBound;
        private GameObject lastCategoryPanel;

        private void Awake()
        {
            CacheCategoryPanels();
            CacheLastCategoryPanel();
        }

        private void OnEnable()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnOutputVolumesChanged += OnAudioOutputChanged;
            }

            InitializeAudioControls();
            InitializeGeneralControls();
            InitializeOnlineControls();
            InitializeAccessibilityControls();
            SyncAudioControls();
            RestoreLastCategoryPanel();
        }

        private void OnDisable()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnOutputVolumesChanged -= OnAudioOutputChanged;
            }

            if (waitForAudioManagerRoutine != null)
            {
                StopCoroutine(waitForAudioManagerRoutine);
                waitForAudioManagerRoutine = null;
            }

            UnbindGeneralControls();
            UnbindOnlineControls();
            UnbindAccessibilityControls();
        }

        private void InitializeAudioControls()
        {
            ConfigureAudioSlider(masterVolumeSlider);
            ConfigureAudioSlider(musicVolumeSlider);
            ConfigureAudioSlider(sfxVolumeSlider);
            TryBindAudioControls();
        }

        private void TryBindAudioControls()
        {
            if (audioControlsBound)
            {
                return;
            }

            AudioManager audio = AudioManager.Instance;
            if (audio == null)
            {
                if (waitForAudioManagerRoutine == null)
                {
                    waitForAudioManagerRoutine = StartCoroutine(WaitForAudioManagerAndBind());
                }
                return;
            }

            BindAudioControls(audio);
        }

        private IEnumerator WaitForAudioManagerAndBind()
        {
            while (AudioManager.Instance == null)
            {
                yield return null;
            }

            BindAudioControls(AudioManager.Instance);
            UpdateAudioLabels();
            waitForAudioManagerRoutine = null;
        }

        private void BindAudioControls(AudioManager audio)
        {
            audioControlsBound = true;
            audio.OnOutputVolumesChanged -= OnAudioOutputChanged;
            audio.OnOutputVolumesChanged += OnAudioOutputChanged;

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeSliderChanged);
                masterVolumeSlider.SetValueWithoutNotify(audio.GetMasterVolume());
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeSliderChanged);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeSliderChanged);
                musicVolumeSlider.SetValueWithoutNotify(audio.GetMusicVolume());
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeSliderChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeSliderChanged);
                sfxVolumeSlider.SetValueWithoutNotify(audio.GetSFXVolume());
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeSliderChanged);
            }

            if (musicMuteToggle != null)
            {
                musicMuteToggle.onValueChanged.RemoveListener(OnMusicMuteToggled);
                musicMuteToggle.SetIsOnWithoutNotify(audio.IsMusicMuted);
                musicMuteToggle.onValueChanged.AddListener(OnMusicMuteToggled);
            }

            if (sfxMuteToggle != null)
            {
                sfxMuteToggle.onValueChanged.RemoveListener(OnSfxMuteToggled);
                sfxMuteToggle.SetIsOnWithoutNotify(audio.IsSfxMuted);
                sfxMuteToggle.onValueChanged.AddListener(OnSfxMuteToggled);
            }

            if (masterMuteToggle != null)
            {
                masterMuteToggle.onValueChanged.RemoveListener(OnMasterMuteToggled);
                masterMuteToggle.SetIsOnWithoutNotify(audio.IsMasterMuted);
                masterMuteToggle.onValueChanged.AddListener(OnMasterMuteToggled);
            }

            if (muteAllToggle != null)
            {
                muteAllToggle.onValueChanged.RemoveListener(OnMuteAllToggled);
                muteAllToggle.SetIsOnWithoutNotify(audio.IsMasterMuted);
                muteAllToggle.onValueChanged.AddListener(OnMuteAllToggled);
            }
        }

        private static void ConfigureAudioSlider(Slider slider)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.SetValueWithoutNotify(Mathf.Clamp01(slider.value));
        }

        private void OnMasterVolumeSliderChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(value);
            }

            UpdatePercentageLabel(masterValueLabel, value);
        }

        private void OnMusicVolumeSliderChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(value);
            }

            UpdatePercentageLabel(musicValueLabel, value);
        }

        private void OnSfxVolumeSliderChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSFXVolume(value);
            }

            UpdatePercentageLabel(sfxValueLabel, value);
        }

        private void OnMusicMuteToggled(bool isMuted)
        {
            AudioManager.Instance?.SetMusicMuted(isMuted);
        }

        private void OnSfxMuteToggled(bool isMuted)
        {
            AudioManager.Instance?.SetSfxMuted(isMuted);
        }

        private void OnMasterMuteToggled(bool isMuted)
        {
            AudioManager.Instance?.SetMasterMuted(isMuted);
            if (muteAllToggle != null)
            {
                muteAllToggle.SetIsOnWithoutNotify(isMuted);
            }
        }

        private void OnMuteAllToggled(bool isMuted)
        {
            AudioManager.Instance?.SetMasterMuted(isMuted);
            AudioManager.Instance?.SetMusicMuted(isMuted);
            AudioManager.Instance?.SetSfxMuted(isMuted);
            if (masterMuteToggle != null)
            {
                masterMuteToggle.SetIsOnWithoutNotify(isMuted);
            }
            if (musicMuteToggle != null)
            {
                musicMuteToggle.SetIsOnWithoutNotify(isMuted);
            }
            if (sfxMuteToggle != null)
            {
                sfxMuteToggle.SetIsOnWithoutNotify(isMuted);
            }
        }

        private void OnAudioOutputChanged()
        {
            SyncAudioControls();
        }

        private void UpdateAudioLabels()
        {
            if (masterVolumeSlider != null)
            {
                UpdatePercentageLabel(masterValueLabel, masterVolumeSlider.value);
            }

            if (musicVolumeSlider != null)
            {
                UpdatePercentageLabel(musicValueLabel, musicVolumeSlider.value);
            }

            if (sfxVolumeSlider != null)
            {
                UpdatePercentageLabel(sfxValueLabel, sfxVolumeSlider.value);
            }

            if (AudioManager.Instance != null)
            {
                if (masterMuteToggle != null)
                {
                    masterMuteToggle.SetIsOnWithoutNotify(AudioManager.Instance.IsMasterMuted);
                }

                if (muteAllToggle != null)
                {
                    muteAllToggle.SetIsOnWithoutNotify(AudioManager.Instance.IsMasterMuted);
                }

                if (musicMuteToggle != null)
                {
                    musicMuteToggle.SetIsOnWithoutNotify(AudioManager.Instance.IsMusicMuted);
                }

                if (sfxMuteToggle != null)
                {
                    sfxMuteToggle.SetIsOnWithoutNotify(AudioManager.Instance.IsSfxMuted);
                }
            }
        }

        private void SyncAudioControls()
        {
            AudioManager audio = AudioManager.Instance;
            if (audio == null)
            {
                UpdateAudioLabels();
                return;
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(audio.GetMasterVolume());
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValueWithoutNotify(audio.GetMusicVolume());
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(audio.GetSFXVolume());
            }

            if (masterMuteToggle != null)
            {
                masterMuteToggle.SetIsOnWithoutNotify(audio.IsMasterMuted);
            }

            if (muteAllToggle != null)
            {
                muteAllToggle.SetIsOnWithoutNotify(audio.IsMasterMuted);
            }

            if (musicMuteToggle != null)
            {
                musicMuteToggle.SetIsOnWithoutNotify(audio.IsMusicMuted);
            }

            if (sfxMuteToggle != null)
            {
                sfxMuteToggle.SetIsOnWithoutNotify(audio.IsSfxMuted);
            }

            UpdateAudioLabels();
        }

        private void UpdatePercentageLabel(TMP_Text label, float value)
        {
            if (label != null)
            {
                label.text = $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
            }
        }

        public void OnSaveClicked()
        {
            ApplyAudioSettingsFromControls();
            PlayerPrefs.Save();
        }

        public void OnGeneralClicked()
        {
            ShowCategoryPanel(generalPanel);
        }

        public void OnAudioClicked()
        {
            ShowCategoryPanel(audioPanel);
        }

        public void OnGameplayClicked()
        {
            ShowCategoryPanel(gameplayPanel);
        }

        public void OnGraphicsClicked()
        {
            ShowCategoryPanel(graphicsPanel);
        }

        public void OnOnlineClicked()
        {
            ShowCategoryPanel(onlinePanel);
        }

        public void OnAccessibilityClicked()
        {
            ShowCategoryPanel(accessibilityPanel);
        }

        private void ApplyAudioSettingsFromControls()
        {
            AudioManager audio = AudioManager.Instance;
            if (audio == null)
            {
                return;
            }

            if (masterVolumeSlider != null)
            {
                audio.SetMasterVolume(masterVolumeSlider.value);
            }

            if (musicVolumeSlider != null)
            {
                audio.SetMusicVolume(musicVolumeSlider.value);
            }

            if (sfxVolumeSlider != null)
            {
                audio.SetSFXVolume(sfxVolumeSlider.value);
            }

            if (masterMuteToggle != null)
            {
                audio.SetMasterMuted(masterMuteToggle.isOn);
            }

            if (muteAllToggle != null && masterMuteToggle == null)
            {
                audio.SetMasterMuted(muteAllToggle.isOn);
            }

            if (musicMuteToggle != null)
            {
                audio.SetMusicMuted(musicMuteToggle.isOn);
            }

            if (sfxMuteToggle != null)
            {
                audio.SetSfxMuted(sfxMuteToggle.isOn);
            }
        }

        private void CacheCategoryPanels()
        {
            categoryPanels.Clear();
            AddCategoryPanel(generalPanel);
            AddCategoryPanel(audioPanel);
            AddCategoryPanel(gameplayPanel);
            AddCategoryPanel(graphicsPanel);
            AddCategoryPanel(onlinePanel);
            AddCategoryPanel(accessibilityPanel);
        }

        private void CacheLastCategoryPanel()
        {
            lastCategoryPanel = null;
            for (int i = 0; i < categoryPanels.Count; i++)
            {
                GameObject panel = categoryPanels[i];
                if (panel != null && panel.activeSelf)
                {
                    lastCategoryPanel = panel;
                    break;
                }
            }

            if (lastCategoryPanel == null)
            {
                lastCategoryPanel = generalPanel;
            }
        }

        private void RestoreLastCategoryPanel()
        {
            if (panelController == null)
            {
                return;
            }

            GameObject panelToShow = lastCategoryPanel != null ? lastCategoryPanel : generalPanel;
            if (panelToShow == null)
            {
                return;
            }

            panelsToShow.Clear();
            if (settingsRootPanel != null)
            {
                panelsToShow.Add(settingsRootPanel);
            }
            panelsToShow.Add(panelToShow);
            panelController.ShowPanels(panelsToShow);

            panelsToHide.Clear();
            for (int i = 0; i < categoryPanels.Count; i++)
            {
                GameObject panel = categoryPanels[i];
                if (panel != null && panel != panelToShow)
                {
                    panelsToHide.Add(panel);
                }
            }

            if (blockedListPanel != null)
            {
                panelsToHide.Add(blockedListPanel);
            }

            panelController.HidePanels(panelsToHide);
        }

        private void AddCategoryPanel(GameObject panel)
        {
            if (panel != null && !categoryPanels.Contains(panel))
            {
                categoryPanels.Add(panel);
            }
        }

        private void InitializeGeneralControls()
        {
            if (generalControlsBound)
            {
                return;
            }

            generalControlsBound = true;

            BindDropdown(languageDropdown, DefaultLanguageOptions, LanguageKey, DefaultLanguageOptions[0], OnLanguageChanged);
            BindDropdown(timeFormatDropdown, DefaultTimeFormatOptions, TimeFormatKey, DefaultTimeFormatOptions[0], OnTimeFormatChanged);

            if (profanityFilterToggle != null)
            {
                profanityFilterToggle.onValueChanged.RemoveListener(OnProfanityFilterToggled);
                profanityFilterToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(ProfanityFilterKey, 0) == 1);
                profanityFilterToggle.onValueChanged.AddListener(OnProfanityFilterToggled);
            }

            if (tutorialToggle != null)
            {
                tutorialToggle.onValueChanged.RemoveListener(OnTutorialsToggled);
                tutorialToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(TutorialEnabledKey, 1) == 1);
                tutorialToggle.onValueChanged.AddListener(OnTutorialsToggled);
            }

            if (resetTutorialsButton != null)
            {
                resetTutorialsButton.onClick.RemoveListener(OnResetTutorialsClicked);
                resetTutorialsButton.onClick.AddListener(OnResetTutorialsClicked);
            }
        }

        private void UnbindGeneralControls()
        {
            if (!generalControlsBound)
            {
                return;
            }

            generalControlsBound = false;

            if (languageDropdown != null)
            {
                languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
            }

            if (timeFormatDropdown != null)
            {
                timeFormatDropdown.onValueChanged.RemoveListener(OnTimeFormatChanged);
            }

            if (profanityFilterToggle != null)
            {
                profanityFilterToggle.onValueChanged.RemoveListener(OnProfanityFilterToggled);
            }

            if (tutorialToggle != null)
            {
                tutorialToggle.onValueChanged.RemoveListener(OnTutorialsToggled);
            }

            if (resetTutorialsButton != null)
            {
                resetTutorialsButton.onClick.RemoveListener(OnResetTutorialsClicked);
            }
        }

        private void InitializeOnlineControls()
        {
            if (onlineControlsBound)
            {
                return;
            }

            onlineControlsBound = true;

            if (onlineServicesToggle != null)
            {
                onlineServicesToggle.onValueChanged.RemoveListener(OnOnlineServicesToggled);
                onlineServicesToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(OnlineServicesEnabledKey, 1) == 1);
                onlineServicesToggle.onValueChanged.AddListener(OnOnlineServicesToggled);
            }

            if (blockedListButton != null)
            {
                blockedListButton.onClick.RemoveListener(OnBlockedListClicked);
                blockedListButton.onClick.AddListener(OnBlockedListClicked);
            }
        }

        private void UnbindOnlineControls()
        {
            if (!onlineControlsBound)
            {
                return;
            }

            onlineControlsBound = false;

            if (onlineServicesToggle != null)
            {
                onlineServicesToggle.onValueChanged.RemoveListener(OnOnlineServicesToggled);
            }

            if (blockedListButton != null)
            {
                blockedListButton.onClick.RemoveListener(OnBlockedListClicked);
            }
        }

        private void InitializeAccessibilityControls()
        {
            if (accessibilityControlsBound)
            {
                return;
            }

            accessibilityControlsBound = true;

            BindDropdown(colorBlindDropdown, DefaultColorBlindOptions, ColorBlindModeKey, DefaultColorBlindOptions[0], OnColorBlindModeChanged);
        }

        private void UnbindAccessibilityControls()
        {
            if (!accessibilityControlsBound)
            {
                return;
            }

            accessibilityControlsBound = false;

            if (colorBlindDropdown != null)
            {
                colorBlindDropdown.onValueChanged.RemoveListener(OnColorBlindModeChanged);
            }
        }

        private void BindDropdown(TMP_Dropdown dropdown, List<string> defaultOptions, string prefsKey, string fallbackValue, UnityEngine.Events.UnityAction<int> onChanged)
        {
            if (dropdown == null)
            {
                return;
            }

            dropdown.ClearOptions();
            dropdown.AddOptions(defaultOptions);

            string savedValue = PlayerPrefs.GetString(prefsKey, fallbackValue);
            int selectedIndex = GetOptionIndex(dropdown, savedValue);
            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }

            dropdown.onValueChanged.RemoveListener(onChanged);
            dropdown.SetValueWithoutNotify(selectedIndex);
            dropdown.onValueChanged.AddListener(onChanged);
        }

        private static int GetOptionIndex(TMP_Dropdown dropdown, string value)
        {
            if (dropdown == null || dropdown.options == null)
            {
                return -1;
            }

            for (int i = 0; i < dropdown.options.Count; i++)
            {
                if (dropdown.options[i].text == value)
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnLanguageChanged(int optionIndex)
        {
            SaveDropdownPreference(languageDropdown, optionIndex, LanguageKey);

            Locale targetLocale = null;

            switch (optionIndex)
            {
                case 0: // English
                targetLocale = LocalizationSettings.AvailableLocales.GetLocale("en");
                break;

                case 1: // French (France)
                targetLocale = LocalizationSettings.AvailableLocales.GetLocale("fr");
                break;

                case 2: // Spanish
                targetLocale = LocalizationSettings.AvailableLocales.GetLocale("es");
                break;
            }

            if (targetLocale != null)
            {
                LocalizationSettings.SelectedLocale = targetLocale;
            }
        }

        private void OnTimeFormatChanged(int optionIndex)
        {
            SaveDropdownPreference(timeFormatDropdown, optionIndex, TimeFormatKey);
        }

        private void OnColorBlindModeChanged(int optionIndex)
        {
            SaveDropdownPreference(colorBlindDropdown, optionIndex, ColorBlindModeKey);
        }

        private static void SaveDropdownPreference(TMP_Dropdown dropdown, int optionIndex, string prefsKey)
        {
            if (dropdown == null || dropdown.options == null || dropdown.options.Count == 0)
            {
                return;
            }

            int clampedIndex = Mathf.Clamp(optionIndex, 0, dropdown.options.Count - 1);
            PlayerPrefs.SetString(prefsKey, dropdown.options[clampedIndex].text);
        }

        private void OnProfanityFilterToggled(bool isEnabled)
        {
            PlayerPrefs.SetInt(ProfanityFilterKey, isEnabled ? 1 : 0);
        }

        private void OnTutorialsToggled(bool isEnabled)
        {
            PlayerPrefs.SetInt(TutorialEnabledKey, isEnabled ? 1 : 0);
        }

        private void OnResetTutorialsClicked()
        {
            int resets = PlayerPrefs.GetInt(TutorialsResetCounterKey, 0) + 1;
            PlayerPrefs.SetInt(TutorialsResetCounterKey, resets);
            if (tutorialToggle != null)
            {
                tutorialToggle.SetIsOnWithoutNotify(true);
            }
            PlayerPrefs.SetInt(TutorialEnabledKey, 1);
        }

        private void OnOnlineServicesToggled(bool isEnabled)
        {
            PlayerPrefs.SetInt(OnlineServicesEnabledKey, isEnabled ? 1 : 0);
        }

        public void OnBlockedListClicked()
        {
            if (panelController == null || blockedListPanel == null)
            {
                return;
            }

            panelsToHide.Clear();
            if (settingsRootPanel != null)
            {
                panelsToHide.Add(settingsRootPanel);
            }

            for (int i = 0; i < categoryPanels.Count; i++)
            {
                GameObject panel = categoryPanels[i];
                if (panel != null)
                {
                    panelsToHide.Add(panel);
                }
            }

            panelController.NavigateTo(blockedListPanel, panelsToHide);
        }

        public string FilterProfanity(string input)
        {
            if (!IsProfanityFilterEnabled() || string.IsNullOrEmpty(input))
            {
                return input;
            }

            string filtered = input;
            for (int i = 0; i < profanityList.Count; i++)
            {
                string term = profanityList[i];
                if (string.IsNullOrWhiteSpace(term))
                {
                    continue;
                }

                string escaped = Regex.Escape(term.Trim());
                filtered = Regex.Replace(filtered, escaped, new string('*', term.Trim().Length), RegexOptions.IgnoreCase);
            }

            return filtered;
        }

        private bool IsProfanityFilterEnabled()
        {
            return PlayerPrefs.GetInt(ProfanityFilterKey, 0) == 1;
        }

        private void ShowCategoryPanel(GameObject panelToShow)
        {
            if (panelController == null || panelToShow == null)
            {
                return;
            }

            lastCategoryPanel = panelToShow;

            panelsToShow.Clear();
            panelsToShow.Add(panelToShow);
            panelController.ShowPanels(panelsToShow);

            panelsToHide.Clear();
            for (int i = 0; i < categoryPanels.Count; i++)
            {
                GameObject panel = categoryPanels[i];
                if (panel != null && panel != panelToShow)
                {
                    panelsToHide.Add(panel);
                }
            }

            panelController.HidePanels(panelsToHide);
        }
    }
}