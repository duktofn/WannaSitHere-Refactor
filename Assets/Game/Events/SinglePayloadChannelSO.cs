using System;
using UnityEngine;

namespace Game.Events
{
    [CreateAssetMenu(fileName = "Single Payload", menuName = "Game/Event Channel/1 Payload")]
    public class EventChannelSO<T> : ScriptableObject
    {
        public event Action<T> OnRaised;

        public void Raise(T value)
        {
            OnRaised?.Invoke(value);
        }
    }
}