using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StateMachineLib
{
    public class StateMachineContext<TTrig, TName>
    {
        internal List<(TTrig trig, bool isAsync)> NextTriggers = new List<(TTrig trig, bool isAsync)>();

        internal StateMachineContext()
        {
            
        }

        public void Next(TTrig triggerValue) => NextTriggers.Add((triggerValue, false));
        public void NextAsync(TTrig triggerValue) => NextTriggers.Add((triggerValue, true));
    }

    public class StateMachine<TTrig, TName>
    {
        private readonly StateMachineContext<TTrig, TName> _context = new StateMachineContext<TTrig, TName>();
        public State<TTrig, TName> CurrentState { get; private set; }
        public State<TTrig, TName>? PreviousState { get; private set; }

        public event Action<State<TTrig, TName>?, State<TTrig, TName>, TTrig>? OnStateChanged;

        public event Action<State<TTrig, TName>?, State<TTrig, TName>>? OnStateSet;

        internal StateMachine(State<TTrig, TName> startState, List<State<TTrig, TName>> allStates, string? name)
        {
            CurrentState = startState;
            PreviousState = null;
            OnStateSet?.Invoke(PreviousState, CurrentState);
            StateMachineInfo = new StateMachineInfo<TTrig, TName>(allStates, startState, name);
        }

        public StateMachineInfo<TTrig, TName> StateMachineInfo { get; }

        public virtual State<TTrig, TName>? Next(TTrig triggerValue)
        {
            State<TTrig, TName>? nextState = CurrentState.Next(triggerValue);

            if (nextState == null)
            {
                return null;
            }


            var enterArgs = new StateEnterArgs<TTrig, TName>()
            {
                Trigger = triggerValue,
                CurrentState = CurrentState,
                NextState = nextState,
                Context = _context,
            };

            if (CurrentState != nextState)
            {
                var exitArgs = new StateExitArgs<TTrig, TName>()
                {
                    Trigger = triggerValue,
                    CurrentState = CurrentState,
                    NextState = nextState,
                };

                if (CurrentState.IsAsyncExit)
                {
                    CurrentState.ExitAsync(exitArgs).GetAwaiter().GetResult();
                }
                else
                {
                    CurrentState.Exit(exitArgs);
                }
            }


            PreviousState = CurrentState;
            CurrentState = nextState;
            OnStateChanged?.Invoke(PreviousState, CurrentState, triggerValue);


            if (CurrentState.IsAsyncEnter)
            {
                CurrentState.ActivateAsync(enterArgs).GetAwaiter().GetResult();
            }
            else
            {
                CurrentState.Activate(enterArgs);
            }

            State<TTrig, TName>? toReturn = CurrentState;
            for (int i = 0; i < _context.NextTriggers.Count; i++)
            {
                var next = _context.NextTriggers[i];
                _context.NextTriggers.RemoveAt(i);
                if (next.isAsync) toReturn = NextAsync(next.trig).GetAwaiter().GetResult();
                else toReturn = Next(next.trig);
            }

            return toReturn;
        }

        public virtual async Task<State<TTrig, TName>?> NextAsync(TTrig triggerValue)
        {
            State<TTrig, TName>? nextState = CurrentState.Next(triggerValue);

            if (nextState == null)
            {
                return null;
            }


            var enterArgs = new StateEnterArgs<TTrig, TName>()
            {
                Trigger = triggerValue,
                CurrentState = CurrentState,
                NextState = nextState,
                Context = _context,
            };

            if (CurrentState != nextState)
            {
                var exitArgs = new StateExitArgs<TTrig, TName>()
                {
                    Trigger = triggerValue,
                    CurrentState = CurrentState,
                    NextState = nextState,
                };

                if (CurrentState.IsAsyncExit)
                {
                    await CurrentState.ExitAsync(exitArgs).ConfigureAwait(false);
                }
                else
                {
                    CurrentState.Exit(exitArgs);
                }
            }


            PreviousState = CurrentState;
            CurrentState = nextState;
            OnStateChanged?.Invoke(PreviousState, CurrentState, triggerValue);
            if (CurrentState.IsAsyncEnter)
            {
                await CurrentState.ActivateAsync(enterArgs).ConfigureAwait(false);
            }
            else
            {
                CurrentState.Activate(enterArgs);
            }


            State<TTrig, TName>? toReturn = CurrentState;
            for (int i = 0; i < _context.NextTriggers.Count; i++)
            {
                var next = _context.NextTriggers[i];
                _context.NextTriggers.RemoveAt(i);
                if (next.isAsync) toReturn = await NextAsync(next.trig).ConfigureAwait(false);
                else toReturn = Next(next.trig);
            }


            return toReturn;
        }

        public void Restore(State<TTrig, TName> state)
        {
            PreviousState = CurrentState;
            CurrentState = state;
            OnStateSet?.Invoke(PreviousState, CurrentState);
        }
    }
}