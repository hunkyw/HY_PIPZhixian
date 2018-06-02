using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HY_PIP
{
    public partial class CloseForm : Form
    {
        public CloseForm()
        {
            InitializeComponent();
        }

        int timerCntr = 10;
        private void timer1_Tick(object sender, EventArgs e)
        {
            timerCntr--;                        
            if(timerCntr<=0)
            {
                this.Close();
                System.Environment.Exit(0);
            }
            label2.Text = "系统即将在" + timerCntr.ToString() + "秒后关闭！";

            /*
             * 1.this.Close();   只是关闭当前窗口，若不是主窗体的话，是无法退出程序的，另外若有托管线程（非主线程），也无法干净地退出；
             * 2.Application.Exit();  强制所有消息中止，退出所有的窗体，但是若有托管线程（非主线程），也无法干净地退出；
             * 3.Application.ExitThread(); 强制中止调用线程上的所有消息，同样面临其它线程无法正确退出的问题；
             * 4.System.Environment.Exit(0);   这是最彻底的退出方式，不管什么线程都被强制退出，把程序结束的很干净。
             */
            /*
            System.Diagnostics.Process myProcess = new System.Diagnostics.Process(); 
            myProcess.StartInfo.FileName = "cmd.exe";
            myProcess.StartInfo.UseShellExecute = false; 
            myProcess.StartInfo.RedirectStandardInput = true; 
            myProcess.StartInfo.RedirectStandardOutput = true; 
            myProcess.StartInfo.RedirectStandardError = true; 
            myProcess.StartInfo.CreateNoWindow = true;
            myProcess.Start();
            myProcess.StandardInput.WriteLine("shutdown -s -f -t 0");
             * */
            //System.Diagnostics.Process.Start("shutdown", "-s -f -t 0");
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            this.Close();
            System.Environment.Exit(0);

            timerCntr = 0;// 关闭倒计时总时间
            label2.Text = "系统即将在" + timerCntr.ToString() + "秒后关闭！";    
            timer1.Start();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            this.Close();
        }

        private void CloseForm_Load(object sender, EventArgs e)
        {
            this.CenterToScreen();
        }

        private void CloseForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer1.Stop();
        }

    }
}
