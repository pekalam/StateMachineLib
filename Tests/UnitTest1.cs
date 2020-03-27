using System;
using System.Collections.Generic;
using Shouldly;
using StateMachineLib;
using Xunit;

namespace Tests
{
    static class TestStateMachineContext<TTrig, TName>
    {
        //format _TRIG1_STATE1_TRIG2_STATE2
        private static string _result = "";
        private static Dictionary<TName, int> _stateTimesCalled = new Dictionary<TName, int>();

        public static void Reset()
        {
            _stateTimesCalled.Clear();
            _result = "";
        }

        public static void UpdateResult(TTrig trig, TName stateName) 
        {
            _result += $"_{trig}-{stateName}";
        }

        public static void UpdateStateCalled(TName stateName)
        {
            if (_stateTimesCalled.ContainsKey(stateName))
            {
                _stateTimesCalled[stateName]++;
            }
            else
            {
                _stateTimesCalled.Add(stateName, 1);
            }
        }

        public static int GetStateTimesCalled(State<TTrig, TName> state) 
        {
            return GetStateTimesCalled(state.Name);
        }

        public static int GetStateTimesCalled(TName stateName)
        {
            if (_stateTimesCalled.ContainsKey(stateName))
            {
                return _stateTimesCalled[stateName];
            }
            else
            {
                return 0;
            }
        }

        public static string Result => _result;
    }

    static class StateMachineBuilderTestExtensions
    {
        private static void TestOnEntry<TTrig, TName>(TTrig trig, TName stateName) 
        {
            TestStateMachineContext<TTrig, TName>.UpdateResult(trig, stateName);
            TestStateMachineContext<TTrig, TName>.UpdateStateCalled(stateName);
        }

        public static StateMachineBuilder<TTrig, TName>.StateBuilder CreateTestState<TTrig, TName>(
            this StateMachineBuilder<TTrig, TName> builder,
            TName stateName) 
        {
            return builder.CreateState(stateName)
                .Enter((t) => TestOnEntry(t, stateName));
        }
    }

    enum TrigType
    {
        T1,
        T2
    }

    enum States
    {
        S1,S2, RESETINTER1
    }

    public class UnitTest1
    {


        public UnitTest1()
        {
            TestStateMachineContext<TrigType, States>.Reset();
        }


        [Fact]
        public void Test1()
        {
            var sm = new StateMachineBuilder<TrigType, States>()
                .CreateTestState(States.S1)
                .Transition(TrigType.T1, States.S2)
                .End()
                .CreateTestState(States.S2)
                .Loop(TrigType.T2)
                .Transition(TrigType.T1, States.S1)
                .End()
                .Build(States.S1);

            

            TestStateMachineContext<TrigType, States>.GetStateTimesCalled(States.S1).ShouldBe(0);
            TestStateMachineContext<TrigType, States>.Result.ShouldBe("");
        }


        [Fact]
        public void Test2()
        {
            var sm = new StateMachineBuilder<TrigType, States>()
                .CreateTestState(States.S1)
                    .AllTransition(States.S2)
                .End()
                .CreateTestState(States.S2)
                    .Loop(TrigType.T2)
                    .Transition(TrigType.T1, States.S1)
                .End()
                .Build(States.S1);

            sm.Next(TrigType.T1);
            sm.Next(TrigType.T1);
            sm.Next(TrigType.T1);

            TestStateMachineContext<TrigType, States>.GetStateTimesCalled(States.S1).ShouldBe(1);
            TestStateMachineContext<TrigType, States>.GetStateTimesCalled(States.S2).ShouldBe(2);
            TestStateMachineContext<TrigType, States>.Result.ShouldBe("_T1-S2_T1-S1_T1-S2");
            sm.CurrentState.Name.ShouldBe(States.S2);
        }


        [Fact]
        public void Test3()
        {
            var sm = new StateMachineBuilder<TrigType, States>()
                .CreateTestState(States.S1)
                    .Transition(TrigType.T2,States.S2)
                .End()
                .CreateTestState(States.S2)
                    .Loop(TrigType.T2)
                .End()
                .ResetInterruptState(TrigType.T1, t => {}, States.RESETINTER1, States.S1)
                .Build(States.S1);

            sm.Next(TrigType.T2);
            sm.Next(TrigType.T1);

            sm.CurrentState.Name.ShouldBe(States.S1);

            sm.Next(TrigType.T2);

            TestStateMachineContext<TrigType, States>.GetStateTimesCalled(States.S1).ShouldBe(0);
            TestStateMachineContext<TrigType, States>.GetStateTimesCalled(States.S2).ShouldBe(2);
            TestStateMachineContext<TrigType, States>.Result.ShouldBe("_T2-S2_T2-S2");
            sm.CurrentState.Name.ShouldBe(States.S2);
        }
    }
}