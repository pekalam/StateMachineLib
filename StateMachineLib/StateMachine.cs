using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StateMachineLib
{
    public class StateMachine<TTrig, TName> 
    {
        public State<TTrig, TName> CurrentState { get; private set; }
        public State<TTrig, TName>? PreviousState { get; private set; }

        public event Action<State<TTrig, TName>?, State<TTrig, TName>> OnStateChanged;

        internal StateMachine(State<TTrig, TName> startState, List<State<TTrig, TName>> allStates)
        {
            CurrentState = startState;
            PreviousState = null;
            OnStateChanged?.Invoke(PreviousState, CurrentState);
            StateMachineInfo = new StateMachineInfo<TTrig, TName>(allStates, startState);
        }

        public StateMachineInfo<TTrig, TName> StateMachineInfo { get; }

        public State<TTrig, TName>? Next(TTrig triggerValue)
        {
            State<TTrig, TName>? nextState = CurrentState.Next(triggerValue);

            if (nextState == null)
            {
                return null;
            }

            if (CurrentState.IsAsyncExit)
            {
                CurrentState.ExitAsync(triggerValue).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            else
            {
                CurrentState.Exit(triggerValue);
            }

            PreviousState = CurrentState;
            CurrentState = nextState;
            OnStateChanged?.Invoke(PreviousState, CurrentState);


            if (CurrentState.IsAsyncEnter)
            {
                CurrentState.ActivateAsync(triggerValue).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            else
            {
                CurrentState.Activate(triggerValue);
            }


            return CurrentState;
        }

        public async Task<State<TTrig, TName>?> NextAsync(TTrig triggerValue)
        {
            State<TTrig, TName>? nextState = CurrentState.Next(triggerValue);

            if (nextState == null)
            {
                return null;
            }

            if (CurrentState.IsAsyncExit)
            {
                await CurrentState.ExitAsync(triggerValue);
            }
            else
            {
                CurrentState.Exit(triggerValue);
            }

            PreviousState = CurrentState;
            CurrentState = nextState;
            OnStateChanged?.Invoke(PreviousState, CurrentState);
            if (CurrentState.IsAsyncEnter)
            {
                await CurrentState.ActivateAsync(triggerValue);
            }
            else
            {
                CurrentState.Activate(triggerValue);
            }
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