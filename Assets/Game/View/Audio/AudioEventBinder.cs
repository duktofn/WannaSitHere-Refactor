using Game.Events;
using UnityEngine;

namespace Game.View.Audio
{
    /// <summary>
    /// Bridges existing game-flow channels and the typed gameplay audio cue channel to the
    /// scene-owned <see cref="AudioPlayer"/>.
    /// </summary>
    public sealed class AudioEventBinder : MonoBehaviour
    {
        [SerializeField] private AudioPlayer audioPlayer;
        [SerializeField] private AudioCueEventChannelSO onAudioCue;

        [Header("Game Flow")]
        [SerializeField] private VoidEventChannelSO onWinEvent;
        [SerializeField] private VoidEventChannelSO onLoseEvent;
        [SerializeField] private VoidEventChannelSO onPlayGameEvent;
        [SerializeField] private VoidEventChannelSO onMainPanelEvent;
        [SerializeField] private VoidEventChannelSO onBackToHomeEvent;
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
            _listener.Listen(onPlayGameEvent, HandlePlayGame);
            _listener.Listen(onMainPanelEvent, HandleMainPanel);
            _listener.Listen(onBackToHomeEvent, HandleMainPanel);
        }

        private void Start()
        {
            if (playMainMenuOnStart)
                HandleMainPanel();
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

        private void HandlePlayGame()
        {
            audioPlayer?.PlayMusic(MusicId.InGame);
        }

        private void HandleMainPanel()
        {
            audioPlayer?.PlayMusic(MusicId.MainMenu);
        }
    }
}
