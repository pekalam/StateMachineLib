using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace StateMachineLib
{
    public interface ILogger
    {
        void Log(string msg) => Console.WriteLine(msg);
    }

    internal class ConsoleLogger : ILogger
    {
        void Log(string msg) => Console.WriteLine(msg);
    }

    public class StateMachineLogging<TTrig, TName>
    {
        private readonly ILogger _logger;
        private readonly StateMachine<TTrig, TName> _stateMachine;

        public StateMachineLogging(StateMachine<TTrig, TName> stateMachine, ILogger? logger = null)
        {
            _stateMachine = stateMachine;
            _stateMachine.OnStateSet += StateMachineOnStateSet;
            _stateMachine.OnStateChanged += StateMachineOnStateChanged;
            _logger = logger ?? new ConsoleLogger();
        }

        private void StateMachineOnStateChanged(State<TTrig, TName>? prev, State<TTrig, TName> curr, TTrig trig)
        {
            _logger.Log($"[{_stateMachine.StateMachineInfo.Name ?? "no_name"}] trigger: {trig?.ToString()} transition: {(prev != null && prev.Name != null ? prev.Name.ToString() : "(empty)")} -> {curr.Name}");
        }

        private void StateMachineOnStateSet(State<TTrig, TName>? prev, State<TTrig, TName> curr)
        {
            _logger.Log($"[{_stateMachine.StateMachineInfo.Name ?? "no_name"}] {(prev != null && prev.Name != null ? prev.Name.ToString() : "(empty)")} set to {curr.Name}");
        }
    }

    public class StateMachineVis<TTrig, TName> : IDisposable 
    {
        private readonly NamedPipeServerStream _pipeServer;
        private readonly DotGenerator<TTrig, TName> _dotGenerator;
        private StreamWriter? _sw;
        private readonly object _stateChangeLck = new object();
        private long _msgCounter;
        private readonly Mutex _visMutex;
        private readonly StateMachine<TTrig, TName> _stateMachine;
        private readonly string _name;
        private bool _vizAppStarted;
        private List<Task> _activeTasks = new List<Task>();

        public StateMachineVis(StateMachine<TTrig, TName> stateMachine, string name = "graphViz")
        {
            _stateMachine = stateMachine;
            _pipeServer = new NamedPipeServerStream(name, PipeDirection.Out);
            _dotGenerator = new DotGenerator<TTrig, TName>(stateMachine.StateMachineInfo);
            _visMutex = new Mutex(true, name);
            _name = name;
        }

        public void Start(string vizAppPath = null, string clientArgs = null)
        {
            if (_vizAppStarted)
            {
                throw new Exception("StateMachineVis already started");
            }
            
            _stateMachine.OnStateChanged += StateMachineOnOnStateChanged;
            _stateMachine.OnStateSet += SendChange;
            if (!string.IsNullOrEmpty(vizAppPath))
            {
                var task = _pipeServer.WaitForConnectionAsync();

                if (!string.IsNullOrEmpty(clientArgs))
                {
                    Process.Start(vizAppPath, clientArgs);
                }
                else
                {
                    Process.Start(vizAppPath);
                }

                task.GetAwaiter().GetResult();
                _sw = new StreamWriter(_pipeServer);
                _sw.AutoFlush = true;
                _vizAppStarted = true;
            }
        }

        private void StateMachineOnOnStateChanged(State<TTrig, TName>? prev, State<TTrig, TName> curr, TTrig arg3) => SendChange(prev,curr);

        private void SendChange(State<TTrig, TName>? prev, State<TTrig, TName> current)
        {
            if (prev != current)
            {
                var msgCounter = Interlocked.Add(ref _msgCounter, 1);

                if (_vizAppStarted)
                {
                    var task = new Task(() =>
                    {
                        var msg = _dotGenerator.Generate(true, current);
                        lock (_stateChangeLck)
                        {
                            if (msgCounter < Interlocked.Read(ref _msgCounter))
                            {
                                Console.WriteLine($"Dropping state change msg {current.Name}");
                                return;
                            }
                            if (_sw == null)
                            {
                                return;
                            }
                            
                            try
                            {
                                _sw.WriteLine($"start {msgCounter}");
                                _sw.WriteLine(msg);
                                _sw.WriteLine("end");
                                _sw.Flush();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Vis error: {ex}");
                            }
                        }
                    });
                    _activeTasks.Add(task);
                    task.Start();
                    task.ContinueWith(t => _activeTasks.Remove(t));
                }
            }
           
        }

        public void Stop()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            _stateMachine.OnStateChanged -= StateMachineOnOnStateChanged;
            _stateMachine.OnStateSet -= SendChange;
            try
            {
                Task.WaitAll(_activeTasks.ToArray(), cts.Token);
            }
            catch (OperationCanceledException e)
            {
                Console.WriteLine(e);
            }
            _activeTasks.Clear();
            _pipeServer.Close();
            _vizAppStarted = false;
        }

        public void Dispose()
        {
            _pipeServer?.Dispose();
            _visMutex?.Dispose();
        }
    }
}