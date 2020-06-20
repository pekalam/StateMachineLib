using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable 8714

namespace StateMachineLib
{
    public class StateEnterArgs<TTrig, TName>
    {
        public TTrig Trigger { get; set; }
        public StateMachineContext<TTrig, TName> Context { get; set; }
        public State<TTrig, TName> CurrentState { get; set; }
        public State<TTrig, TName> NextState { get; set; }
    }

    public class StateExitArgs<TTrig, TName>
    {
        public TTrig Trigger { get; set; }
        public State<TTrig, TName> CurrentState { get; set; }
        public State<TTrig, TName> NextState { get; set; }
    }

    public class State<TTrig, TName>
    {
        private State<TTrig, TName>? _allTransition;

        protected readonly Dictionary<TTrig, State<TTrig, TName>> _transitions =
            new Dictionary<TTrig, State<TTrig, TName>>();

        public bool IsAsyncEnter { get; private set; }
        public bool IsAsyncExit { get; private set; }
        public event Action<StateEnterArgs<TTrig, TName>>? OnEnter;
        public event Action<StateExitArgs<TTrig, TName>>? OnExit;
        public Func<TTrig, State<TTrig, TName>?, bool>? CanExit;
        public Func<StateEnterArgs<TTrig, TName>, Task>? OnEnterAsync;
        public Func<StateExitArgs<TTrig, TName>, Task>? OnExitAsync;
        public TName Name { get; internal set; }
        public bool Ignoring { get; internal set; }

        internal State(TName name)
        {
            Name = name;
        }

        public IReadOnlyDictionary<TTrig, State<TTrig, TName>> Transitions => _transitions;

        internal void OnBuild()
        {
            IsAsyncEnter = OnEnterAsync != null;
            IsAsyncExit = OnExitAsync != null;
        }

        internal void Activate(StateEnterArgs<TTrig, TName> value)
        {
            OnEnter?.Invoke(value);
        }

        internal Task ActivateAsync(StateEnterArgs<TTrig, TName> value)
        {
            return OnEnterAsync == null ? Task.CompletedTask : OnEnterAsync.Invoke(value);
        }


        internal void Exit(StateExitArgs<TTrig, TName> value)
        {
            OnExit?.Invoke(value);
        }

        internal Task ExitAsync(StateExitArgs<TTrig, TName> value)
        {
            return OnExitAsync == null ? Task.CompletedTask : OnExitAsync.Invoke(value);
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

            if (CanExit != null && !CanExit.Invoke(trigValue, state))
            {
                return null;
            }


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
                    $"Cannot find transition with key {trigValue} from current state {Name}");
            }

            return state;
        }
    }
}