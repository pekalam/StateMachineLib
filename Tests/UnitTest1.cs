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
                .Enter(t => TestOnEntry(t.Trigger, stateName));
            if (testExit)
            {
                b.Exit(t => TestOnExit(t.Trigger, stateName));
            }

            return b;
        }

        public static StateMachineBuilder<TTrig, TName> InterruptTestState<TTrig, TName>(this StateMachineBuilder<TTrig, TName> builder, TTrig triggerValue,
            TName stateName)
        {
            var b = builder.InterruptState(triggerValue, t => TestOnEntry(t.Trigger, stateName), stateName);

            return b;
        }

        public static StateMachineBuilder<TTrig, TName>.StateBuilder CreateTestAsyncState<TTrig, TName>(
            this StateMachineBuilder<TTrig, TName> builder,
            TName stateName, bool testExit = false)
        {
            var b = builder.CreateState(stateName)
                .EnterAsync((t) =>
                {
                    TestOnEntry(t.Trigger, stateName);
                    return Task.CompletedTask;
                });

            if (testExit)
            {
                b.ExitAsync(t =>
                {
                    TestOnExit(t.Trigger, stateName);
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
        S0,S1,S2, RESETINTER1, S3, RESETINTER2, INTER1, INTER2,S4,S5,S6
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


        [Fact]
        public async Task TransitionPrecedenceTest()
        {
            var sm = new StateMachineBuilder<TrigType, States>()
                .CreateTestState(States.S1)
                    .Transition(TrigType.T1, States.S2)
                .End()

                .InterruptTestState(TrigType.T1, States.INTER1)
                
                .CreateTestState(States.S2)
                    .Loop(TrigType.T2)
                    .Transition(TrigType.T3, States.S1)
                .End()

                .Build(States.S1);



            sm.Next(TrigType.T1);
            sm.Next(TrigType.T1);

            sm.Next(TrigType.T3);
            await sm.NextAsync(TrigType.T1);

            TestStateMachineContext<TrigType, States>.GetStateTimesCalled(States.INTER1).ShouldBe(1);
            TestStateMachineContext<TrigType, States>.Result.ShouldBe("_T1-S2_T1-INTER1_T3-S1_T1-S2");
        }

        [Fact]
        public void Exit_not_called_if_transition_not_found()
        {
            var sm = new StateMachineBuilder<TrigType, States>()
                .CreateTestState(States.S1, true)
                .Transition(TrigType.T1, States.S2)
                .End()
                .CreateTestState(States.S2, true)
                .Transition(TrigType.T1, States.S1)
                .End()

                .Build(States.S1);

            Assert.ThrowsAny<Exception>(() => sm.Next(TrigType.T3).ShouldBeNull());
            TestStateMachineContext<TrigType, States>.Result.ShouldBe("");
        }

        [Fact]
        public async Task IgnoringState()
        {
            var sm = new StateMachineBuilder<TrigType, States>()
                .CreateTestState(States.S1, true)
                .Transition(TrigType.T1, States.S2)
                .Ignoring()
                .End()

                .CreateTestState(States.S2, true)
                .Ignoring()
                .End()

                .InterruptTestState(TrigType.T2, States.INTER1)

                .Build(States.S1);


            sm.Next(TrigType.T3);
            await sm.NextAsync(TrigType.T3);
            TestStateMachineContext<TrigType, States>.Result.ShouldBe("");

            sm.Next(TrigType.T1);

            TestStateMachineContext<TrigType, States>.Result.ShouldBe("_ET1-ES1_T1-S2");

            sm.Next(TrigType.T2);

            TestStateMachineContext<TrigType, States>.Result.ShouldBe("_ET1-ES1_T1-S2_ET2-ES2_T2-INTER1");
        }

        [Fact]
        public void Enter_exit_null_does_not_throw()
        {
            var sm = new StateMachineBuilder<TrigType, States>()
                .CreateState(States.S3)
                .Transition(TrigType.T1, States.S2)
                .End()
                .CreateState(States.S2)
                .Transition(TrigType.T1, States.S3)
                .End()
                .Build(States.S3);

            sm.NextAsync(TrigType.T1);
            sm.Next(TrigType.T1);
        }

        [Fact]
        public void Next_called_on_context_enters_next_state_after_current_finished()
        {
            bool s1Called = false;
            bool s2Called = false;
            bool s3Called = false;
            bool s4Called = false;


            var sm = new StateMachineBuilder<TrigType, States>()
                .CreateState(States.S0)
                    .Transition(TrigType.T1, States.S1)
                .End()
                    .CreateState(States.S1)
                        .Enter(args =>
                    {
                        args.Context.Next(TrigType.T1);
                        s1Called = true;
                    })
                    .Transition(TrigType.T1, States.S2)
                .End()
                    .CreateState(States.S2)
                        .Enter(args =>
                    {
                        args.Context.NextAsync(TrigType.T2);
                        s2Called = true;
                    })
                    .Transition(TrigType.T2, States.S3)
                .End()
                .CreateState(States.S3)
                    .EnterAsync(args => { s3Called = true; return Task.CompletedTask; })
                    .Transition(TrigType.T2, States.S4)
                .End()
                .CreateState(States.S4)
                    .Enter(args => s4Called=true)
                .End()
                .Build(States.S0);


            var current = sm.Next(TrigType.T1);

            current.Name.ShouldBe(States.S3);
            s1Called.ShouldBeTrue();
            s2Called.ShouldBeTrue();
            s3Called.ShouldBeTrue();
            s4Called.ShouldBeFalse();


            //context clear test
            current = sm.Next(TrigType.T2);
            current.Name.ShouldBe(States.S4);
            s4Called.ShouldBeTrue();
        }




        [Fact]
        public async Task Next_async_called_on_context_enters_next_state_after_current_finished()
        {
            bool s1Called = false;
            bool s2Called = false;
            bool s3Called = false;
            bool s4Called = false;


            var sm = new StateMachineBuilder<TrigType, States>()
                .CreateState(States.S0)
                    .Transition(TrigType.T1, States.S1)
                .End()
                .CreateState(States.S1)
                    .EnterAsync(args =>
                    {
                        args.Context.Next(TrigType.T1);
                        s1Called = true;
                        return Task.CompletedTask;
                    })
                    .Transition(TrigType.T1, States.S2)
                .End()
                .CreateState(States.S2)
                    .Enter(args =>
                    {
                        args.Context.NextAsync(TrigType.T2);
                        s2Called = true;
                    })
                    .Transition(TrigType.T2, States.S3)
                .End()
                .CreateState(States.S3)
                    .EnterAsync(args => { s3Called = true; return Task.CompletedTask; })
                    .Transition(TrigType.T2, States.S4)
                .End()
                .CreateState(States.S4)
                    .EnterAsync(args =>
                {
                    s4Called = true;
                    return Task.CompletedTask;
                })
                .End()
                .Build(States.S0);


            var current =  await sm.NextAsync(TrigType.T1);

            current?.Name.ShouldBe(States.S3);
            s1Called.ShouldBeTrue();
            s2Called.ShouldBeTrue();
            s3Called.ShouldBeTrue();
            s4Called.ShouldBeFalse();


            //context clear test
            current = await sm.NextAsync(TrigType.T2);
            current?.Name.ShouldBe(States.S4);
            s4Called.ShouldBeTrue();
        }


        [Fact]
        public void Loop_does_not_call_exit()
        {
            var sm = new StateMachineBuilder<TrigType, States>()
                .CreateTestState(States.S0)
                .Loop(TrigType.T1)
                .End()
                .Build(States.S0);


            sm.Next(TrigType.T1);
            sm.Next(TrigType.T1);
            sm.Next(TrigType.T1);

            TestStateMachineContext<TrigType, States>.Result.ShouldBe("_T1-S0_T1-S0_T1-S0");
        }
    }
}