using System;
using System.Collections.Generic;

namespace StateMachineLib
{
    public class State<TTrig, TName>
    {
        private State<TTrig, TName> _allTransition = null;
        private readonly Dictionary<TTrig, State<TTrig, TName>> _transitions = new Dictionary<TTrig, State<TTrig, TName>>();
        public event Action<TTrig> OnEnter;
        public event Action<TTrig> OnExit;
        public TName Name { get; set; }
        public bool Ignoring { get; set; }

        public State()
        {
        }

        public State(Action<TTrig> onEnter, Action<TTrig> onExit = null)
        {
            OnEnter += onEnter;
            OnExit += onExit;
        }

        public IReadOnlyDictionary<TTrig, State<TTrig, TName>> Transitions => _transitions;

        public void Activate(TTrig value)
        {
            OnEnter?.Invoke(value);
        }

        public void AddTransition(TTrig triggerValue, State<TTrig, TName> targetState)
        {
            _transitions.Add(triggerValue, targetState);
        }

        public void AddAllTransition(State<TTrig, TName> targetState)
        {
            _allTransition = targetState;
        }

        public virtual State<TTrig, TName> Next(TTrig trigValue)
        {
            OnExit?.Invoke(trigValue);
            _transitions.TryGetValue(trigValue, out var state);
            if (state == null)
            {
                if (_allTransition != null)
                {
                    return _allTransition;
                }
                else if (Ignoring)
                {
                    return null;
                }
                else
                {
                    throw new KeyNotFoundException(
                        $"Cannot find tranistion with key {trigValue} from current state {Name}");
                }
            }

            return state;
        }
    }
}