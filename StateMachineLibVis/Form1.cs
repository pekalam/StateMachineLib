using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmGraph
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOZORDER = 0x0004;
        private const int SWP_SHOWWINDOW = 0x0040;

        private PipeClient _pipeClient;
        private bool _started;
        private int _winOffsetX;

        public Form1(PipeClient pipeClient, int winOffsetX)
        {
            _winOffsetX = winOffsetX;
            _pipeClient = pipeClient;
            _pipeClient.OnGraphGenerated = SetImage;
            InitializeComponent();
        }

        public void SetImage(Image img)
        {
            pictureBox1.Invoke((MethodInvoker) delegate
            {
                pictureBox1.Image = img;
            });
        }


        private void startStopBtn_Click(object sender, EventArgs e)
        {
            _pipeClient.GenerateGraph = !_pipeClient.GenerateGraph;
            
            if (_pipeClient.GenerateGraph)
            {
                startStopBtn.Text = "Stop";
            }
            else
            {
                startStopBtn.Text = "Start";
            }
        }

        private void connectBtn_Click(object sender, EventArgs e)
        {
            if (!_started)
            {
                Connect(pipeNameTextBox.Text);
            }
            else
            {
                Disconnect();
            }
            _started = !_started;
        }


        public void Connect(string pipeName)
        {
            _pipeClient.PipeName = pipeName;
            _pipeClient.StartPipeClient();
            connectedToLabel.Text = pipeName;
            connectBtn.Text = "Disconnect";
            _started = true;
        }

        public void Disconnect()
        {
            _pipeClient.Stop();
            connectedToLabel.Text = "none";
            connectBtn.Text = "Connect";
            _started = false;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Rectangle area = Screen.AllScreens[0].WorkingArea;

            var x = SetWindowPos(this.Handle, IntPtr.Zero, area.Left + _winOffsetX,
                area.Top, area.Width / 2, area.Height, SWP_NOZORDER);
        }
    }
}