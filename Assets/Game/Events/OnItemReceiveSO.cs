using UnityEngine;
using Game.Core.Economy;

namespace Game.Events
{
    [CreateAssetMenu(fileName = "OnItemReceive", menuName = "Game/Event Channel/On Item Receive")]
    public class OnItemReceiveSO : EventChannelSO<Reward> { }
}

