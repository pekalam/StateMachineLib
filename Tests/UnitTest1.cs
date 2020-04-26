using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using StateMachineLib;
using Xunit;

namespace Tests
{
    static class TestStateMachineContext<TTrig, TName>
    {
        //format _TRIG1_STATE1(_ETRIG2_ESTATE1)_TRIG2_STATE2
        private static string _result = "";
        private static Dictionary<TName, int> _stateTimesCalled = new Dictionary<TName, int>();
        private static Dictionary<TName, int> _stateExitTimesCalled = new Dictionary<TName, int>();


        public static void Reset()
        {
            _stateTimesCalled.Clear();
            _stateExitTimesCalled.Clear();
            _result = "";
        }

        public static void UpdateResult(TTrig trig, TName stateName) 
        {
            _result += $"_{trig}-{stateName}";
        }

        public static void UpdateExitResult(TTrig trig, TName stateName)
        {
            _result += $"_E{trig}-E{stateName}";
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

        public static void UpdateStateExitCalled(TName stateName)
        {
            if (_stateExitTimesCalled.ContainsKey(stateName))
            {
                _stateExitTimesCalled[stateName]++;
            }
            else
            {
                _stateExitTimesCalled.Add(stateName, 1);
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
            return 0;
        }

        public static int GetStateExitTimesCalled(TName stateName)
        {
            if (_stateExitTimesCalled.ContainsKey(stateName))
            {
                return _stateExitTimesCalled[stateName];
            }
            return 0;
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

        private static void TestOnExit<TTrig, TName>(TTrig trig, TName stateName)
        {
            TestStateMachineContext<TTrig, TName>.UpdateExitResult(trig, stateName);
            TestStateMachineContext<TTrig, TName>.UpdateStateExitCalled(stateName);
        }

        public static StateMachineBuilder<TTrig, TName>.StateBuilder CreateTestState<TTrig, TName>(
            this StateMachineBuilder<TTrig, TName> builder,
            TName stateName, bool testExit = false) 
        {
            var b = builder.CreateState(stateName)
                .Enter((t) => TestOnEntry(t, stateName));
            if (testExit)
            {
                b.Exit(trig => TestOnExit(trig, stateName));
            }

            return b;
        }

        public static StateMachineBuilder<TTrig, TName>.StateBuilder CreateTestAsyncState<TTrig, TName>(
            this StateMachineBuilder<TTrig, TName> builder,
            TName stateName, bool testExit = false)
        {
            var b = builder.CreateState(stateName)
                .EnterAsync((t) =>
                {
                    TestOnEntry(t, stateName);
                    return Task.CompletedTask;
                });

            if (testExit)
            {
                b.ExitAsync(trig =>
                {
                    TestOnExit(trig, stateName);
                    return Task.CompletedTask;
                });
            }

            return b;
        }
    }

    enum TrigType
    {
        T1,
        T2,
        T3,
        RET
    }

    enum States
    {
        S1,S2, RESETINTER1, S3, RESETINTER2, INTER1, INTER2
    }

    public class UnitTest1
    {


        public UnitTest1()
        {
            TestStateMachineContext<TrigType, States>.Reset();
        }


        [Fact]
        public void StartState()
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
        public void Transitions_test()
        {
            var sm = new StateMachineBuilder<TrigType, States>()
                .CreateTestState(States.S1, true)
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
            TestStateMachineContext<TrigType, States>.GetStateExitTimesCalled(States.S1).ShouldBe(2);
            TestStateMachineContext<TrigType, States>.GetStateTimesCalled(States.S2).ShouldBe(2);
            TestStateMachineContext<TrigType, States>.Result.ShouldBe("_ET1-ES1_T1-S2_T1-S1_ET1-ES1_T1-S2");
            sm.CurrentState.Name.ShouldBe(States.S2);
        }


        [Fact]
        public async Task ResetInterruptState()
        {
            var sm = new StateMachineBuilder<TrigType, States>()
                .CreateTestState(States.S1)
                    .Transition(TrigType.T2,States.S2)
                .End()
                .CreateTestState(States.S2)
                    .Loop(TrigType.T2)
                .End()
                .ResetInterruptState(TrigType.T1, t => {}, States.RESETINTER1, States.S1)
                .AsyncResetInterruptState(TrigType.T3, _ => Task.CompletedTask, States.RESETINTER2, States.S2)
                .Build(States.S1);

            sm.Next(TrigType.T2);
            sm.Next(TrigType.T1);

            sm.CurrentState.Name.ShouldBe(States.S1);

            await sm.NextAsync(TrigType.T3);

            sm.CurrentState.Name.ShouldBe(States.S2);

            sm.Next(TrigType.T2);

            TestStateMachineContext<TrigType, States>.GetStateTimesCalled(States.S1).ShouldBe(0);
            TestStateMachineContext<TrigType, States>.GetStateTimesCalled(States.S2).ShouldBe(2);
            TestStateMachineContext<TrigType, States>.Result.ShouldBe("_T2-S2_T2-S2");
            sm.CurrentState.Name.ShouldBe(States.S2);
        }

        [Fact]
        public async Task InterruptState()
        {
            int i1Called = 0;
            int i2Called = 0;

            var sm = new StateMachineBuilder<TrigType, States>()
                .CreateTestState(States.S1)
                .End()
                .AsyncInterruptState(TrigType.T2, _ =>
                {
                    i2Called++;
                    return Task.CompletedTask;
                }, States.INTER2)
                .InterruptState(TrigType.T1, _ => i1Called++, States.INTER1)
                .Build(States.S1);

            sm.Next(TrigType.T1);
            sm.Next(TrigType.T2);

            await sm.NextAsync(TrigType.T1);
            await sm.NextAsync(TrigType.T2);

            i1Called.ShouldBe(2);
            i2Called.ShouldBe(2);
            TestStateMachineContext<TrigType, States>.GetStateTimesCalled(States.S1).ShouldBe(0);
        }

        [Fact]
        public async Task HoldingGlobState()
        {
            int i1Called = 0;
            int i2Called = 0;

            var sm = new StateMachineBuilder<TrigType, States>()
                .CreateTestState(States.S1)
                .End()
                .AsyncHoldingGlobState(TrigType.T2, _ =>
                {
                    i2Called++;
                    return Task.CompletedTask;
                }, States.INTER2, TrigType.RET)
                .HoldingGlobState(TrigType.T1, _ => i1Called++, States.INTER1, TrigType.RET)
                .Build(States.S1);


            sm.Next(TrigType.T1);
            sm.CurrentState.Name.ShouldBe(States.INTER1);

            await sm.NextAsync(TrigType.T1);
            sm.CurrentState.Name.ShouldBe(States.INTER1);
            sm.Next(TrigType.T2);
            sm.CurrentState.Name.ShouldBe(States.INTER1);

            sm.Next(TrigType.RET);
            sm.CurrentState.Name.ShouldBe(States.S1);

            await sm.NextAsync(TrigType.T2);
            sm.CurrentState.Name.ShouldBe(States.INTER2);

            await sm.NextAsync(TrigType.T1);
            sm.CurrentState.Name.ShouldBe(States.INTER2);
            sm.Next(TrigType.T2);
            sm.CurrentState.Name.ShouldBe(States.INTER2);

            await sm.NextAsync(TrigType.RET);
            sm.CurrentState.Name.ShouldBe(States.S1);

            i1Called.ShouldBe(1);
            i2Called.ShouldBe(1);
            TestStateMachineContext<TrigType, States>.GetStateTimesCalled(States.S1).ShouldBe(2);
        }

        [Fact]
        public async Task AsynState()
        {
            var sm = new StateMachineBuilder<TrigType, States>()
                .CreateTestAsyncState(States.S1, true)
                    .Transition(TrigType.T2, States.S2)
                .End()
                .CreateTestAsyncState(States.S2)
                    .Transition(TrigType.T1, States.S1)
                    .Transition(TrigType.T3, States.S3)
                .End()
                .CreateTestState(States.S3)
                    .Transition(TrigType.T3, States.S1)
                    .Loop(TrigType.T1)
                .End()
                .Build(States.S1);

            sm.CurrentState.Name.ShouldBe(States.S1);

            sm.Next(TrigType.T2);
            await sm.NextAsync(TrigType.T1);
            await sm.NextAsync(TrigType.T2);
            sm.Next(TrigType.T3);
            await sm.NextAsync(TrigType.T1);
            await sm.NextAsync(TrigType.T3);

            TestStateMachineContext<TrigType, States>.GetStateTimesCalled(States.S1).ShouldBe(2);
            TestStateMachineContext<TrigType, States>.GetStateTimesCalled(States.S2).ShouldBe(2);
            TestStateMachineContext<TrigType, States>.GetStateTimesCalled(States.S3).ShouldBe(2);

            TestStateMachineContext<TrigType, States>.Result.ShouldBe("_ET2-ES1_T2-S2_T1-S1_ET2-ES1_T2-S2_T3-S3_T1-S3_T3-S1");
        }
    }
}