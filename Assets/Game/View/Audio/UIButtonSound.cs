using Game.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Game.View.Audio
{
    /// <summary>
    /// View-only click feedback. Gameplay event raising remains owned by ButtonEventRaiser
    /// and other button listeners already present on the same Button.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonSound : MonoBehaviour
    {
        [SerializeField] private AudioPlayer audioPlayer;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            _button?.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            _button?.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            audioPlayer?.Play(AudioCueId.ButtonClick);
        }
    }
}
