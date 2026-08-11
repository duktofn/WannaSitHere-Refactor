using Game.Shared;

namespace Game.Domain.Condition
{
    public class ConditionRuntimeData
    {
        public readonly ConditionType Type;
        public readonly ConditionTarget Target;
        public readonly PersonTrait TargetTrait; // for Person ConditionType
        public readonly Food FoodTarget;         // for Food ConditionType
        public readonly string Description;
        public readonly string AngryDescription;

        public ConditionRuntimeData(ConditionType type, 
                                    ConditionTarget target, 
                                    PersonTrait targetTrait, 
                                    Food foodTarget, 
                                    string description, 
                                    string angryDescription)
        {
            Type = type;
            Target = target;
            TargetTrait = targetTrait;
            FoodTarget = foodTarget;
            Description = description;
            AngryDescription = angryDescription;
        }
    }
}