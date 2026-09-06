using System;
using UnityEngine;

namespace Game.Events
{
    public abstract class EventChannelSO : ScriptableObject
    {
        public event Action OnRaised;

        public void Raise()
        {
            OnRaised?.Invoke();
        }
    }
}
