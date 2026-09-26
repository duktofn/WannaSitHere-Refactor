using Game.Events;
using UnityEngine;

namespace Game.View.Audio
{
    /// <summary>
    /// Bridges accepted outcome channels and the typed gameplay audio cue channel to the
    /// scene-owned <see cref="AudioPlayer"/>.
    /// </summary>
    public sealed class AudioEventBinder : MonoBehaviour
    {
        [SerializeField] private AudioPlayer audioPlayer;
        [SerializeField] private AudioCueEventChannelSO onAudioCue;

        [Header("Game Flow")]
        [SerializeField] private VoidEventChannelSO onWinEvent;
        [SerializeField] private VoidEventChannelSO onLoseEvent;
        [SerializeField] private bool playMainMenuOnStart = true;

        private readonly EventListener _listener = new();

        private void Awake()
        {
            if (audioPlayer == null)
                audioPlayer = GetComponent<AudioPlayer>();
        }

        private void OnEnable()
        {
            _listener.Listen(onAudioCue, HandleAudioCue);
            _listener.Listen(onWinEvent, HandleWin);
            _listener.Listen(onLoseEvent, HandleLose);
        }

        private void Start()
        {
            if (playMainMenuOnStart)
                audioPlayer?.PlayMusic(MusicId.MainMenu);
        }

        private void OnDisable()
        {
            _listener.UnbindAll();
        }

        private void HandleAudioCue(AudioCueId cue)
        {
            audioPlayer?.Play(cue);
        }

        private void HandleWin()
        {
            audioPlayer?.Play(AudioCueId.Win);
        }

        private void HandleLose()
        {
            audioPlayer?.Play(AudioCueId.Lose);
        }

    }
}
