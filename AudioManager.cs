using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

// REMEMBER TO GO INTO PROPERTIES OF EACH SONG, ENABLE PRE-LOAD!
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    public bool IsMusicMuted => isMusicManuallyMuted;
    public bool IsSfxMuted => isSfxMuted;
    public bool IsMasterMuted => isMasterMuted;

    [Header("Audio Mixer & Exposed Params (must match mixer)")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string masterParam = "MasterVolume";
    [SerializeField] private string musicParam = "MusicVolume";
    [SerializeField] private string sfxParam = "SFXVolume";

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music Playlist")]
    [SerializeField] private AudioClip[] musicTracks;
    [SerializeField] private bool loopPlaylist = true;
    [SerializeField] private bool shuffle = false;
    [SerializeField, Range(0f, 5f)] private float crossfadeSeconds = 1.0f;
    [SerializeField, Tooltip("Extra headroom before end to start preloading (sec).")]
    private float preloadLeadSeconds = 0.25f;

    // State
    private const string MusicMutedKey = "MusicMuted";
    private const string SfxMutedKey = "SfxMuted";
    private const string MasterMutedKey = "MasterMuted";

    private int currentTrackIndex = 0;
    private bool isMusicManuallyMuted = false;
    private bool isSfxMuted = false;
    private bool isMasterMuted = false;
    private float lastStoredMusicLinear = 1f;

    private float masterVolumeLinear = 1f;
    private float musicVolumeLinear = 1f;
    private float sfxVolumeLinear = 1f;

    private float musicSourceANormalized = 0f;
    private float musicSourceBNormalized = 0f;

    // Coroutines
    private Coroutine musicFadeRoutine;
    private Coroutine sfxFadeRoutine;
    private Coroutine crossfadeRoutine;
    private Coroutine trackWatchdogRoutine;

    private string Key(string p) => p;

    public event Action OnOutputVolumesChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LoadSavedSettings();

        if (musicSourceA != null) musicSourceA.loop = false;
        if (musicSourceB != null) musicSourceB.loop = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void Start()
    {
        if (musicTracks != null && musicTracks.Length > 0 && !isMusicManuallyMuted)
        {
            PlayNextTrack(initial: true);
        }
    }

    // ---------------------------
    // Public: SFX
    // ---------------------------
    public void PlaySFX(AudioClip clip)
    {
        PlaySFXInternal(clip, 1f);
    }

    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        PlaySFXInternal(clip, volumeScale);
    }

    public void ApplySfxMixerRouting(AudioSource source)
    {
        if (source == null)
            return;

        if (sfxSource != null && sfxSource.outputAudioMixerGroup != null)
            source.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
    }

    private void PlaySFXInternal(AudioClip clip, float volumeScale)
    {
        if (clip == null || sfxSource == null)
            return;

        if (isSfxMuted || isMasterMuted)
            return;

        ApplySfxSourceVolume();

        if (sfxSource.volume <= 0f)
            return;

        float scaledVolume = Mathf.Clamp01(volumeScale);
        float finalScale = Mathf.Clamp01(sfxVolumeLinear * scaledVolume);
        if (finalScale <= 0f)
            return;

        sfxSource.PlayOneShot(clip, finalScale);
    }

    // ---------------------------
    // Public: Volume Setters (UI-friendly, 0..1)
    // ---------------------------
    public void SetMasterVolume(float linear01)
    {
        masterVolumeLinear = Mathf.Clamp01(linear01);
        if (!string.IsNullOrEmpty(masterParam))
            PlayerPrefs.SetFloat(Key(masterParam), masterVolumeLinear);
        ApplyVolumes();
    }

    public void SetMasterMuted(bool isMuted)
    {
        isMasterMuted = isMuted;
        PlayerPrefs.SetInt(MasterMutedKey, isMuted ? 1 : 0);
        ApplyVolumes();
    }

    public void SetMusicVolume(float linear01)
    {
        musicVolumeLinear = Mathf.Clamp01(linear01);
        PlayerPrefs.SetFloat(Key(musicParam), musicVolumeLinear);
        ApplyVolumes();
        if (!isMusicManuallyMuted)
        {
            EnsureMusicPlayingIfNeeded();
        }
    }

    public void SetSFXVolume(float linear01)
    {
        sfxVolumeLinear = Mathf.Clamp01(linear01);
        PlayerPrefs.SetFloat(Key(sfxParam), sfxVolumeLinear);
        ApplyVolumes();
    }

    public float GetMasterVolume() => masterVolumeLinear;
    public float GetMusicVolume() => musicVolumeLinear;
    public float GetSFXVolume() => sfxVolumeLinear;

    // ---------------------------
    // Public: Music Mute / Dampen / Restore
    // ---------------------------
    public void SetMusicMuted(bool isMuted)
    {
        isMusicManuallyMuted = isMuted;
        PlayerPrefs.SetInt(MusicMutedKey, isMuted ? 1 : 0);

        ApplyVolumes();

        var active = GetActiveMusicSource();
        if (isMuted)
        {
            if (active != null && active.isPlaying)
                active.Pause();
            KillTrackWatchdog();
        }
        else
        {
            if (active != null)
                active.UnPause();
            EnsureMusicPlayingIfNeeded();
        }
    }

    public void SetSfxMuted(bool isMuted)
    {
        isSfxMuted = isMuted;
        PlayerPrefs.SetInt(SfxMutedKey, isMuted ? 1 : 0);

        ApplyVolumes();

        if (isMuted && sfxSource != null)
        {
            sfxSource.Stop();
        }
    }

    public void DampenMusic(float factor, float duration)
    {
        if (mixer == null || string.IsNullOrEmpty(musicParam))
            return;

        mixer.GetFloat(musicParam, out float currentDb);
        lastStoredMusicLinear = DbToLinear(currentDb);
        float damp = Mathf.Clamp01(lastStoredMusicLinear * Mathf.Clamp01(factor));
        FadeMusicVolume(damp, duration);
    }

    public void RestoreMusicVolume(float duration)
    {
        if (lastStoredMusicLinear > 0f)
        {
            FadeMusicVolume(lastStoredMusicLinear, duration);
            lastStoredMusicLinear = 1f;
        }
    }

    public void FadeMusicVolume(float targetLinear, float duration)
    {
        if (musicFadeRoutine != null) StopCoroutine(musicFadeRoutine);
        musicFadeRoutine = StartCoroutine(FadeMixerVolume(musicParam, targetLinear, duration));
    }

    public void FadeSfxVolume(float targetLinear, float duration)
    {
        if (sfxFadeRoutine != null) StopCoroutine(sfxFadeRoutine);
        sfxFadeRoutine = StartCoroutine(FadeMixerVolume(sfxParam, targetLinear, duration));
    }

    private void ApplyVolumes()
    {
        ApplyMixerVolume(masterParam, isMasterMuted ? 0f : masterVolumeLinear);
        ApplyMixerVolume(musicParam, (isMasterMuted || isMusicManuallyMuted) ? 0f : masterVolumeLinear * musicVolumeLinear);
        ApplyMixerVolume(sfxParam, (isMasterMuted || isSfxMuted) ? 0f : masterVolumeLinear * sfxVolumeLinear);
        ApplyMusicSourceVolumes();
        ApplySfxSourceVolume();
        OnOutputVolumesChanged?.Invoke();
    }

    private void ApplyMixerVolume(string param, float linear01)
    {
        if (string.IsNullOrEmpty(param) || mixer == null)
            return;

        float clamped = Mathf.Clamp01(linear01);
        float db = LinearToDb(clamped);
        mixer.SetFloat(param, db);
    }

    private void ApplyMusicSourceVolumes()
    {
        float scale = GetMusicOutputScale();
        if (musicSourceA != null)
            musicSourceA.volume = musicSourceANormalized * scale;
        if (musicSourceB != null)
            musicSourceB.volume = musicSourceBNormalized * scale;
    }

    private float GetMusicOutputScale()
    {
        if (isMasterMuted || isMusicManuallyMuted)
            return 0f;
        return masterVolumeLinear * musicVolumeLinear;
    }

    private void ApplySfxSourceVolume()
    {
        if (sfxSource != null)
            sfxSource.volume = (isMasterMuted || isSfxMuted) ? 0f : masterVolumeLinear * sfxVolumeLinear;
    }

    private float GetMusicSourceNormalizedVolume(AudioSource source)
    {
        if (source == musicSourceA) return musicSourceANormalized;
        if (source == musicSourceB) return musicSourceBNormalized;
        return 0f;
    }

    private void SetMusicSourceNormalizedVolume(AudioSource source, float normalized)
    {
        if (source == null) return;

        float clamped = Mathf.Clamp01(normalized);
        if (source == musicSourceA) musicSourceANormalized = clamped;
        else if (source == musicSourceB) musicSourceBNormalized = clamped;

        ApplyMusicSourceVolumes();
    }

    // ---------------------------
    // Public: Playlist Controls
    // ---------------------------
    public void NextTrack()
    {
        if (musicTracks.Length == 0) return;
        PlayNextTrack(forceAdvance: true);
    }

    public void PreviousTrack()
    {
        if (musicTracks.Length == 0) return;
        currentTrackIndex = (currentTrackIndex - 2 + musicTracks.Length) % musicTracks.Length;
        PlayNextTrack(forceAdvance: true);
    }

    public void PlaySpecificTrack(int index0)
    {
        if (musicTracks.Length == 0) return;
        currentTrackIndex = Mathf.Clamp(index0, 0, musicTracks.Length - 1);
        StartCrossfadeTo(musicTracks[currentTrackIndex]);
        currentTrackIndex = NextIndex(currentTrackIndex);
    }

    // ---------------------------
    // Private: Playback
    // ---------------------------
    private void PlayNextTrack(bool forceAdvance = false, bool initial = false)
    {
        if (musicTracks.Length == 0) return;

        if (initial && shuffle)
            currentTrackIndex = UnityEngine.Random.Range(0, musicTracks.Length);

        var clip = musicTracks[currentTrackIndex];
        StartCrossfadeTo(clip);
        currentTrackIndex = NextIndex(currentTrackIndex);
    }

    private int NextIndex(int idx)
    {
        if (shuffle && musicTracks.Length > 1)
        {
            int next;
            do { next = UnityEngine.Random.Range(0, musicTracks.Length); }
            while (next == idx);
            return next;
        }

        if (loopPlaylist) return (idx + 1) % musicTracks.Length;
        return Mathf.Min(idx + 1, musicTracks.Length - 1);
    }

    private void StartCrossfadeTo(AudioClip nextClip)
    {
        if (isMusicManuallyMuted) return;
        if (musicSourceA == null || musicSourceB == null) return;
        if (nextClip == null) return;

        if (crossfadeRoutine != null) StopCoroutine(crossfadeRoutine);
        crossfadeRoutine = StartCoroutine(Co_Crossfade_WithPreload(nextClip, crossfadeSeconds));
    }

    /// <summary>
    /// Preload nextClip (if needed), then perform crossfade.
    /// Also starts a watchdog that will preload & launch the *subsequent* clip in advance.
    /// </summary>
    private IEnumerator Co_Crossfade_WithPreload(AudioClip nextClip, float seconds)
    {
        if (!nextClip.preloadAudioData && nextClip.loadState != AudioDataLoadState.Loaded)
        {
            nextClip.LoadAudioData();
        }
        while (nextClip.loadState == AudioDataLoadState.Loading)
            yield return null;

        var active = GetActiveMusicSource();
        var inactive = (active == musicSourceA) ? musicSourceB : musicSourceA;

        if (inactive == null || active == null) yield break;

        inactive.clip = nextClip;
        inactive.Play();

        float dur = Mathf.Max(0f, seconds);
        float t = 0f;
        float startA = GetMusicSourceNormalizedVolume(active);
        float startB = GetMusicSourceNormalizedVolume(inactive);

        SetMusicSourceNormalizedVolume(inactive, startB);

        KillTrackWatchdog();
        trackWatchdogRoutine = StartCoroutine(Co_TrackWatchdog(inactive, nextClip));

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = (dur <= 0f) ? 1f : Mathf.Clamp01(t / dur);
            float activeNorm = Mathf.Lerp(startA, 0f, k);
            float inactiveNorm = Mathf.Lerp(startB, 1f, k);
            SetMusicSourceNormalizedVolume(active, activeNorm);
            SetMusicSourceNormalizedVolume(inactive, inactiveNorm);
            yield return null;
        }

        SetMusicSourceNormalizedVolume(active, 0f);
        active.Stop();
        SetMusicSourceNormalizedVolume(inactive, 1f);
    }

    /// <summary>
    /// Watches the currently-rising source; preloads and crossfades to the following track
    /// a bit before the clip ends (clip.length - crossfade - preloadLead).
    /// </summary>
    private IEnumerator Co_TrackWatchdog(AudioSource sourcePlaying, AudioClip clip)
    {
        if (clip == null || sourcePlaying == null) yield break;

        float lead = Mathf.Max(0.0f, crossfadeSeconds + preloadLeadSeconds);
        float wait = Mathf.Max(0.0f, clip.length - lead);

        yield return new WaitForSecondsRealtime(wait);

        if (isMusicManuallyMuted || musicTracks.Length == 0)
            yield break;

        int nextIdxPreview = (shuffle && musicTracks.Length > 1)
            ? GetRandomIndexExcluding(ArrayUtilitySafeIndexOf(musicTracks, clip))
            : NextIndex(ArrayUtilitySafeIndexOf(musicTracks, clip));

        nextIdxPreview = Mathf.Clamp(nextIdxPreview, 0, musicTracks.Length - 1);
        var upcoming = musicTracks[nextIdxPreview];

        if (upcoming != null)
        {
            if (!upcoming.preloadAudioData && upcoming.loadState != AudioDataLoadState.Loaded)
                upcoming.LoadAudioData();
            while (upcoming.loadState == AudioDataLoadState.Loading)
                yield return null;

            StartCrossfadeTo(upcoming);

            currentTrackIndex = NextIndex(nextIdxPreview);
        }
    }

    private int GetRandomIndexExcluding(int exclude)
    {
        if (musicTracks == null || musicTracks.Length <= 1) return 0;
        int next;
        do { next = UnityEngine.Random.Range(0, musicTracks.Length); }
        while (next == exclude);
        return next;
    }

    private int ArrayUtilitySafeIndexOf(AudioClip[] arr, AudioClip value)
    {
        if (arr == null) return -1;
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] == value) return i;
        return -1;
    }

    private void KillTrackWatchdog()
    {
        if (trackWatchdogRoutine != null)
        {
            StopCoroutine(trackWatchdogRoutine);
            trackWatchdogRoutine = null;
        }
    }

    private AudioSource GetActiveMusicSource()
    {
        if (musicSourceA != null && musicSourceA.isPlaying) return musicSourceA;
        if (musicSourceB != null && musicSourceB.isPlaying) return musicSourceB;

        if (musicSourceA != null && musicSourceB != null)
            return musicSourceANormalized >= musicSourceBNormalized ? musicSourceA : musicSourceB;

        return musicSourceA != null ? musicSourceA : musicSourceB;
    }

    private void EnsureMusicPlayingIfNeeded()
    {
        if (isMusicManuallyMuted || musicTracks.Length == 0) return;
        var active = GetActiveMusicSource();
        if (active != null && !active.isPlaying)
        {
            PlayNextTrack();
        }
    }

    // ---------------------------
    // Fading helper (mixer param)
    // ---------------------------
    private IEnumerator FadeMixerVolume(string exposedParam, float targetLinear, float duration)
    {
        if (mixer == null || string.IsNullOrEmpty(exposedParam))
            yield break;

        mixer.GetFloat(exposedParam, out float startDb);
        float startLinear = DbToLinear(startDb);
        float endLinear = Mathf.Clamp01(targetLinear);

        float t = 0f;
        float dur = Mathf.Max(0f, duration);

        if (dur == 0f)
        {
            SetMixerLinear(exposedParam, endLinear);
            yield break;
        }

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            float current = Mathf.Lerp(startLinear, endLinear, k);
            SetMixerLinear(exposedParam, current);
            yield return null;
        }

        SetMixerLinear(exposedParam, endLinear);
    }

    // ---------------------------
    // Persistence
    // ---------------------------
    private void LoadSavedSettings()
    {
        masterVolumeLinear = string.IsNullOrEmpty(masterParam)
            ? 1f
            : Mathf.Clamp01(PlayerPrefs.GetFloat(Key(masterParam), 1f));
        musicVolumeLinear = Mathf.Clamp01(PlayerPrefs.GetFloat(Key(musicParam), 1f));
        sfxVolumeLinear = Mathf.Clamp01(PlayerPrefs.GetFloat(Key(sfxParam), 1f));
        isMusicManuallyMuted = PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;
        isSfxMuted = PlayerPrefs.GetInt(SfxMutedKey, 0) == 1;
        isMasterMuted = PlayerPrefs.GetInt(MasterMutedKey, 0) == 1;

        ApplyVolumes();

        if (isMusicManuallyMuted)
        {
            var active = GetActiveMusicSource();
            if (active != null && active.isPlaying)
                active.Pause();
        }

        Debug.Log($"[AudioManager] Loaded Master:{masterVolumeLinear} Music:{musicVolumeLinear} MasterMuted:{isMasterMuted} MusicMuted:{isMusicManuallyMuted} SFXMuted:{isSfxMuted} SFX:{sfxVolumeLinear}");
    }

    // ---------------------------
    // Util: Linear<->dB & Set helper
    // ---------------------------
    private void SetMixerLinear(string param, float linear01)
    {
        ApplyMixerVolume(param, linear01);
    }

    private static float LinearToDb(float linear)
    {
        return (linear <= 0.0001f) ? -80f : Mathf.Log10(linear) * 20f;
    }

    private static float DbToLinear(float dB)
    {
        return Mathf.Pow(10f, dB / 20f);
    }
}