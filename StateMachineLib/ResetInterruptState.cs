using System;
using System.Threading.Tasks;

namespace StateMachineLib
{
    class ResetInterruptState<TTrig, TName> : State<TTrig, TName>
    {
        private readonly StateMachine<TTrig, TName> _stateMachine;
        private readonly Action<StateEnterArgs<TTrig, TName>>? _action;
        private readonly Func<StateEnterArgs<TTrig, TName>, Task>? _asyncAction;
        private readonly State<TTrig, TName> _resetState;

        public ResetInterruptState(StateMachine<TTrig, TName> stateMachine, ResetInterruptStateBuildArgs<TTrig, TName> args, State<TTrig, TName> resetState) : base(args.StateName)
        {
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

        private async Task OnAsyncInterruptStateEnter(StateEnterArgs<TTrig, TName> arg)
        {
            if (_asyncAction == null) throw new NullReferenceException("Null state async action");
            
            await _asyncAction.Invoke(arg);
            _stateMachine.Restore(_resetState);
        }

        private void OnInterruptStateEnter(StateEnterArgs<TTrig, TName> triggerValue)
        {
            if (_action == null) throw new NullReferenceException("Null state action");

            _action.Invoke(triggerValue);
            _stateMachine.Restore(_resetState);
        }
    }
}