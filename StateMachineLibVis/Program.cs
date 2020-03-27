using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommandLine;

namespace SmGraph
{
    public class Options
    {
        [Option('c', "connect", Required = false, HelpText = "Server pipe name")]
        public string PipeName { get; set; }

        [Option('l', "left", Required = false, HelpText = "Window left position", Default = 0)]
        public int Left { get; set; }

    }

    static class Program
    {
        private static void StartApp(Options opt)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var pipeClient = new PipeClient();

            var form = new Form1(pipeClient, opt.Left);

            if (!string.IsNullOrEmpty(opt.PipeName))
            {
                form.Connect(opt.PipeName);
            }

            Application.Run(form);
        }

        [STAThread]
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o => StartApp(o));




        }
    }
}
