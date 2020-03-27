namespace StateMachineLib
{
    class DotGenerator<TTrig, TName>
    {
        private readonly StateMachineInfo<TTrig, TName> _stateMachineInfo;

        public DotGenerator(StateMachineInfo<TTrig, TName> stateMachineInfo)
        {
            _stateMachineInfo = stateMachineInfo;
        }

        private static string GetTransitionStr(State<TTrig, TName> source, TTrig trigger, State<TTrig, TName> target)
        {
            var label = $"[style=\"solid\", label=\"{trigger.ToString()}\"]";

            var transition = $"{source.Name} -> {target.Name} {label};\n";
            return transition;
        }

        public string Generate(bool ignoreInterruptStates, State<TTrig, TName> currentState = null)
        {
            string output = "digraph {\n";


            foreach (var state in _stateMachineInfo.States)
            {
                if (currentState != null && state == currentState)
                {
                    output +=
                        $"{currentState.Name}[label=\"{currentState.Name}\" style=filled color=\"red\" fontcolor=\"white\" ]\n";
                }
                else if (state.Ignoring)
                {
                    output +=
                        $"{state.Name}[label=\"{state.Name}\" style=filled color=\"blue\" fontcolor=\"white\" ]\n";
                }

                if (ignoreInterruptStates && (state.GetType() == typeof(InterruptState<TTrig, TName>) ||
                                              state.GetType() == typeof(ResetInterruptState<TTrig, TName>)))
                {
                    continue;
                }

                foreach (var transition in state.Transitions)
                {
                    if (ignoreInterruptStates &&
                        (transition.Value.GetType() == typeof(InterruptState<TTrig, TName>) || 
                         transition.Value.GetType() == typeof(ResetInterruptState<TTrig, TName>)))
                    {
                        continue;
                    }

                    output += GetTransitionStr(state, transition.Key, transition.Value);
                }
            }


            return output + "\n}\n";
        }
    }
}