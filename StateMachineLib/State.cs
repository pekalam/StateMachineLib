using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StateMachineLib
{
    public class State<TTrig, TName>
    {
        private State<TTrig, TName> _allTransition = null;
        protected readonly Dictionary<TTrig, State<TTrig, TName>> _transitions = new Dictionary<TTrig, State<TTrig, TName>>();
        public bool IsAsyncEnter { get; private set; }
        public bool IsAsyncExit { get; private set; }
        public event Action<TTrig> OnEnter;
        public event Action<TTrig>? OnExit;
        public event Func<TTrig, Task> OnEnterAsync;
        public event Func<TTrig, Task>? OnExitAsync;
        public TName Name { get; internal set; }
        public bool Ignoring { get; internal set; }

        internal State()
        {
        }

        public IReadOnlyDictionary<TTrig, State<TTrig, TName>> Transitions => _transitions;

        internal void OnBuild()
        {
            IsAsyncEnter = OnEnterAsync != null;
            IsAsyncExit = OnExitAsync != null;
        }

        internal void Activate(TTrig value)
        {
            OnEnter?.Invoke(value);
        }

        internal Task ActivateAsync(TTrig value)
        {
            if (OnEnterAsync == null)
            {
                return Task.CompletedTask;
            }
            return OnEnterAsync?.Invoke(value);
        }

        internal void Exit(TTrig value)
        {
            OnExit?.Invoke(value);
        }

        internal Task ExitAsync(TTrig value)
        {
            if (OnExitAsync == null)
            {
                return Task.CompletedTask;
            }
            return OnExitAsync?.Invoke(value);
        }

        public bool TryAddTransition(TTrig triggerValue, State<TTrig, TName> targetState)
        {
            if (_transitions.ContainsKey(triggerValue))
            {
                return false;
            }
            _transitions.Add(triggerValue, targetState);
            return true;
        }

        public void AddTransition(TTrig triggerValue, State<TTrig, TName> targetState)
        {
            _transitions.Add(triggerValue, targetState);
        }

        public void AddAllTransition(State<TTrig, TName> targetState)
        {
            _allTransition = targetState;
        }

        public virtual State<TTrig, TName>? Next(TTrig trigValue)
        {
            _transitions.TryGetValue(trigValue, out var state);
            if (state == null)
            {
                if (_allTransition != null)
                {
                    return _allTransition;
                }

                if (Ignoring)
                {
                    return null;
                }
                throw new KeyNotFoundException(
                    $"Cannot find tranistion with key {trigValue} from current state {Name}");
            }

            return state;
        }
    }
}