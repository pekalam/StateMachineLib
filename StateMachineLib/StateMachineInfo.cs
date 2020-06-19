using System.Collections.Generic;

namespace StateMachineLib
{
    public class StateMachineInfo<TTrig, TName> 
    {
        public IReadOnlyList<State<TTrig, TName>> States { get; }
        public State<TTrig, TName> StartState { get; }
        
        public string? Name { get; }
        
        public StateMachineInfo(IReadOnlyList<State<TTrig, TName>> states, State<TTrig, TName> startState, string? name)
        {
            States = states;
            StartState = startState;
            Name = name;
        }
    }
}