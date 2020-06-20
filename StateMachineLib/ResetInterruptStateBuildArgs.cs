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
        public Action<StateEnterArgs<TTrig, TName>>? StateAction { get; set; }
        public Func<StateEnterArgs<TTrig, TName>, Task>? AsyncStateAction { get; set; }
    }
}