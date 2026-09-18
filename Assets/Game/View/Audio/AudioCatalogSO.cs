using System;
using System.Collections.Generic;
using Game.Events;
using UnityEngine;

namespace Game.View.Audio
{
    public enum AudioBus
    {
        UI,
        SFX
    }

    public enum MusicId
    {
        MainMenu,
        InGame
    }

    [CreateAssetMenu(fileName = "AudioCatalog", menuName = "Game/Audio/Audio Catalog")]
    public sealed class AudioCatalogSO : ScriptableObject
    {
        [Serializable]
        public sealed class CueEntry
        {
            [SerializeField] private AudioCueId cue;
            [SerializeField] private List<AudioClip> clips = new();
            [SerializeField] private AudioBus bus = AudioBus.SFX;
            [SerializeField, Range(0f, 1f)] private float volume = 1f;
            [SerializeField, Min(0f)] private float pitchMin = 1f;
            [SerializeField, Min(0f)] private float pitchMax = 1f;
            [SerializeField, Min(0f)] private float cooldown;
            [SerializeField] private bool allowOverlap = true;

            public AudioCueId Cue => cue;
            public IReadOnlyList<AudioClip> Clips => clips;
            public AudioBus Bus => bus;
            public float Volume => volume;
            public float PitchMin => pitchMin;
            public float PitchMax => pitchMax;
            public float Cooldown => cooldown;
            public bool AllowOverlap => allowOverlap;

            public CueEntry(AudioCueId cue)
            {
                this.cue = cue;
                bus = cue == AudioCueId.ButtonClick ||
                      cue == AudioCueId.Transition ||
                      cue == AudioCueId.Claim ||
                      cue == AudioCueId.Spend
                    ? AudioBus.UI
                    : AudioBus.SFX;
                cooldown = cue switch
                {
                    AudioCueId.ButtonClick => 0.04f,
                    AudioCueId.Win => 0.4f,
                    AudioCueId.Lose => 0.4f,
                    _ => 0f
                };
                allowOverlap = cue == AudioCueId.ButtonClick;
            }

            public CueEntry()
            {
            }
        }

        [Serializable]
        public sealed class MusicEntry
        {
            [SerializeField] private MusicId music;
            [SerializeField] private AudioClip clip;
            [SerializeField, Range(0f, 1f)] private float volume = 1f;
            [SerializeField, Min(0f)] private float fadeDuration = 0.5f;

            public MusicId Music => music;
            public AudioClip Clip => clip;
            public float Volume => volume;
            public float FadeDuration => fadeDuration;

            public MusicEntry(MusicId music)
            {
                this.music = music;
            }

            public MusicEntry()
            {
            }
        }

        [SerializeField] private List<CueEntry> cues = new();
        [SerializeField] private List<MusicEntry> music = new();

        public IReadOnlyList<CueEntry> Cues => cues;
        public IReadOnlyList<MusicEntry> Music => music;

        public bool TryGetCue(AudioCueId cue, out CueEntry entry)
        {
            if (cues != null)
            {
                for (int i = 0; i < cues.Count; i++)
                {
                    if (cues[i] != null && cues[i].Cue == cue)
                    {
                        entry = cues[i];
                        return true;
                    }
                }
            }

            entry = null;
            return false;
        }

        public bool TryGetMusic(MusicId musicId, out MusicEntry entry)
        {
            if (music != null)
            {
                for (int i = 0; i < music.Count; i++)
                {
                    if (music[i] != null && music[i].Music == musicId)
                    {
                        entry = music[i];
                        return true;
                    }
                }
            }

            entry = null;
            return false;
        }

        /// <summary>
        /// Ensures the catalog exposes one editable entry per supported cue and music id.
        /// It is used while authoring the single catalog asset; clip references remain
        /// Inspector-owned data.
        /// </summary>
        public void EnsureRequiredEntries()
        {
            cues ??= new List<CueEntry>();
            music ??= new List<MusicEntry>();

            foreach (AudioCueId cue in Enum.GetValues(typeof(AudioCueId)))
            {
                if (!ContainsCue(cue))
                    cues.Add(new CueEntry(cue));
            }

            foreach (MusicId musicId in Enum.GetValues(typeof(MusicId)))
            {
                if (!ContainsMusic(musicId))
                    music.Add(new MusicEntry(musicId));
            }
        }

        private bool ContainsCue(AudioCueId cue)
        {
            if (cues == null) return false;
            for (int i = 0; i < cues.Count; i++)
            {
                if (cues[i] != null && cues[i].Cue == cue)
                    return true;
            }

            return false;
        }

        private bool ContainsMusic(MusicId musicId)
        {
            if (music == null) return false;
            for (int i = 0; i < music.Count; i++)
            {
                if (music[i] != null && music[i].Music == musicId)
                    return true;
            }

            return false;
        }
    }
}
