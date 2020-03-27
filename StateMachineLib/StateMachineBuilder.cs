using System;
using System.Collections.Generic;
using System.Linq;

namespace StateMachineLib
{
    public class StateMachineBuilder<TTrig, TName>
    {
        private StateMachine<TTrig, TName> _stateMachine;
        private StateBuilder _currentStateBuilder;

        private readonly Dictionary<TName, State<TTrig, TName>> _createdStates =
            new Dictionary<TName, State<TTrig, TName>>();

        private readonly List<InterruptStateBuildArgs<TTrig, TName>> _intStateArgs =
            new List<InterruptStateBuildArgs<TTrig, TName>>();

        private readonly List<ResetInterruptStateBuildArgs<TTrig, TName>> _resetIntStateArgs =
            new List<ResetInterruptStateBuildArgs<TTrig, TName>>();


        private readonly Dictionary<(TName stateName, TTrig triggerValue), TName> _transitions =
            new Dictionary<(TName, TTrig), TName>();

        private readonly Dictionary<TName, TName> _allTransitions = new Dictionary<TName, TName>();

        public StateBuilder CreateState(TName name)
        {
            _currentStateBuilder = new StateBuilder(this, name);
            return _currentStateBuilder;
        }

        public StateMachine<TTrig, TName> Build(TName startStateName)
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

            _stateMachine =
                new StateMachine<TTrig, TName>(_createdStates[startStateName], _createdStates.Values.ToList());


            foreach (var intStateArgs in _intStateArgs)
            {
                var intState = new InterruptState<TTrig, TName>(_stateMachine, intStateArgs.StateAction,
                    intStateArgs.StateName);
                foreach (var state in _createdStates.Values)
                {
                    state.AddTransition(intStateArgs.TriggerValue, intState);
                }
            }

            foreach (var arg in _resetIntStateArgs)
            {
                var resetIntState = new ResetInterruptState<TTrig, TName>(_stateMachine, arg,
                    _createdStates.Where(kv => kv.Key.Equals(arg.ResetStateName)).Select(kv => kv.Value).First());
                foreach (var state in _createdStates.Values)
                {
                    state.AddTransition(arg.TriggerValue, resetIntState);
                }
            }

            return _stateMachine;
        }

        public StateMachineBuilder<TTrig, TName> InterruptState(TTrig triggerValue, Action<TTrig> action,
            TName stateName)
        {
            _intStateArgs.Add(new InterruptStateBuildArgs<TTrig, TName>()
            {
                StateAction = action,
                StateName = stateName,
                TriggerValue = triggerValue
            });
            return this;
        }


        public StateMachineBuilder<TTrig, TName> ResetInterruptState(TTrig triggerValue, Action<TTrig> action,
            TName stateName, TName resetState)
        {
            _resetIntStateArgs.Add(new ResetInterruptStateBuildArgs<TTrig, TName>()
            {
                StateAction = action,
                StateName = stateName,
                TriggerValue = triggerValue,
                ResetStateName = resetState,
            });
            return this;
        }

        public class StateBuilder
        {
            private readonly StateMachineBuilder<TTrig, TName> _parentBuilder;
            private readonly State<TTrig, TName> _state = new State<TTrig, TName>();

            public StateBuilder(StateMachineBuilder<TTrig, TName> parentBuilder, TName name)
            {
                _parentBuilder = parentBuilder;
                _state.Name = name;
                _parentBuilder._createdStates.Add(name, _state);
            }

            public StateBuilder Enter(Action<TTrig> action)
            {
                _state.OnEnter += action;
                return this;
            }

            public StateBuilder Exit(Action<TTrig> action)
            {
                _state.OnExit += action;
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

            public StateMachineBuilder<TTrig, TName> End()
            {
                return _parentBuilder;
            }
        }
    }
}