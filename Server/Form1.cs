using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace Server
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private bool running = false;

        private void Btn_StartServer_Click(object sender, EventArgs e)
        {
            if (!running)
            {
                Thread thread = new Thread(new ParameterizedThreadStart(StartServer));
                thread.Start();
                running = true;
                timer1.Interval = 500;
                timer1.Enabled = true;
            }
        }

        private void StartServer(object? obj)
        {
            HttpServer.HttpServer server = new HttpServer.HttpServer();
            server.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            string s = Logger.GetLog();
            if(s != string.Empty)
            {
                textBox1.AppendText(s + Environment.NewLine);
            }
        }

    }
}
