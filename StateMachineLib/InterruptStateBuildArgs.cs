using System;
using System.Threading.Tasks;
#pragma warning disable 8618

namespace StateMachineLib
{
    class InterruptStateBuildArgs<TTrig, TName> 
    {
        public TName StateName { get; set; }
        public TTrig Trigger { get; set; }
        public Action<StateEnterArgs<TTrig, TName>>? StateAction { get; set; }
        public Func<StateEnterArgs<TTrig, TName>, Task>? AsyncStateAction { get; set; }
    }
}