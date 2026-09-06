using UnityEngine;
using Game.Core.Economy;
using Game.Events;

namespace Game.View.UI
{
    public class RewardEventRaiser : MonoBehaviour
    {
        [Header("Payload")]
        [SerializeField] private Reward reward;

        [Header("Event Channel")]
        [SerializeField] private EventChannelSO<Reward> eventChannel;

        public void RaiseEvent()
        {
            eventChannel?.Raise(reward);
        }
    }
}

