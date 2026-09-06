using System;
using UnityEngine;

namespace Game.Events
{
    public abstract class EventChannelSO<T1, T2> : EventChannelSO
    {
        public new event Action<T1, T2> OnRaised;

        public void Raise(T1 val1, T2 val2)
        {
            OnRaised?.Invoke(val1, val2);
        }
    }
}
