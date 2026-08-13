using System;
using System.Collections.Generic;

namespace DefaultNamespace
{
    /// <summary>
    /// [Duong] Maps each event type to its registered handler delegates.
    /// </summary>
    public class GameplayEventDispatcher
    {
        /// <summary>
        /// [Duong] EventType to the Action that handles it
        /// </summary>
        private readonly Dictionary<Type, List<Action<IGameplayEvent>>> handlersByEventType = new Dictionary<Type, List<Action<IGameplayEvent>>>();

        public void Register<TEvent>(IGameplayEventHandler<TEvent> handler) where TEvent : IGameplayEvent
        {
            Type eventType = typeof(TEvent);
            if (!handlersByEventType.TryGetValue(eventType, out List<Action<IGameplayEvent>> handlers))
            {
                handlers = new List<Action<IGameplayEvent>>();
                handlersByEventType[eventType] = handlers;
            }

            handlers.Add(gameplayEvent => handler.Handle((TEvent)gameplayEvent));
        }

        public void Dispatch(IGameplayEvent gameplayEvent)
        {
            if (gameplayEvent == null) throw new ArgumentNullException(nameof(gameplayEvent));
            if (!handlersByEventType.TryGetValue(gameplayEvent.GetType(), out List<Action<IGameplayEvent>> handlers)) return;

            foreach (Action<IGameplayEvent> handler in handlers)
                handler(gameplayEvent);
        }
    }
}
