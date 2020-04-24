using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StateMachineLib
{
    public class State<TTrig, TName>
    {
        private State<TTrig, TName> _allTransition = null;
        private readonly Dictionary<TTrig, State<TTrig, TName>> _transitions = new Dictionary<TTrig, State<TTrig, TName>>();
        public bool IsAsyncState { get; private set; }
        public event Action<TTrig> OnEnter;
        public event Action<TTrig> OnExit;
        public event Func<TTrig, Task> OnEnterAsync;
        public event Func<TTrig, Task> OnExitAsync;
        public TName Name { get; internal set; }
        public bool Ignoring { get; internal set; }

        internal State()
        {
        }

        public IReadOnlyDictionary<TTrig, State<TTrig, TName>> Transitions => _transitions;

        internal void OnBuild()
        {
            IsAsyncState = OnEnterAsync != null;
        }

        public void Activate(TTrig value)
        {
            OnEnter?.Invoke(value);
        }

        public Task ActivateAsync(TTrig value)
        {
            return OnEnterAsync?.Invoke(value);
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
            if (OnExitAsync != null)
            {
                OnExitAsync.Invoke(trigValue).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            else
            {
                OnExit?.Invoke(trigValue);
            }
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

        public virtual async Task<State<TTrig, TName>?> NextAsync(TTrig trigValue)
        {
            if (OnExitAsync != null)
            {
                await OnExitAsync.Invoke(trigValue);
            }
            else
            {
                OnExit?.Invoke(trigValue);
            }
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