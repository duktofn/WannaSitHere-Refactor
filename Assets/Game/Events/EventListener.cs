using System;
using System.Collections.Generic;

namespace Game.Events
{
    public sealed class EventListener
    {
        private readonly List<Action> _unbindActions = new();

        public void Listen(EventChannelSO channel, Action handler)
        {
            if (channel == null || handler == null)
                return;

            channel.OnRaised += handler;
            _unbindActions.Add(() => channel.OnRaised -= handler);
        }

        public void Listen<T>(EventChannelSO<T> channel, Action<T> handler)
        {
            if (channel == null || handler == null)
                return;

            channel.OnRaised += handler;
            _unbindActions.Add(() => channel.OnRaised -= handler);
        }

        public void Listen<T1, T2>(EventChannelSO<T1, T2> channel, Action<T1, T2> handler)
        {
            if (channel == null || handler == null)
                return;

            channel.OnRaised += handler;
            _unbindActions.Add(() => channel.OnRaised -= handler);
        }

        public void UnbindAll()
        {
            foreach (Action unbind in _unbindActions)
                unbind();

            _unbindActions.Clear();
        }
    }
}
