using System.Collections.Generic;

namespace Game.Core.Booster
{
    public class MoveHistory
    {
        private readonly Stack<MoveRecord> _history = new();

        public int Count => _history.Count;
        public bool HasHistory => _history.Count > 0;

        public void Record(MoveRecord record)
        {
            _history.Push(record);
        }

        public bool TryPop(out MoveRecord record)
        {
            if (_history.Count == 0)
            {
                record = default;
                return false;
            }

            record = _history.Pop();
            return true;
        }

        public void Clear()
        {
            _history.Clear();
        }
    }
}

