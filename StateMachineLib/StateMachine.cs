using System;
using System.Collections.Generic;

namespace StateMachineLib
{
    public class StateMachine<TTrig, TName> 
    {
        public State<TTrig, TName> CurrentState { get; private set; }
        public State<TTrig, TName> PreviousState { get; private set; }

        public event Action<State<TTrig, TName>, State<TTrig, TName>> OnStateChanged;

        internal StateMachine(State<TTrig, TName> startState, List<State<TTrig, TName>> allStates)
        {
            CurrentState = startState;
            PreviousState = null;
            OnStateChanged?.Invoke(PreviousState, CurrentState);
            StateMachineInfo = new StateMachineInfo<TTrig, TName>(allStates, startState);
        }

        public StateMachineInfo<TTrig, TName> StateMachineInfo { get; }

        public State<TTrig, TName> Next(TTrig triggerValue)
        {
            var nextState = CurrentState.Next(triggerValue);
            if (nextState == null)
            {
                return null;
            }
            PreviousState = CurrentState;
            CurrentState = nextState;
            OnStateChanged?.Invoke(PreviousState, CurrentState);
            CurrentState.Activate(triggerValue);
            return CurrentState;
        }

        public void Restore(State<TTrig, TName> state)
        {
            PreviousState = CurrentState;
            CurrentState = state;
            OnStateChanged?.Invoke(PreviousState, CurrentState);
        }
    }
}