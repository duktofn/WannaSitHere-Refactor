using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "New Game Config", menuName = "Game/New Game Config")]
    public class GameConfigSO : ScriptableObject
    {
        public int maxConditionPerPerson;
        public int moreMoveAmount;
        
    }
}