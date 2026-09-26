using System;
using Game.App;
using UnityEngine;

namespace Game.App.Tutorial
{
    public abstract class TutorialStep : MonoBehaviour
    {
        private IGamePresentation _gamePresentation;

        public event Action<TutorialStep> Completed;

        protected IGamePresentation GamePresentation => _gamePresentation;

        public void BindPresentation(IGamePresentation gamePresentation)
        {
            _gamePresentation = gamePresentation;
        }

        public abstract void StartStep();
        public abstract void UpdateStep();
        public abstract void EndStep();

        protected void CompleteStep()
        {
            Completed?.Invoke(this);
        }
    }
}
