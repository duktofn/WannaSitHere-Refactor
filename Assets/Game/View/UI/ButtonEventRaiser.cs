using System.Collections.Generic;
using UnityEngine;
using Game.Events;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

namespace Game.View.UI
{
    public class ButtonEventRaiser : MonoBehaviour
    {
        [Header("Event Channels")]
        [SerializeField] private List<EventChannelSO> eventChannels;

        [Header("Timing")]
        [SerializeField] private float delay;

        [Header("Button Setting")]
        [SerializeField] private bool isPressedOnce;
        [SerializeField] private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.interactable = true;
            var colors = button.colors;
            colors.disabledColor = colors.normalColor; 
            button.colors = colors;
        }

        public void RaiseAll()
        {
            if (isPressedOnce)
            {
                button.interactable = false;
            }

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

