using System;
using Game.App;
using UnityEngine;

namespace Game.App.Tutorial
{
    public sealed class MechanicTutorial : MonoBehaviour
    {
        [SerializeField] private MechanicTutorialID _id;
        [SerializeField] private TutorialTrigger _trigger;
        [SerializeField] private TutorialStep[] _steps;

        private int _currentStepIndex;
        private bool _isRunning;

        public MechanicTutorialID ID => _id;
        public TutorialTrigger Trigger => _trigger;
        public event Action<MechanicTutorial> Completed;

        public void Configure(MechanicTutorialID id, TutorialTrigger trigger, TutorialStep[] steps)
        {
            if (_isRunning)
                Cancel();

            _id = id;
            _trigger = trigger;
            _steps = steps ?? Array.Empty<TutorialStep>();
        }

        public void BindPresentation(IGamePresentation presentation)
        {
            if (_steps == null)
                return;

            for (int i = 0; i < _steps.Length; i++)
                _steps[i]?.BindPresentation(presentation);
        }

        public void Begin()
        {
            if (_isRunning)
                Cancel();
            _currentStepIndex = 0;
            _isRunning = true;
            StartCurrentStep();
        }

        public void Tick()
        {
            if (_isRunning && _steps != null && _currentStepIndex < _steps.Length)
                _steps[_currentStepIndex]?.UpdateStep();
        }

        public void Cancel()
        {
            if (!_isRunning)
                return;

            if (_steps != null && _currentStepIndex >= 0 && _currentStepIndex < _steps.Length)
            {
                TutorialStep step = _steps[_currentStepIndex];
                if (step != null)
                {
                    step.Completed -= HandleStepCompleted;
                    step.EndStep();
                }
            }

            _isRunning = false;
        }

        private void StartCurrentStep()
        {
            while (_steps != null && _currentStepIndex < _steps.Length && _steps[_currentStepIndex] == null)
                _currentStepIndex++;

            if (_steps == null || _currentStepIndex >= _steps.Length)
            {
                _isRunning = false;
                Completed?.Invoke(this);
                return;
            }

            TutorialStep step = _steps[_currentStepIndex];
            step.Completed += HandleStepCompleted;
            step.StartStep();
        }

        private void HandleStepCompleted(TutorialStep completedStep)
        {
            if (!_isRunning || _steps == null || _currentStepIndex >= _steps.Length ||
                _steps[_currentStepIndex] != completedStep)
            {
                return;
            }

            completedStep.Completed -= HandleStepCompleted;
            completedStep.EndStep();
            _currentStepIndex++;
            StartCurrentStep();
        }
    }
}
