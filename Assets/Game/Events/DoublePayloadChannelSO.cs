using System;
using System.IO;
using UnityEngine;

namespace Game.Events
{
    [CreateAssetMenu(fileName = "Double Payload", menuName = "Game/Event Channel/2 Payload")]
    public class EventChannelSO<T1, T2> : ScriptableObject
    {
        public event Action<T1, T2> OnRaised;

        public void Raise(T1 val1, T2 val2)
        {
            OnRaised?.Invoke(val1, val2);
        }
    }
}