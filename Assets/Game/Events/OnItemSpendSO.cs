using UnityEngine;
using Game.Core.Economy;

namespace Game.Events
{
    [CreateAssetMenu(fileName = "OnItemSpend", menuName = "Game/Event Channel/On Item Spend")]
    public class OnItemSpendSO : EventChannelSO<Reward> { }
}

