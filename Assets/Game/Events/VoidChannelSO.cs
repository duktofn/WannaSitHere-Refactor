using System;
using UnityEngine;

namespace Game.Events
{
    [CreateAssetMenu(fileName = "No Payload", menuName = "Game/Event Channel/No Payload")]
    public class VoidEventChannelSO : ScriptableObject
    {
        public event Action OnRaised;

        public void Raise()
        {
            OnRaised?.Invoke();
            Debug.Log($"{GetType().Name} Raised");
        }
    }
}
