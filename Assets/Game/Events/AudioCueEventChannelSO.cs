using UnityEngine;

namespace Game.Events
{
    [CreateAssetMenu(fileName = "OnAudioCue", menuName = "Game/Event Channel/Audio Cue")]
    public sealed class AudioCueEventChannelSO : EventChannelSO<AudioCueId>
    {
    }
}
