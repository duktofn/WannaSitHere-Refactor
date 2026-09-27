using System;
using System.Collections.Generic;
using Game.App;

namespace Game.App.Tutorial
{
    public sealed class TutorialService
    {
        private readonly MechanicTutorial[] _tutorials;
        private readonly HashSet<MechanicTutorialID> _completed = new HashSet<MechanicTutorialID>();
        private MechanicTutorial _activeTutorial;

        public event Action<TutorialTrigger> TriggerFired;
        public event Action<MechanicTutorialID> TutorialCompleted;
        public bool HasActiveTutorial => _activeTutorial != null;

        public TutorialService(IEnumerable<MechanicTutorial> tutorials, IGamePresentation presentation = null)
        {
            _tutorials = tutorials == null ? Array.Empty<MechanicTutorial>() : new List<MechanicTutorial>(tutorials).ToArray();

            for (int i = 0; i < _tutorials.Length; i++)
                _tutorials[i]?.BindPresentation(presentation);
        }

        public void RestoreCompleted(IEnumerable<MechanicTutorialID> tutorialIds = null)
        {
            _completed.Clear();
            if (tutorialIds == null)
                return;

            foreach (MechanicTutorialID tutorialId in tutorialIds)
                _completed.Add(tutorialId);
        }

        public bool IsCompleted(MechanicTutorialID tutorialId)
        {
            return _completed.Contains(tutorialId);
        }

        public bool TryStart(TutorialTrigger trigger)
        {
            TriggerFired?.Invoke(trigger);

            if (_activeTutorial != null)
                return false;

            for (int i = 0; i < _tutorials.Length; i++)
            {
                MechanicTutorial tutorial = _tutorials[i];
                if (tutorial == null || tutorial.Trigger != trigger || _completed.Contains(tutorial.ID))
                    continue;

                _activeTutorial = tutorial;
                _activeTutorial.Completed += HandleTutorialCompleted;
                _activeTutorial.Begin();
                return true;
            }

            return false;
        }

        public void Tick()
        {
            _activeTutorial?.Tick();
        }

        public void Cancel()
        {
            if (_activeTutorial == null)
                return;

            _activeTutorial.Completed -= HandleTutorialCompleted;
            _activeTutorial.Cancel();
            _activeTutorial = null;
        }

        private void HandleTutorialCompleted(MechanicTutorial tutorial)
        {
            if (_activeTutorial != tutorial)
                return;

            _activeTutorial.Completed -= HandleTutorialCompleted;
            _activeTutorial = null;
            _completed.Add(tutorial.ID);
            TutorialCompleted?.Invoke(tutorial.ID);
        }
    }

    public enum TutorialTrigger
    {
        FirstTimePlaying,
        LevelReady,
        SuccessfulMove,
        MoreMovesBoosterUsed,
        UndoBoosterUsed,
        RemoveBoosterUsed,
        LevelTwoReady
    }

    public enum MechanicTutorialID
    {
        Drag,
        Condition,
        MoreMovesBooster,
        UndoBooster,
        RemoveBooster,
    }
}
