using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.View.Audio
{
    /// <summary>
    /// Presentation-only adapter for the existing Sound/Music settings panels. The
    /// composition root supplies persistence callbacks so this view never owns GameData.
    /// </summary>
    public sealed class AudioSettingsView : MonoBehaviour
    {
        [Header("Sound")]
        [SerializeField] private Slider soundVolumeSlider;
        [SerializeField] private Button soundMuteButton;

        [Header("Music")]
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Button musicMuteButton;

        private Action<int, bool> _onSoundChanged;
        private Action<int, bool> _onMusicChanged;
        private bool _soundMuted;
        private bool _musicMuted;
        private bool _isBound;
        private bool _isSubscribed;

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Bind(
            int soundVolume,
            bool soundMuted,
            int musicVolume,
            bool musicMuted,
            Action<int, bool> onSoundChanged,
            Action<int, bool> onMusicChanged)
        {
            CacheReferences();
            _soundMuted = soundMuted;
            _musicMuted = musicMuted;
            _onSoundChanged = onSoundChanged;
            _onMusicChanged = onMusicChanged;
            _isBound = true;
            RefreshControls(soundVolume, musicVolume);
            Subscribe();
        }

        private void CacheReferences()
        {
            soundVolumeSlider ??= transform.Find("Sound/Slider")?.GetComponent<Slider>();
            soundMuteButton ??= transform.Find("Sound/SoundButton")?.GetComponent<Button>();
            musicVolumeSlider ??= transform.Find("Music/Slider")?.GetComponent<Slider>();
            musicMuteButton ??= transform.Find("Music/MusicButton")?.GetComponent<Button>();
        }

        private void Subscribe()
        {
            if (!_isBound || _isSubscribed || !isActiveAndEnabled)
                return;

            soundVolumeSlider?.onValueChanged.AddListener(HandleSoundVolumeChanged);
            musicVolumeSlider?.onValueChanged.AddListener(HandleMusicVolumeChanged);
            soundMuteButton?.onClick.AddListener(HandleSoundMuteClicked);
            musicMuteButton?.onClick.AddListener(HandleMusicMuteClicked);
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
                return;

            soundVolumeSlider?.onValueChanged.RemoveListener(HandleSoundVolumeChanged);
            musicVolumeSlider?.onValueChanged.RemoveListener(HandleMusicVolumeChanged);
            soundMuteButton?.onClick.RemoveListener(HandleSoundMuteClicked);
            musicMuteButton?.onClick.RemoveListener(HandleMusicMuteClicked);
            _isSubscribed = false;
        }

        private void RefreshControls(int soundVolume, int musicVolume)
        {
            soundVolumeSlider?.SetValueWithoutNotify(Mathf.Clamp01(soundVolume / 100f));
            musicVolumeSlider?.SetValueWithoutNotify(Mathf.Clamp01(musicVolume / 100f));
        }

        private void HandleSoundVolumeChanged(float value)
        {
            _onSoundChanged?.Invoke(Mathf.RoundToInt(Mathf.Clamp01(value) * 100f), _soundMuted);
        }

        private void HandleMusicVolumeChanged(float value)
        {
            _onMusicChanged?.Invoke(Mathf.RoundToInt(Mathf.Clamp01(value) * 100f), _musicMuted);
        }

        private void HandleSoundMuteClicked()
        {
            _soundMuted = !_soundMuted;
            _onSoundChanged?.Invoke(GetVolume(soundVolumeSlider), _soundMuted);
        }

        private void HandleMusicMuteClicked()
        {
            _musicMuted = !_musicMuted;
            _onMusicChanged?.Invoke(GetVolume(musicVolumeSlider), _musicMuted);
        }

        private static int GetVolume(Slider slider)
        {
            return slider == null
                ? 100
                : Mathf.RoundToInt(Mathf.Clamp01(slider.value) * 100f);
        }
    }
}
