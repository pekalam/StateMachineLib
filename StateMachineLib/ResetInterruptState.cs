using System;
using System.Threading.Tasks;

namespace StateMachineLib
{
    class ResetInterruptState<TTrig, TName> : State<TTrig, TName>
    {
        private readonly StateMachine<TTrig, TName> _stateMachine;
        private readonly Action<TTrig> _action;
        private readonly Func<TTrig, Task> _asyncAction;
        private readonly State<TTrig, TName> _resetState;

        public ResetInterruptState(StateMachine<TTrig, TName> stateMachine, ResetInterruptStateBuildArgs<TTrig, TName> args, State<TTrig, TName> resetState)
        {
            Name = args.StateName;
            _stateMachine = stateMachine;
            _resetState = resetState;

            if (args.StateAction != null)
            {
                OnEnter += OnInterruptStateEnter;
                _action = args.StateAction;
            }
            else
            {
                OnEnterAsync += OnAsyncInterruptStateEnter;
                _asyncAction = args.AsyncStateAction;
            }

            

            OnBuild();
        }

        private async Task OnAsyncInterruptStateEnter(TTrig arg)
        {
            await _asyncAction.Invoke(arg);
            _stateMachine.Restore(_resetState);
        }

        private void OnInterruptStateEnter(TTrig triggerValue)
        {
            _action.Invoke(triggerValue);
            _stateMachine.Restore(_resetState);
        }
    }
}