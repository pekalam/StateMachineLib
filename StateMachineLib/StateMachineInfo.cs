using System.Collections.Generic;

namespace StateMachineLib
{
    public class StateMachineInfo<TTrig, TName> 
    {
        public IReadOnlyList<State<TTrig, TName>> States { get; }
        public State<TTrig, TName> StartState { get; }

        public StateMachineInfo(IReadOnlyList<State<TTrig, TName>> states, State<TTrig, TName> startState)
        {
            States = states;
            StartState = startState;
        }
    }
}