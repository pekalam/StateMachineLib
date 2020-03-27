using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmGraph
{
    public class GraphImageGenerator
    {
        private static string GetDotExecutablePath()
        {
            var enviromentPath = System.Environment.GetEnvironmentVariable("PATH");

            var paths = enviromentPath.Split(';');
            var exePath = paths
                .Select(x => Path.Combine(x, "dot.exe"))
                .FirstOrDefault(x => File.Exists(x));
            if (exePath == null)
            {
                throw new ArgumentException();
            }

            return exePath;
        }

        public static Image GenerateGraph(string dot)
        {
            var exePath = GetDotExecutablePath();
            ProcessStartInfo startInfo = new ProcessStartInfo(exePath, "-Tpng -Gdpi=120");
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            var process = Process.Start(startInfo);

            process.StandardInput.Write(dot);
            process.StandardInput.Close();

            var bmp = Image.FromStream(process.StandardOutput.BaseStream);

            process.WaitForExit();

            return bmp;
        }
    }
}