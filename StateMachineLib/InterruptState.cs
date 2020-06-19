using System;
using System.Threading.Tasks;

namespace StateMachineLib
{
    class InterruptState<TTrig, TName> : State<TTrig, TName> 
    {
        private readonly StateMachine<TTrig, TName> _stateMachine;
        private readonly Action<TTrig>? _action;
        private readonly Func<TTrig, Task>? _asyncAction;

        public InterruptState(StateMachine<TTrig, TName> stateMachine, InterruptStateBuildArgs<TTrig, TName> args) : base(args.StateName)
        {
            _stateMachine = stateMachine;

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
            if (_asyncAction == null) throw new NullReferenceException("Null state async action");
            if (_stateMachine.PreviousState == null) throw new Exception("Previous state cannot be null");

            await _asyncAction.Invoke(arg);
            _stateMachine.Restore(_stateMachine.PreviousState);
        }

        private void OnInterruptStateEnter(TTrig triggerValue)
        {
            if (_action == null) throw new NullReferenceException("Null state async action");
            if (_stateMachine.PreviousState == null) throw new Exception("Previous state cannot be null");
            
            _action.Invoke(triggerValue);
            _stateMachine.Restore(_stateMachine.PreviousState);
        }
    }
}