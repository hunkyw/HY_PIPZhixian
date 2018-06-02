using System;
using System.Drawing;
using System.Windows.Forms;

namespace HY_PIP
{
    public partial class MainFrame : Form
    {
        public static MainForm mainForm;
        public static NewProcessForm newProcessForm;
        public static ManualForm manualForm;
        public static PersonForm personForm;

        public static int SCREEN_HEIGHT;
        public static int SCREEN_WIDTH;
        public static int CLIENT_HEIGHT;
        public static bool SCREEN_AUTO_SCROLL = true;

        public class GlobalMouseHandler : IMessageFilter
        {
            private const int WM_LBUTTONDOWN = 0x201;

            public bool PreFilterMessage(ref Message m)
            {
                /*
                if (m.Msg == WM_LBUTTONDOWN)
                {
                    // Do stuffs
                    if(MainForm.Permition == MainForm.PERMITION.None)
                    {
                        MainForm.Permition = MainForm.PERMITION.Validate;// 进入验证权限阶段
                        PermitForm permitForm = new PermitForm();
                        permitForm.Left = 800;
                        permitForm.Top = 300;
                        permitForm.ShowDialog();
                        if(MainForm.Permition == MainForm.PERMITION.Validate)
                        {
                            MainForm.Permition = MainForm.PERMITION.None;// 如果验证权限后，权限依然还是没有升级，那么就依然拦截鼠标消息。
                        }
                        //return true;// 拦截鼠标消息
                    }
                }*/
                return false;// 不拦截
            }
        }

        public MainFrame()
        {
            InitializeComponent();
        }

        private void MainFrame_Load(object sender, EventArgs e)
        {
            SCREEN_WIDTH = this.Width = Screen.PrimaryScreen.Bounds.Width;
            SCREEN_HEIGHT = this.Height = Screen.PrimaryScreen.Bounds.Height;
            if (SCREEN_HEIGHT == 1080 && SCREEN_WIDTH == 1920)
            {
                SCREEN_AUTO_SCROLL = false;
            }
            else
            {
                SCREEN_AUTO_SCROLL = true;
            }
            this.Left = 00;
            this.Top = 000;
            this.BackColor = MainForm.sysBackColor;

            pictureBoxTabTitle.Top = 0;
            pictureBoxTabTitle.Left = 0;

            panel1.Left = 0;
            panel1.Top = pictureBoxTabTitle.Height;
            panel1.Width = this.Width;
            CLIENT_HEIGHT = panel1.Height = this.Height - pictureBoxTabTitle.Height;

            // 主页面
            mainForm = new MainForm();
            mainForm.FormBorderStyle = FormBorderStyle.None; // 无边框
            mainForm.Dock = DockStyle.Fill;  // 让尺寸全覆盖
            mainForm.TopLevel = false;
            mainForm.Show();
            panel1.Controls.Add(mainForm);

            // 新增工艺页面
            newProcessForm = new NewProcessForm();
            newProcessForm.FormBorderStyle = FormBorderStyle.None; // 无边框
            newProcessForm.Dock = DockStyle.Fill;  // 让尺寸全覆盖
            newProcessForm.TopLevel = false;
            newProcessForm.Width = MainFrame.SCREEN_WIDTH;
            panel1.Controls.Add(newProcessForm);

            // 手动控制页面
            manualForm = new ManualForm();
            manualForm.FormBorderStyle = FormBorderStyle.None; // 无边框
            manualForm.Dock = DockStyle.Fill;  // 让尺寸全覆盖
            manualForm.TopLevel = false;
            manualForm.Width = MainFrame.SCREEN_WIDTH;
            panel1.Controls.Add(manualForm);

            //
            personForm = new PersonForm();

            //
            pictureBox1.Image = Image.FromFile(@"Resources\tab0_down.png");
            pictureBox2.Image = Image.FromFile(@"Resources\tab1_up.png");
            pictureBox3.Image = Image.FromFile(@"Resources\tab2_up.png");

            // 拦截鼠标点击消息
            //GlobalMouseHandler globalClick = new GlobalMouseHandler();
            //Application.AddMessageFilter(globalClick);
        }

        private void pictureBoxTabTitle_Click(object sender, EventArgs e)
        {
            if (!mainForm.CheckPermition()) return;

            CloseForm closeForm = new CloseForm();
            closeForm.ShowDialog(this);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (!mainForm.CheckPermition()) return;

            pictureBox1.Image = Image.FromFile(@"Resources\tab0_down.png");
            pictureBox2.Image = Image.FromFile(@"Resources\tab1_up.png");
            pictureBox3.Image = Image.FromFile(@"Resources\tab2_up.png");

            mainForm.BringToFront();
            mainForm.Show();
            //newProcessForm.Hide();
            //manualForm.Hide();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            if (!mainForm.CheckPermition()) return;
            if (MainForm.Permition < MainForm.PERMITION.Manager) return;// 管理者才允许进入

            pictureBox1.Image = Image.FromFile(@"Resources\tab0_up.png");
            pictureBox2.Image = Image.FromFile(@"Resources\tab1_down.png");
            pictureBox3.Image = Image.FromFile(@"Resources\tab2_up.png");

            newProcessForm.BringToFront();
            newProcessForm.Show();
            //mainForm.Hide();
            //manualForm.Hide();
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            if (!mainForm.CheckPermition()) return;

            pictureBox1.Image = Image.FromFile(@"Resources\tab0_up.png");
            pictureBox2.Image = Image.FromFile(@"Resources\tab1_up.png");
            pictureBox3.Image = Image.FromFile(@"Resources\tab2_down.png");

            MainFrame.newProcessForm.SendToBack();//
            //manualForm.Dock = DockStyle.Fill;
            manualForm.BringToFront();
            manualForm.Show();
            //mainForm.Hide();
            //newProcessForm.Hide();
        }
    }
}