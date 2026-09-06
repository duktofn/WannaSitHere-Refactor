using System.Collections.Generic;
using UnityEngine;
using Game.Events;
using Cysharp.Threading.Tasks;

namespace Game.View.UI
{
    public class ButtonEventRaiser : MonoBehaviour
    {
        [Header("Event Channels")]
        [SerializeField] private List<EventChannelSO> eventChannels;

        [Header("Timing")]
        [SerializeField] private float delay;

        public void RaiseAll()
        {
            if (delay > 0f)
            {
                RaiseAllWithDelay().Forget();
            }
            else
            {
                RaiseImmediately();
            }
        }

        public void RaiseImmediately()
        {
            foreach (var channel in eventChannels)
            {
                channel?.Raise();
            }
        }

        private async UniTaskVoid RaiseAllWithDelay()
        {
            await UniTask.Delay((int)(delay * 1000));
            RaiseImmediately();
        }
    }
}

