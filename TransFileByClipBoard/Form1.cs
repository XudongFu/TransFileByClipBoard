using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Core;

namespace TransFileByClipBoard
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        string path = string.Empty;
        Timer time;
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            var res = open.ShowDialog();
            if (res == DialogResult.OK)
            {
                label3.Text = open.FileName;
                path = open.FileName;
            }
        }


        ~Form1()
        {
            if (co != null)
            {
                if (co.ReadThread != null && co.ReadThread.IsAlive)
                {
                    co.ReadThread.Abort();
                }
                if (co.CopyThread != null && co.CopyThread.IsAlive)
                {
                    co.CopyThread.Abort();
                }
            }
        }


        int SumSecond = 0;

        private void button2_Click(object sender, EventArgs e)
        {
            int speed = Core.Core2.Interval;
            int size = PartFileInfo.BufferSize;
            label2.Text =Math.Round((1000.0/speed)*size/1024.0,2).ToString()+"KB/S";

            var file= new FileInfo(path);

            if (file.Exists)
            {
                SumSecond =(int) (file.Length / ((1000.0 / speed) * size));
                 time = new Timer();
                time.Interval = 1000;
                time.Tick += Time_Tick;
                time.Enabled = true;
                var stream= file.OpenRead();
                co = new Core.Core2(file.Name ,stream);               
                co.CopyProgressGo += Co_CopyProgressGo;
                co.CopyToBoard();
            }
        }
        Core.Core2 co;
        private void Time_Tick(object sender, EventArgs e)
        {
            if (SumSecond <= 0)
            {
                time.Enabled = false;
            }
            label4.Text= string.Format("剩余时间{0}", TimeSpan.FromSeconds(SumSecond--).ToString());
        }

        private void Co_CopyProgressGo(object sender, Core2.ProgressInfo e)
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new Action<int, int>((x, y) =>
                {
                    progressBar1.Maximum = y;
                    progressBar1.Value = x;
                }), (int)e.value, (int)e.max);
            }
            else
            {
                progressBar1.Value = (int)e.value;
                progressBar1.Maximum = (int)e.max;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
