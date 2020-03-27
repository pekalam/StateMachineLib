using System;

namespace StateMachineLib
{
    class InterruptState<TTrig, TName> : State<TTrig, TName> 
    {
        private readonly StateMachine<TTrig, TName> _stateMachine;
        private readonly Action<TTrig> _action;

        public InterruptState(StateMachine<TTrig, TName> stateMachine, Action<TTrig> action, TName name)
        {
            OnEnter += OnInterruptStateEnter;
            _stateMachine = stateMachine;
            _action = action;
            Name = name;
        }

        private void OnInterruptStateEnter(TTrig triggerValue)
        {
            _action.Invoke(triggerValue);
            _stateMachine.Restore(_stateMachine.PreviousState);
        }
    }


    class ResetInterruptState<TTrig, TName> : State<TTrig, TName>
    {
        private readonly StateMachine<TTrig, TName> _stateMachine;
        private readonly Action<TTrig> _action;
        private readonly State<TTrig, TName> _resetState;

        public ResetInterruptState(StateMachine<TTrig, TName> stateMachine, ResetInterruptStateBuildArgs<TTrig, TName> args, State<TTrig, TName> resetState)
        {
            OnEnter += OnInterruptStateEnter;
            _stateMachine = stateMachine;
            _action = args.StateAction;
            Name = args.StateName;
            _resetState = resetState;
        }

        private void OnInterruptStateEnter(TTrig triggerValue)
        {
            _action.Invoke(triggerValue);
            _stateMachine.Restore(_resetState);
        }
    }
}