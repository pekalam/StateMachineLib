using System;

namespace StateMachineLib
{
    class InterruptStateBuildArgs<TTrig, TName> 
    {
        public TName StateName { get; set; }
        public Action<TTrig> StateAction { get; set; }
        public TTrig TriggerValue { get; set; }
    }


    class ResetInterruptStateBuildArgs<TTrig, TName>
    {
        public TName StateName { get; set; }
        public Action<TTrig> StateAction { get; set; }
        public TTrig TriggerValue { get; set; }
        public TName ResetStateName { get; set; }
    }
}