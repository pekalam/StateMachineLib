using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#pragma warning disable 8714

namespace StateMachineLib
{
    public class LockingStateMachineBuilder<TTrig, TName> : StateMachineBuilderBase<TTrig, TName, LockingStateMachineBuilder<TTrig, TName>, LockingStateMachine<TTrig, TName>>
    {
        public LockingStateMachineBuilder()
        {
            _builder = this;
        }

        protected override LockingStateMachine<TTrig, TName> CreateStateMachine(State<TTrig, TName> start, List<State<TTrig, TName>> states, string? name)
        {
            return new LockingStateMachine<TTrig, TName>(start, states, name);
        }
    }

    public class StateMachineBuilder<TTrig, TName> : StateMachineBuilderBase<TTrig, TName, StateMachineBuilder<TTrig, TName>, StateMachine<TTrig, TName>>
    {
        public StateMachineBuilder()
        {
            _builder = this;
        }

        protected override StateMachine<TTrig, TName> CreateStateMachine(State<TTrig, TName> start, List<State<TTrig, TName>> states, string? name)
        {
            return new StateMachine<TTrig, TName>(start, states, name);
        }
    }


    public abstract class StateMachineBuilderBase<TTrig, TName, TBuilder, TSm> where TBuilder : StateMachineBuilderBase<TTrig, TName, TBuilder, TSm>
    where TSm : StateMachine<TTrig, TName>
    {
        private StateBuilder? _currentStateBuilder;
        protected TSm? _stateMachine;
        protected TBuilder _builder { private get; set; }

        protected readonly Dictionary<TName, State<TTrig, TName>> _createdStates =
            new Dictionary<TName, State<TTrig, TName>>();

        private readonly List<InterruptStateBuildArgs<TTrig, TName>> _intStateArgs =
            new List<InterruptStateBuildArgs<TTrig, TName>>();

        private readonly List<ResetInterruptStateBuildArgs<TTrig, TName>> _resetIntStateArgs =
            new List<ResetInterruptStateBuildArgs<TTrig, TName>>();

        private readonly List<HoldingGlobStateBuildArgs<TTrig, TName>> _holdingIntStateBuildArgs = 
            new List<HoldingGlobStateBuildArgs<TTrig, TName>>();

        private readonly Dictionary<(TName stateName, TTrig triggerValue), TName> _transitions =
            new Dictionary<(TName, TTrig), TName>();

        private readonly Dictionary<TName, TName> _allTransitions = new Dictionary<TName, TName>();

        public StateBuilder CreateState(TName name)
        {
            _currentStateBuilder = new StateBuilder(_builder, name);
            return _currentStateBuilder;
        }

        protected void InternalBuild()
        {
            foreach (var (tuple, targetStateName) in _transitions)
            {
                var state = _createdStates[tuple.stateName];
                state.AddTransition(tuple.triggerValue, _createdStates[targetStateName]);
            }

            foreach (var (stateName, targetAll) in _allTransitions)
            {
                var state = _createdStates[stateName];
                var targetState = _createdStates[targetAll];
                state.AddAllTransition(targetState);
            }
            
            foreach (var intStateArgs in _intStateArgs)
            {
                var intState = new InterruptState<TTrig, TName>(_stateMachine, intStateArgs);
                foreach (var state in _createdStates.Values)
                {
                    if (!state.TryAddTransition(intStateArgs.Trigger, intState))
                    {
                        Console.WriteLine($"State {state.Name} transition with trigger {intStateArgs.Trigger} took precedence over interrupt state transition");
                    }
                }
            }

            foreach (var arg in _resetIntStateArgs)
            {
                var resetIntState = new ResetInterruptState<TTrig, TName>(_stateMachine, arg,
                    _createdStates
                        .Where(kv => kv.Key != null && kv.Key.Equals(arg.ResetStateName))
                        .Select(kv => kv.Value).First());
                foreach (var state in _createdStates.Values)
                {
                    if (!state.TryAddTransition(arg.TriggerValue, resetIntState))
                    {
                        Console.WriteLine($"State {state.Name} transition with trigger {arg.TriggerValue} took precedence over reset interrupt state transition");
                    }
                }
            }


            foreach (var arg in _holdingIntStateBuildArgs)
            {
                var holdingIntState = new HoldingGlobState<TTrig, TName>(_stateMachine, arg);
                foreach (var state in _createdStates.Values)
                {
                    if (!state.TryAddTransition(arg.Trigger, holdingIntState))
                    {
                        Console.WriteLine($"State {state.Name} transition with trigger {arg.Trigger} took precedence over holding glob state transition");
                    }
                }
            }
        }

        protected abstract TSm CreateStateMachine(State<TTrig, TName> start,
            List<State<TTrig, TName>> states, string? name);

        public TSm Build(TName startStateName, string? name = null)
        {
            _stateMachine = CreateStateMachine(_createdStates[startStateName], _createdStates.Values.ToList(), name);
            InternalBuild();
            return _stateMachine;
        }

        public TBuilder InterruptState(TTrig triggerValue, Action<StateEnterArgs<TTrig, TName>> action,
            TName stateName)
        {
            _intStateArgs.Add(new InterruptStateBuildArgs<TTrig, TName>()
            {
                StateAction = action,
                StateName = stateName,
                Trigger = triggerValue
            });
            return _builder;
        }


        public TBuilder ResetInterruptState(TTrig triggerValue, Action<StateEnterArgs<TTrig, TName>> action,
            TName stateName, TName resetState)
        {
            _resetIntStateArgs.Add(new ResetInterruptStateBuildArgs<TTrig, TName>()
            {
                StateAction = action,
                StateName = stateName,
                TriggerValue = triggerValue,
                ResetStateName = resetState,
            });
            return _builder;
        }


        public TBuilder AsyncInterruptState(TTrig triggerValue, Func<StateEnterArgs<TTrig, TName>, Task> action,
            TName stateName)
        {
            _intStateArgs.Add(new InterruptStateBuildArgs<TTrig, TName>()
            {
                AsyncStateAction = action,
                StateName = stateName,
                Trigger = triggerValue
            });
            return _builder;
        }


        public TBuilder AsyncResetInterruptState(TTrig triggerValue, Func<StateEnterArgs<TTrig, TName>, Task> action,
            TName stateName, TName resetState)
        {
            _resetIntStateArgs.Add(new ResetInterruptStateBuildArgs<TTrig, TName>()
            {
                AsyncStateAction = action,
                StateName = stateName,
                TriggerValue = triggerValue,
                ResetStateName = resetState,
            });
            return _builder;
        }


        public TBuilder HoldingGlobState(TTrig triggerValue, Action<StateEnterArgs<TTrig, TName>> action,
            TName stateName, TTrig returnTrigger)
        {
            _holdingIntStateBuildArgs.Add(new HoldingGlobStateBuildArgs<TTrig, TName>()
            {
                StateAction = action,
                StateName = stateName,
                Trigger = triggerValue,
                ReturnTrigger = returnTrigger,
            });
            return _builder;
        }


        public TBuilder AsyncHoldingGlobState(TTrig triggerValue, Func<StateEnterArgs<TTrig, TName>, Task> action,
            TName stateName, TTrig returnTrigger)
        {
            _holdingIntStateBuildArgs.Add(new HoldingGlobStateBuildArgs<TTrig, TName>()
            {
                AsyncStateAction = action,
                StateName = stateName,
                Trigger = triggerValue,
                ReturnTrigger = returnTrigger,
            });
            return _builder;
        }

        public class StateBuilder
        {
            private readonly TBuilder _parentBuilder;
            private readonly State<TTrig, TName> _state;

            public StateBuilder(TBuilder parentBuilder, TName name)
            {
                _state = new State<TTrig, TName>(name);
                _parentBuilder = parentBuilder;
                _parentBuilder._createdStates.Add(name, _state);
            }

            public StateBuilder Enter(Action<StateEnterArgs<TTrig, TName>> action)
            {
                _state.OnEnter += action;
                return this;
            }

            public StateBuilder EnterAsync(Func<StateEnterArgs<TTrig, TName>, Task> action)
            {
                _state.OnEnterAsync += action;
                return this;
            }

            public StateBuilder Exit(Action<StateExitArgs<TTrig, TName>> action)
            {
                _state.OnExit += action;
                return this;
            }

            public StateBuilder ExitAsync(Func<StateExitArgs<TTrig, TName>, Task> action)
            {
                _state.OnExitAsync += action;
                return this;
            }

            public StateBuilder CanExit(Func<TTrig, State<TTrig, TName>?, bool> action)
            {
                _state.CanExit = action;
                return this;
            }

            public StateBuilder Ignoring(bool ignoring = true)
            {
                _state.Ignoring = ignoring;
                return this;
            }

            public StateBuilder Transition(TTrig triggerValue, TName stateName)
            {
                _parentBuilder._transitions.Add((_state.Name, triggerValue),
                    stateName);
                return this;
            }

            public StateBuilder AllTransition(TName targetStateName)
            {
                _parentBuilder._allTransitions.Add(_state.Name, targetStateName);
                return this;
            }


            public StateBuilder Loop(TTrig triggerValue) => Transition(triggerValue, _state.Name);

            public TBuilder End()
            {
                _state.OnBuild();
                return _parentBuilder;
            }
        }
    }
}