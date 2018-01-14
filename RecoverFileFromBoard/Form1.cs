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

namespace RecoverFileFromBoard
{
    public partial class Form1 : Form
    {
        Core.Core2 co;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

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


        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog file = new FolderBrowserDialog();
            if (file.ShowDialog() == DialogResult.OK)
            {
                label1.Text = file.SelectedPath;
                co = new Core.Core2(file.SelectedPath);
                co.ReadProgressGo += Co_CopyProgressGo;
                co.WriteToFile();
            }
        }

        private void Co_CopyProgressGo(object sender, Core.Core2.ProgressInfo e)
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new Action<int, int>((m, n) =>
                {
                    progressBar1.Maximum = n;
                    progressBar1.Value = m;

                }), (int)e.value, (int)e.max);
            }
            else
            {
                progressBar1.Value =(int) e.value;
                progressBar1.Maximum = (int)e.max;
            }
        }



    }
}
