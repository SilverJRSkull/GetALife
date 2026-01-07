using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GetALife.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle masterMuteToggle;
        [SerializeField] private Toggle musicMuteToggle;
        [SerializeField] private Toggle sfxMuteToggle;
        [SerializeField] private TMP_Text masterValueLabel;
        [SerializeField] private TMP_Text musicValueLabel;
        [SerializeField] private TMP_Text sfxValueLabel;

        private Coroutine waitForAudioManagerRoutine;
        private bool audioControlsBound;

        private void OnEnable()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnOutputVolumesChanged += OnAudioOutputChanged;
            }

            InitializeAudioControls();
            SyncAudioControls();
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

            if (musicMuteToggle != null)
            {
                audio.SetMusicMuted(musicMuteToggle.isOn);
            }

            if (sfxMuteToggle != null)
            {
                audio.SetSfxMuted(sfxMuteToggle.isOn);
            }
        }
    }
}