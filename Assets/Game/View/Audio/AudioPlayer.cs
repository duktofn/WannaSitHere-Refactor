using System;
using System.Collections;
using System.Collections.Generic;
using Game.Events;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.View.Audio
{
    /// <summary>
    /// Scene-owned audio playback service. It owns Unity audio objects and catalog lookup;
    /// gameplay and application layers only communicate through audio cue identifiers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioPlayer : MonoBehaviour
    {
        private const float MutedDb = -80f;

        [Header("Catalog and Mixer")]
        [SerializeField] private AudioCatalogSO catalog;
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup uiMixerGroup;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private AudioMixerGroup musicMixerGroup;

        [Header("Sources")]
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private AudioSource[] sfxSources;
        [SerializeField] private AudioSource[] musicSources;

        private readonly Dictionary<AudioCueId, float> _nextAllowedCueTime = new();
        private readonly Dictionary<AudioCueId, AudioClip> _lastCueClip = new();
        private readonly HashSet<AudioCueId> _missingCueWarnings = new();
        private readonly HashSet<MusicId> _missingMusicWarnings = new();

        private int _nextSfxSourceIndex;
        private int _activeMusicSourceIndex = -1;
        private MusicId _activeMusic;
        private bool _hasActiveMusic;
        private Coroutine _musicFade;

        private void Awake()
        {
            ConfigureSource(uiSource, uiMixerGroup, false);

            if (sfxSources == null || sfxSources.Length < 3)
                Debug.LogError("[AudioPlayer] Assign at least three SFX AudioSources.", this);

            if (musicSources == null || musicSources.Length < 2)
                Debug.LogError("[AudioPlayer] Assign two Music AudioSources for cross-fade.", this);

            if (sfxSources != null)
            {
                for (int i = 0; i < sfxSources.Length; i++)
                    ConfigureSource(sfxSources[i], sfxMixerGroup, false);
            }

            if (musicSources != null)
            {
                for (int i = 0; i < musicSources.Length; i++)
                    ConfigureSource(musicSources[i], musicMixerGroup, true);
            }
        }

        private void OnDestroy()
        {
            if (_musicFade != null)
                StopCoroutine(_musicFade);
        }

        /// <summary>Plays one catalogued UI or SFX cue.</summary>
        public void Play(AudioCueId cue)
        {
            if (catalog == null)
            {
                Debug.LogError("[AudioPlayer] AudioCatalogSO is not assigned.", this);
                return;
            }

            if (!catalog.TryGetCue(cue, out AudioCatalogSO.CueEntry entry))
            {
                WarnMissingCue(cue, "catalog entry");
                return;
            }

            AudioClip clip = SelectClip(cue, entry.Clips);
            if (clip == null)
            {
                WarnMissingCue(cue, "AudioClip");
                return;
            }

            float now = Time.unscaledTime;
            if (_nextAllowedCueTime.TryGetValue(cue, out float nextAllowed) && now < nextAllowed)
                return;

            if (entry.Cooldown > 0f)
                _nextAllowedCueTime[cue] = now + entry.Cooldown;

            float pitch = UnityEngine.Random.Range(
                Mathf.Min(entry.PitchMin, entry.PitchMax),
                Mathf.Max(entry.PitchMin, entry.PitchMax));

            if (entry.Bus == AudioBus.UI)
            {
                PlayUi(clip, entry, pitch);
            }
            else
            {
                PlaySfx(clip, entry, pitch);
            }
        }

        /// <summary>Starts or cross-fades to a catalogued music track.</summary>
        public void PlayMusic(MusicId music)
        {
            if (catalog == null)
            {
                Debug.LogError("[AudioPlayer] AudioCatalogSO is not assigned.", this);
                return;
            }

            if (musicSources == null || musicSources.Length < 2)
            {
                Debug.LogError("[AudioPlayer] Music source pool is not configured.", this);
                return;
            }

            if (!catalog.TryGetMusic(music, out AudioCatalogSO.MusicEntry entry))
            {
                WarnMissingMusic(music, "catalog entry");
                return;
            }

            if (entry.Clip == null)
            {
                WarnMissingMusic(music, "AudioClip");
                return;
            }

            if (_hasActiveMusic && _activeMusic == music)
                return;

            int nextIndex = _activeMusicSourceIndex < 0
                ? 0
                : (_activeMusicSourceIndex + 1) % musicSources.Length;

            AudioSource nextSource = musicSources[nextIndex];
            if (nextSource == null)
            {
                Debug.LogError($"[AudioPlayer] Music AudioSource {nextIndex} is missing.", this);
                return;
            }

            AudioSource previousSource = _activeMusicSourceIndex >= 0
                ? musicSources[_activeMusicSourceIndex]
                : null;

            if (_musicFade != null)
                StopCoroutine(_musicFade);

            ConfigureSource(nextSource, musicMixerGroup, true);
            nextSource.Stop();
            nextSource.clip = entry.Clip;
            nextSource.loop = true;
            nextSource.volume = previousSource == null ? entry.Volume : 0f;
            nextSource.Play();

            _activeMusicSourceIndex = nextIndex;
            _activeMusic = music;
            _hasActiveMusic = true;

            if (previousSource != null && previousSource != nextSource)
            {
                _musicFade = StartCoroutine(CrossFadeMusic(
                    previousSource,
                    nextSource,
                    Mathf.Max(0f, entry.Volume),
                    Mathf.Max(0f, entry.FadeDuration)));
            }
        }

        /// <summary>Stops all music sources and clears the active music state.</summary>
        public void StopMusic()
        {
            if (_musicFade != null)
            {
                StopCoroutine(_musicFade);
                _musicFade = null;
            }

            if (musicSources != null)
            {
                for (int i = 0; i < musicSources.Length; i++)
                {
                    if (musicSources[i] == null) continue;
                    musicSources[i].Stop();
                    musicSources[i].clip = null;
                    musicSources[i].volume = 0f;
                }
            }

            _activeMusicSourceIndex = -1;
            _hasActiveMusic = false;
        }

        /// <summary>
        /// Applies persisted percentage volumes and mute flags to the exposed Mixer
        /// parameters. SoundVolume controls both UI and SFX through the Sound bus.
        /// </summary>
        public void ApplySettings(
            int soundVolume,
            bool soundMuted,
            int musicVolume,
            bool musicMuted)
        {
            SetMixerVolume("SoundVolume", soundVolume, soundMuted);
            SetMixerVolume("MusicVolume", musicVolume, musicMuted);
        }

        private void PlayUi(AudioClip clip, AudioCatalogSO.CueEntry entry, float pitch)
        {
            if (uiSource == null)
            {
                Debug.LogError("[AudioPlayer] UI AudioSource is missing.", this);
                return;
            }

            ConfigureSource(uiSource, uiMixerGroup, false);
            uiSource.pitch = pitch;

            if (!entry.AllowOverlap && uiSource.isPlaying)
                return;

            if (entry.AllowOverlap)
            {
                uiSource.volume = 1f;
                uiSource.PlayOneShot(clip, Mathf.Clamp01(entry.Volume));
            }
            else
            {
                uiSource.Stop();
                uiSource.clip = clip;
                uiSource.volume = Mathf.Clamp01(entry.Volume);
                uiSource.Play();
            }
        }

        private void PlaySfx(AudioClip clip, AudioCatalogSO.CueEntry entry, float pitch)
        {
            if (sfxSources == null || sfxSources.Length == 0)
            {
                Debug.LogError("[AudioPlayer] SFX source pool is missing.", this);
                return;
            }

            AudioSource source = FindFreeSfxSource();
            if (source == null && !entry.AllowOverlap)
                return;

            if (source == null)
            {
                source = sfxSources[_nextSfxSourceIndex % sfxSources.Length];
                _nextSfxSourceIndex = (_nextSfxSourceIndex + 1) % sfxSources.Length;
            }

            if (source == null)
            {
                Debug.LogError("[AudioPlayer] SFX source pool contains a missing AudioSource.", this);
                return;
            }

            ConfigureSource(source, sfxMixerGroup, false);
            source.pitch = pitch;
            source.volume = Mathf.Clamp01(entry.Volume);

            if (entry.AllowOverlap)
            {
                source.PlayOneShot(clip);
            }
            else
            {
                source.Stop();
                source.clip = clip;
                source.Play();
            }
        }

        private AudioSource FindFreeSfxSource()
        {
            if (sfxSources == null || sfxSources.Length == 0)
                return null;

            for (int i = 0; i < sfxSources.Length; i++)
            {
                int index = (_nextSfxSourceIndex + i) % sfxSources.Length;
                if (sfxSources[index] != null && !sfxSources[index].isPlaying)
                {
                    _nextSfxSourceIndex = (index + 1) % sfxSources.Length;
                    return sfxSources[index];
                }
            }

            return null;
        }

        private AudioClip SelectClip(AudioCueId cue, IReadOnlyList<AudioClip> clips)
        {
            if (clips == null || clips.Count == 0)
                return null;

            List<AudioClip> validClips = null;
            for (int i = 0; i < clips.Count; i++)
            {
                if (clips[i] == null) continue;
                validClips ??= new List<AudioClip>();
                validClips.Add(clips[i]);
            }

            if (validClips == null || validClips.Count == 0)
                return null;

            AudioClip selected = validClips[UnityEngine.Random.Range(0, validClips.Count)];
            if (validClips.Count > 1 && _lastCueClip.TryGetValue(cue, out AudioClip previous))
            {
                int guard = 0;
                while (selected == previous && guard++ < 4)
                    selected = validClips[UnityEngine.Random.Range(0, validClips.Count)];
            }

            _lastCueClip[cue] = selected;
            return selected;
        }

        private IEnumerator CrossFadeMusic(
            AudioSource previousSource,
            AudioSource nextSource,
            float nextVolume,
            float duration)
        {
            if (duration <= 0f)
            {
                previousSource.Stop();
                previousSource.volume = 0f;
                nextSource.volume = nextVolume;
                _musicFade = null;
                yield break;
            }

            float previousStartVolume = previousSource.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                previousSource.volume = Mathf.Lerp(previousStartVolume, 0f, t);
                nextSource.volume = Mathf.Lerp(0f, nextVolume, t);
                yield return null;
            }

            previousSource.Stop();
            previousSource.volume = 0f;
            nextSource.volume = nextVolume;
            _musicFade = null;
        }

        private void ConfigureSource(AudioSource source, AudioMixerGroup outputGroup, bool loop)
        {
            if (source == null) return;

            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.loop = loop;
            source.outputAudioMixerGroup = outputGroup;
        }

        private void SetMixerVolume(string parameter, int volume, bool muted)
        {
            if (mixer == null)
            {
                Debug.LogError("[AudioPlayer] GameAudio mixer is not assigned.", this);
                return;
            }

            float normalized = Mathf.Clamp01(volume / 100f);
            float decibels = muted || normalized <= 0f
                ? MutedDb
                : Mathf.Log10(normalized) * 20f;

            if (!mixer.SetFloat(parameter, decibels))
                Debug.LogWarning($"[AudioPlayer] Mixer parameter '{parameter}' is not exposed.", this);
        }

        private void WarnMissingCue(AudioCueId cue, string detail)
        {
            if (_missingCueWarnings.Add(cue))
                Debug.LogWarning($"[AudioPlayer] Cue {cue} is missing {detail}.", this);
        }

        private void WarnMissingMusic(MusicId music, string detail)
        {
            if (_missingMusicWarnings.Add(music))
                Debug.LogWarning($"[AudioPlayer] Music {music} is missing {detail}.", this);
        }
    }
}
