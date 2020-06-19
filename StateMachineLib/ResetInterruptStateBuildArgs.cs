using System;
using System.Threading.Tasks;
#pragma warning disable 8618

namespace StateMachineLib
{
    class ResetInterruptStateBuildArgs<TTrig, TName>
    {
        public TName StateName { get; set; }
        public TTrig TriggerValue { get; set; }
        public TName ResetStateName { get; set; }
        public Action<TTrig>? StateAction { get; set; }
        public Func<TTrig, Task>? AsyncStateAction { get; set; }
    }
}