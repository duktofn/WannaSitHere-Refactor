using System;
using System.Collections.Generic;

namespace Game.Editor.Difficulty
{
    [Serializable]
    public sealed class DifficultyAnalysisSettings
    {
        public int MaxNodes = 2000000;
        public bool UseSolver = true;
    }

    [Serializable]
    public sealed class DifficultyAnalysisResult
    {
        public string LevelName, ModelId, InputFingerprint, Message, MoveSource;
        public int ModelVersion;
        public DifficultyAnalysisStatus Status;
        public bool HasScore, Valid, SolverRan, SolverExact, HasExactMoves;
        public int N, S, W, E, M, MainPeople, WaitPeople, AngryMain, PersonConditions, FoodConditions;
        public int TotalConditions, IgnoredConditions, MinMoves = -1, Slack, BestFoundMoves = -1;
        public int LikePersonConditions, MaxDependencyDepth, CyclePeople, BlockedPeople;
        public long Nodes, SolutionsFound;
        public double MeanSeatDegree, Tightness, Occupancy, LikeDensity, DependencyDepth, CycleRatio;
        public double BudgetTightness, Rearrangement, ConditionComplexity, DependencyComplexity, MovePressure;
        public double ConditionContribution, DependencyContribution, MoveContribution, DifficultyScore, ElapsedSeconds;
        public DifficultyAnalysisSettings Settings;
        public List<string> Problems = new(), Warnings = new();
        public List<DifficultyCellRow> Cells = new();
        public List<DifficultyPersonRow> People = new();
        public List<DifficultyConditionRow> Conditions = new();
        public List<DifficultyCountRow> Counts = new();
        public List<DifficultySeatProbabilityRow> SeatProbabilities = new();
        public List<string> BestAssignment = new();
    }

    [Serializable]
    public sealed class DifficultyCellRow
    {
        public string Grid, Type, Food, Person;
        public int X, Y, AdjacentSeats, AdjacentFoods;
        public string Neighbors;
    }

    [Serializable]
    public sealed class DifficultyPersonRow
    {
        public int Id, X, Y, ConditionCount, SatisfiedConditions;
        public string Name, Trait, Grid;
        public bool InitiallyHappy;
        public int DependencyLayer = -1;
        public bool InDependencyCycle;
        public double ExpectedSeatRatio, Tightness;
    }

    [Serializable]
    public sealed class DifficultyConditionRow
    {
        public int PersonId, Index, MatchingOthers, FoodAdjacentSeats;
        public string Person, Trait, Label, Description;
        public bool Ignored, InitiallySatisfied;
        public double Probability;
    }

    [Serializable]
    public sealed class DifficultySeatProbabilityRow
    {
        public int PersonId, X, Y, AdjacentSeats;
        public List<double> ConditionProbabilities = new();
        public double JointProbability;
    }

    [Serializable]
    public sealed class DifficultyCountRow
    {
        public string Name;
        public int Count;
    }

    public enum DifficultyAnalysisStatus { Running, Complete, InvalidInput, LimitExceeded, Cancelled, Unsolvable, Partial }
}
