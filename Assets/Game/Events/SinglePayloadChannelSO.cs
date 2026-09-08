using System;

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
}
