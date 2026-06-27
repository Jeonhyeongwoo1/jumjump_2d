using System;
using System.Collections.Generic;
using JumJump.Interface;

namespace JumJump.Event
{
    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>(32);

        public void Subscribe<T>(Interface.EventHandler<T> handler) where T : struct
        {
            var eventType = typeof(T);

            if (_handlers.TryGetValue(eventType, out var existingHandler))
            {
                _handlers[eventType] = Delegate.Combine(existingHandler, handler);
                return;
            }

            _handlers.Add(eventType, handler);
        }

        public void Unsubscribe<T>(Interface.EventHandler<T> handler) where T : struct
        {
            var eventType = typeof(T);

            if (!_handlers.TryGetValue(eventType, out var existingHandler))
            {
                return;
            }

            var nextHandler = Delegate.Remove(existingHandler, handler);
            if (nextHandler == null)
            {
                _handlers.Remove(eventType);
                return;
            }

            _handlers[eventType] = nextHandler;
        }

        public void Publish<T>(in T ev) where T : struct
        {
            if (!_handlers.TryGetValue(typeof(T), out var handler))
            {
                return;
            }

            ((Interface.EventHandler<T>)handler).Invoke(in ev);
        }
    }
}
