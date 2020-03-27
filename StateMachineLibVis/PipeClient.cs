using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmGraph
{
    public class PipeClient
    {
        private NamedPipeClientStream _startedPipeClient;
        private StreamReader _sr;
        private long _lastMsgNumber = 0;
        private object _delLock = new object();

        public PipeClient()
        {
        }

        public string PipeName { get; set; }

        public bool GenerateGraph { get; set; } = true;

        public Action<Image> OnGraphGenerated { get; set; }

        private long ParseStartHeader(string msg)
        {
            if (msg.IndexOf("start") != 0)
            {
                throw new ArgumentException();
            }

            var msgNum = Convert.ToInt64(msg.Substring(6));
            return msgNum;
        }

        public void StartPipeClient()
        {
            Task.Factory.StartNew(async () =>
            {
                while (Mutex.TryOpenExisting(PipeName, out var x))
                {
                    await Task.Delay(2000);
                    x.Dispose();
                }
                Application.Exit();
            });


            Task.Factory.StartNew(() =>
            {

                using (NamedPipeClientStream pipeClient =
                    new NamedPipeClientStream(".", PipeName, PipeDirection.In))
                {
                    _startedPipeClient = pipeClient;
                    // Connect to the pipe or wait until the pipe is available.
                    Console.Write("Attempting to connect to pipe...");
                    pipeClient.Connect();
                    Console.WriteLine("Connected to pipe.");
                    Console.WriteLine("There are currently {0} pipe server instances open.",
                        pipeClient.NumberOfServerInstances);
                    using (StreamReader sr = new StreamReader(pipeClient))
                    {
                        _sr = sr;
                        // Display the read text to the console
                        string dot = "";
                        string temp = "";
                        while ((temp = sr.ReadLine()) != "end_con")
                        {
                            if (temp == null)
                            {
                                break;
                            }

                            var msgNum = ParseStartHeader(temp);
                            while ((temp = sr.ReadLine()) != "end")
                            {
                                if (temp == null)
                                {
                                    break;
                                }
                                dot += temp;
                                Console.WriteLine("Received from server: {0}", temp);
                               
                            }

                            if (msgNum < _lastMsgNumber)
                            {
                                Console.WriteLine($"Dropping msg {msgNum}");
                                dot = "";
                                continue;
                            }

                            Interlocked.Exchange(ref _lastMsgNumber, msgNum);

                            if (GenerateGraph)
                            {
                                var dotGraph = dot;
                                Task.Run(() =>
                                {
                                    if (msgNum < Interlocked.Read(ref _lastMsgNumber))
                                    {
                                        Console.WriteLine($"Dropping msg {msgNum}");
                                        return;
                                    }
                                    var img = GraphImageGenerator.GenerateGraph(dotGraph);
                                    lock (_delLock)
                                    {
                                        if (msgNum < Interlocked.Read(ref _lastMsgNumber))
                                        {
                                            Console.WriteLine($"Dropping msg {msgNum}");
                                            return;
                                        }
                                        OnGraphGenerated?.Invoke(img);
                                    }
                                });

                            }

                            dot = "";
                        }
          
                    }
                }



            }, TaskCreationOptions.LongRunning);

        }

        public void Stop()
        {
            _startedPipeClient?.Dispose();
            _sr?.Dispose();
        }

    }
}
