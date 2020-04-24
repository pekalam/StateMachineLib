﻿using System;
using System.Threading.Tasks;

namespace StateMachineLib
{
    class InterruptStateBuildArgs<TTrig, TName> 
    {
        public TName StateName { get; set; }
        public TTrig TriggerValue { get; set; }
        public Action<TTrig>? StateAction { get; set; }
        public Func<TTrig, Task>? AsyncStateAction { get; set; }
    }
}