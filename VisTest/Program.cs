using System;
using System.Threading;
using System.Threading.Tasks;
using StateMachineLib;

namespace VisTest
{
    class Program
    {
        enum State { S1,S2,S3,S4 }
        enum Trig { T1,T2 }

        static void Main(string[] args)
        {
            var t1 = 0;

            var sm = new StateMachineBuilder<Trig, State>()
                .CreateState(State.S1)
                .Ignoring()
                .Transition(Trig.T1, State.S2)
                .End()
                .CreateState(State.S2)
                .Transition(Trig.T1, State.S3)
                .End()
                .CreateState(State.S3)
                .Transition(Trig.T2, State.S1)
                .End()
                .Build(State.S1);

            var vis = new StateMachineVis<Trig, State>(sm, pipeName: "graphVizTest");
            vis.Start("StateMachineLibVis.exe", clientArgs: "-c graphVizTest");

            var cts = new CancellationTokenSource();
            Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    if (t1 < 2)
                    {
                        sm.Next(Trig.T1);
                        t1++;
                    }
                    else
                    {
                        t1 = 0;
                        sm.Next(Trig.T2);
                    }

                    await Task.Delay(750, cts.Token);
                }

            });


            Console.ReadKey();
            cts.Cancel();
        }
    }
}
