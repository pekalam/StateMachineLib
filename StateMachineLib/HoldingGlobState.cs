using System;
using System.Threading.Tasks;
#pragma warning disable 8714

namespace StateMachineLib
{
    class HoldingGlobState<TTrig, TName> : State<TTrig, TName>
    {
        private readonly StateMachine<TTrig, TName> _stateMachine;
        private readonly Action<StateEnterArgs<TTrig, TName>>? _action;
        private readonly Func<StateEnterArgs<TTrig, TName>, Task>? _asyncAction;
        private readonly TTrig _returnTrig;

        public HoldingGlobState(StateMachine<TTrig, TName> stateMachine, HoldingGlobStateBuildArgs<TTrig, TName> args) : base(args.StateName)
        {
            _stateMachine = stateMachine;
            _returnTrig = args.ReturnTrigger;
            Ignoring = true;

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
            if (_stateMachine.PreviousState == null) throw new Exception("Previous state cannot be null");

            _transitions.Remove(_returnTrig);
            AddTransition(_returnTrig, _stateMachine.PreviousState);
            await _asyncAction.Invoke(arg).ConfigureAwait(false);
        }

        private void OnInterruptStateEnter(StateEnterArgs<TTrig, TName> triggerValue)
        {
            if (_action == null) throw new NullReferenceException("Null state action");
            if (_stateMachine.PreviousState == null) throw new Exception("Previous state cannot be null");
            
            _transitions.Remove(_returnTrig);
            AddTransition(_returnTrig, _stateMachine.PreviousState);
            _action.Invoke(triggerValue);
        }
    }
}