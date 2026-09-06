using System;
using UnityEngine;
using Game.Core.Economy;

namespace Game.Events
{
    public abstract class EventChannelSO<T> : EventChannelSO
    {
        public new event Action<T> OnRaised;

        public void Raise(T value)
        {
            OnRaised?.Invoke(value);
        }
    }

    [CreateAssetMenu(fileName = "OnItemChanged", menuName = "Game/Event Channel/On Item Changed")]
    public class OnItemChangedSO : EventChannelSO<Reward> { }
}
