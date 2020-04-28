using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace StateMachineLib
{
    public class StateMachineVis<TTrig, TName> : IDisposable 
    {
        private readonly NamedPipeServerStream _pipeServer;
        private readonly DotGenerator<TTrig, TName> _dotGenerator;
        private StreamWriter _sw;
        private readonly object _stateChangeLck = new object();
        private long _msgCounter;
        private Mutex _visMutex;
        private StateMachine<TTrig, TName> _stateMachine;
        private readonly string _pipeName;
        private readonly bool _loggingEnabled;

        public StateMachineVis(StateMachine<TTrig, TName> stateMachine, string pipeName = "graphViz", bool loggingEnabled = true)
        {
            _loggingEnabled = loggingEnabled;
            _stateMachine = stateMachine;
            _pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.Out);
            _dotGenerator = new DotGenerator<TTrig, TName>(stateMachine.StateMachineInfo);
            _visMutex = new Mutex(true, pipeName);
            _pipeName = pipeName;
        }

        public void Start(string vizAppPath = null, string clientArgs = null)
        {
            var task = _pipeServer.WaitForConnectionAsync();
            if (!string.IsNullOrEmpty(vizAppPath))
            {
                if (!string.IsNullOrEmpty(clientArgs))
                {
                    Process.Start(vizAppPath, clientArgs);
                }
                else
                {
                    Process.Start(vizAppPath);
                }
            }
            task.GetAwaiter().GetResult();
            _stateMachine.OnStateChanged += OnStateChanged;
            _sw = new StreamWriter(_pipeServer);
            _sw.AutoFlush = true;
        }

        private void OnStateChanged(State<TTrig, TName> prev, State<TTrig, TName> current)
        {
            if (prev != current)
            {
                if (_loggingEnabled)
                {
                    Console.WriteLine($"[{_pipeName}] {prev.Name} -> {current.Name}");
                }
                var msgCounter = Interlocked.Add(ref _msgCounter, 1);
                Task.Run(() =>
                {
                    var msg = _dotGenerator.Generate(true, current);
                    lock (_stateChangeLck)
                    {
                        if (msgCounter < Interlocked.Read(ref _msgCounter))
                        {
                            Console.WriteLine($"Dropping state change msg {current.Name}");
                            return;
                        }

                        try
                        {
                            _sw.WriteLine($"start {msgCounter}");
                            _sw.WriteLine(msg);
                            _sw.WriteLine("end");
                            _sw.Flush();
                        }
                        catch (Exception)
                        {

                        }
                    }
                });
            }
        }


        public void Dispose()
        {
            _pipeServer?.Dispose();
            _visMutex?.Dispose();
        }
    }
}