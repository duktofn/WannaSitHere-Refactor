using System;

namespace Game.Core.Booster
{
    /// <summary>
    /// Abstract base for all boosters. Uses the Template Method pattern:
    /// <see cref="TryUse"/> checks <see cref="CanUse"/> before calling <see cref="Execute"/>,
    /// and fires <see cref="OnBoosterUsed"/> only on success.
    /// </summary>
    public abstract class Booster
    {
        public event Action OnBoosterUsed;

        /// <summary>
        /// Attempts to use the booster. Returns true if the booster was successfully executed.
        /// Inventory should only be deducted when this returns true.
        /// </summary>
        public bool TryUse()
        {
            if (!CanUse()) return false;
            Execute();
            OnBoosterUsed?.Invoke();
            return true;
        }

        /// <summary>Precondition check. Return false to prevent execution.</summary>
        protected abstract bool CanUse();

        /// <summary>Core booster logic. Only called when <see cref="CanUse"/> returns true.</summary>
        protected abstract void Execute();
    }
}