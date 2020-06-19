using System;
using System.Threading.Tasks;
#pragma warning disable 8618

namespace StateMachineLib
{
    class HoldingGlobStateBuildArgs<TTrig, TName>
    {
        public TName StateName { get; set; }
        public TTrig Trigger { get; set; }
        public Action<TTrig>? StateAction { get; set; }
        public Func<TTrig, Task>? AsyncStateAction { get; set; }
        public TTrig ReturnTrigger { get; set; }
    }
}