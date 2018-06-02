using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace HY_PIP
{
    public partial class ManualForm : Form
    {
        public static LayoutDrawing layoutDrawing1;
        private Thread threadAB;
        public static Color systemBlue = Color.FromArgb(29, 150, 213);

        public ManualForm()
        {
            InitializeComponent();
        }

        private void ManualForm_Load(object sender, EventArgs e)
        {
            TransparentPanel.m_click = TransparentPanel_GeneralClick;// 通用单击事件

            // Layout 设置
            LayoutInit();

            this.BackColor = MainForm.sysBackColor;
            timer1.Start();
            timer2.Start();

            textBoxSpeedRelativeZ.Text = "1";
            textBoxSpeedRelativeX.Text = "1";
        }

        private void LayoutInit()
        {
            //float dpi = getLogPiex();
            //label1.Text = this.Width.ToString() + "," + this.Height.ToString() + "," + dpi.ToString();

            panelX.AutoScroll = MainFrame.SCREEN_AUTO_SCROLL;// 如果屏幕尺寸符合 1920*1080，就不用加滚动条。

            // 下面的布局总览图的尺寸设置
            panelX.Width = this.Width;
            panelX.Height = this.Height;

            //panel1.Left = 15;
            //panel1.Top = 5;
            // processPanelCtrl1.Left = 490;
            //processPanelCtrl1.Top = 5;
        }

        private void ManualForm_VisibleChanged(object sender, EventArgs e)
        {
        }

        /**
         * 通用单击事件
         *          绝对位置定位   手动操作点
         */
        private int pos_index;
        private int x_dst;
        private int z_dst;

        public void TransparentPanel_GeneralClick(object sender, EventArgs e)
        {
            if (GenericOp.AutoMode)
            {
                MessageBox.Show("当前为自动模式，请切换为手动模式进行操作！");
                return;
            }

            DialogResult dr = MessageBox.Show("确定执行：您真的确定执行绝对位置运动命令吗？", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.Cancel)
            {
                return;
            }
            // 0 - 12 代表 13 个炉子
            // 13,14,15代表前中后3个空位，用于上下料的地方。
            int tag = Convert.ToInt32(((TransparentPanel)sender).Tag);
            if (tag >= 1000)
            {
                pos_index = tag - 1000;

                x_dst = LayoutDrawing.loadPoints[pos_index].X;
                z_dst = layoutDrawing1.zeroPos.Z;// zeroPos 就是最高点
            }
            else
            {
                pos_index = tag;

                x_dst = LayoutDrawing.loadPoints[pos_index].X;
                z_dst = LayoutDrawing.loadPoints[pos_index].Z;
            }

            // -----------------------------------------------------------------------------
            //
            //  任务线程
            //
            if (threadAB != null && threadAB.ThreadState == ThreadState.Running)
            {
                MessageBox.Show(this, "龙门命令正在执行中，本次命令无效！");
                return;
            }
            threadAB = new Thread(new ThreadStart(ThreadAB)); //也可简写为new Thread(ThreadMethod);
            threadAB.Start(); //启动线程
        }

        private void ThreadAB()
        {
            layoutDrawing1.Pos2Pos();
            // 创建任务
            if (z_dst < layoutDrawing1.currPosDraw.Z)
            {
                layoutDrawing1.ManualUp();
            }
            else if (z_dst > layoutDrawing1.currPosDraw.Z)
            {
                if (x_dst == layoutDrawing1.currPosDraw.X)
                {
                    layoutDrawing1.ManualDown(pos_index);
                }
            }
            else if (x_dst != layoutDrawing1.currPosDraw.X)//动画位置判断
            {
                if (layoutDrawing1.currPosDraw.Z == layoutDrawing1.zeroPos.Z)
                {
                    layoutDrawing1.ManualHorizontalMove(pos_index);
                }
            }
        }

        private void buttonUp_Click(object sender, EventArgs e)
        {
            if (GenericOp.AutoMode)
            {
                MessageBox.Show("当前为自动模式，请切换为手动模式进行操作！");
                return;
            }
            DialogResult dr = MessageBox.Show("确定执行：上升动作。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                Z_DRVI(false);
            }
        }

        private void buttonDown_Click(object sender, EventArgs e)
        {
            if (GenericOp.AutoMode)
            {
                MessageBox.Show("当前为自动模式，请切换为手动模式进行操作！");
                return;
            }
            DialogResult dr = MessageBox.Show("确定执行：下降动作。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                Z_DRVI(true);
            }
        }

        private void X_DRVI(bool forward)
        {
            int speed, dis;
            // 相对运动
            try
            {
                // 相对运动
                // 运动距离
                dis = trackBarDistanceX.Value;
                speed = trackBarSpeedX.Value;

                if (!forward) dis = -dis;
                SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.X_RELATIVE;
                SerialWireless.gtryPos = dis;
                SerialWireless.gtrySpeed = speed;
            }
            catch (Exception e1)
            {
                MessageBox.Show("请确保运动距离与运动速度为有效值（正整数）");
            }
        }

        private void Z_DRVI(bool up)
        {
            int speed, dis;
            // 相对运动
            try
            {
                // 相对运动
                // 运动距离
                dis = trackBarDistanceZ.Value;
                speed = trackBarSpeedZ.Value;

                if (!up) dis = -dis;
                SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.Z_RELATIVE;
                SerialWireless.gtryPos = dis;
                SerialWireless.gtrySpeed = speed;
            }
            catch (Exception e1)
            {
                MessageBox.Show("请确保运动距离与运动速度为有效值（正整数）");
            }
        }

        private void buttonForward_Click(object sender, EventArgs e)
        {
            if (GenericOp.AutoMode)
            {
                MessageBox.Show("当前为自动模式，请切换为手动模式进行操作！");
                return;
            }

            DialogResult dr = MessageBox.Show("确定执行：前进动作。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                X_DRVI(true);
            }
        }

        private void buttonBackward_Click(object sender, EventArgs e)
        {
            if (GenericOp.AutoMode)
            {
                MessageBox.Show("当前为自动模式，请切换为手动模式进行操作！");
                return;
            }

            DialogResult dr = MessageBox.Show("确定执行：后退动作。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                X_DRVI(false);
            }
        }

        private void textBoxPos_TextChanged(object sender, EventArgs e)
        {
        }

        private int weight_lpf;

        private void timer1_Tick(object sender, EventArgs e)
        {
            weight_lpf = (GenericOp.weight * 2 + weight_lpf * 8) / 10;
            labelPosX.Text = "X位置：" + GenericOp.xPos.ToString() + "[" + GenericOp.xPos_servo.ToString() + "] ,Z位置：" + GenericOp.zPos.ToString() + "[" + GenericOp.zPos_servo.ToString() + "] ,重量：" + weight_lpf.ToString() + "公斤";
            if (SerialWireless.GetGtryState(SerialWireless.GTRY_STATE_CLAMP))
            {
                labelPosX.Text += ",夹爪合";
            }
            if (SerialWireless.GetGtryState(SerialWireless.GTRY_STATE_CLAMP_RELAX))
            {
                labelPosX.Text += ",夹爪开";
            }

            label1.Text = "";
            for (int i = 0; i < 7; i++)
            {
                if (SerialManual.stoveLidState[i] == SerialManual.STOVE_LID_STATE.CLOSED)
                {
                    label1.Text += (i + 1).ToString() + "#关;      ";
                }
                else if (SerialManual.stoveLidState[i] == SerialManual.STOVE_LID_STATE.OPENED)
                {
                    label1.Text += (i + 1).ToString() + "#开;      ";
                }
                else
                {
                    label1.Text += (i + 1).ToString() + "#未知;    ";
                }
            }
            label2.Text = "";
            if (SerialManual.stoveLidState[8] == SerialManual.STOVE_LID_STATE.ERROR)
            {
                label2.Text += "前门打开";
            }
            else
            {
                label2.Text += "前门关闭";
            }
            houmen.Text = "";
            if (SerialManual.stoveLidState[7] == SerialManual.STOVE_LID_STATE.ERROR)
            {
                houmen.Text += "后门打开";
            }
            else
            {
                houmen.Text += "后门关闭";
            }
            // 手动、自动按钮的 button 样式改变。
            if (GenericOp.AutoMode)
            {
                buttonAuto.BackColor = systemBlue;
                buttonManual.BackColor = Color.LightGray;
            }
            else
            {
                buttonAuto.BackColor = Color.LightGray;
                buttonManual.BackColor = systemBlue;
            }
            //
            if (GenericOp.EStop_Soft)
            {
                pictureBoxEStop.Image = HY_PIP.Properties.Resources.estop_down;
            }
            else
            {
                pictureBoxEStop.Image = HY_PIP.Properties.Resources.estop_up;
            }
        }

        private void trackBarXdistance_Scroll(object sender, EventArgs e)
        {
            textBoxDistanceX.Text = trackBarDistanceX.Value.ToString();
        }

        private void trackBarSpeedX_Scroll(object sender, EventArgs e)
        {
            textBoxSpeedRelativeX.Text = trackBarSpeedX.Value.ToString();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            textBoxDistanceZ.Text = trackBarDistanceZ.Value.ToString();
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            textBoxSpeedRelativeZ.Text = trackBarSpeedZ.Value.ToString();
        }

        private void textBoxDistanceX_TextChanged(object sender, EventArgs e)
        {
            int dis = 0;
            try
            {
                dis = Convert.ToInt32(textBoxDistanceX.Text);
                dis = Math.Max(0, dis);
                dis = Math.Min(dis, trackBarDistanceX.Maximum);
            }
            catch (Exception e1)
            {
                textBoxDistanceX.Text = "0";
                dis = 0;
            }
            textBoxDistanceX.Text = dis.ToString();
            trackBarDistanceX.Value = dis;
        }

        private void textBoxSpeedRelativeX_TextChanged(object sender, EventArgs e)
        {
            int speed = 0;
            try
            {
                speed = Convert.ToInt32(textBoxSpeedRelativeX.Text);
                speed = Math.Max(0, speed);
                speed = Math.Min(speed, trackBarSpeedX.Maximum);
            }
            catch (Exception e1)
            {
                textBoxSpeedRelativeX.Text = "0";
                speed = 0;
            }
            textBoxSpeedRelativeX.Text = speed.ToString();
            trackBarSpeedX.Value = speed;
        }

        private void textBoxDistanceZ_TextChanged(object sender, EventArgs e)
        {
            int dis = 0;
            try
            {
                dis = Convert.ToInt32(textBoxDistanceZ.Text);
                dis = Math.Max(0, dis);
                dis = Math.Min(dis, trackBarDistanceZ.Maximum);
            }
            catch (Exception e1)
            {
                dis = 0;
            }
            textBoxDistanceZ.Text = dis.ToString();
            trackBarDistanceZ.Value = dis;
        }

        private void textBoxSpeedRelativeZ_TextChanged(object sender, EventArgs e)
        {
            int speed = 0;
            try
            {
                speed = Convert.ToInt32(textBoxSpeedRelativeZ.Text);
                speed = Math.Max(0, speed);
                speed = Math.Min(speed, trackBarSpeedZ.Maximum);
            }
            catch (Exception e1)
            {
                textBoxSpeedRelativeZ.Text = "0";
                speed = 0;
            }
            textBoxSpeedRelativeZ.Text = speed.ToString();
            trackBarSpeedZ.Value = speed;
        }

        private void buttonClampRelax_Click(object sender, EventArgs e)
        {
            if (GenericOp.AutoMode)
            {
                MessageBox.Show("当前为自动模式，请切换为手动模式进行操作！");
                return;
            }

            DialogResult dr = MessageBox.Show("确定执行：夹爪松开动作。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.CLAMP_RELAX;
            }
        }

        private void buttonClamp_Click(object sender, EventArgs e)
        {
            if (GenericOp.AutoMode)
            {
                MessageBox.Show("当前为自动模式，请切换为手动模式进行操作！");
                return;
            }
            DialogResult dr = MessageBox.Show("确定执行：抓取动作。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.CLAMP;
            }
        }

        private void buttonXPosSetZero_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定重新设置龙门“X”位置的零点吗？\r\n注意：每次断电，零点都会丢失，在上电时，龙门会自动寻找零点。\r\n本命令可以手动设置任意位置为零点。\r\n" +
                "自行设置零点，可能导致龙门工作不正常，甚至导致龙门撞毁。", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (dr == DialogResult.OK)
            {
                SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.XPOS_SET_ZREO;
                SerialWireless.gtryPos = 0x04030201;
            }
            else if (dr == DialogResult.Cancel)
            {
            }
        }

        private void buttonZPosSetZero_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定重新设置龙门“Z”位置的零点吗？\r\n注意：每次断电，零点都会丢失，在上电时，龙门会自动寻找零点。\r\n本命令可以手动设置任意位置为零点。\r\n" +
                "自行设置零点，可能导致龙门工作不正常，甚至导致龙门撞毁。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.ZPOS_SET_ZERO;
                SerialWireless.gtryPos = 0x08070605;
            }
            else if (dr == DialogResult.Cancel)
            {
            }
        }

        private void pictureBoxEStop_Click(object sender, EventArgs e)
        {
            GenericOp.EStop_Soft = !GenericOp.EStop_Soft;
            GenericOp.EStop = GenericOp.EStop_Soft || GenericOp.EStop_Manual;
            if (GenericOp.EStop_Soft)
            {
                SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.STOP;
                pictureBoxEStop.Image = HY_PIP.Properties.Resources.estop_down;
            }
            if (!GenericOp.EStop)
            {
                SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.STOP_RELEASE;
                pictureBoxEStop.Image = HY_PIP.Properties.Resources.estop_up;
            }
        }

        private void groupBox4_Enter(object sender, EventArgs e)
        {
        }

        private void buttonAuto_Click(object sender, EventArgs e)
        {
            if (GenericOp.RequestZRN) { MessageBox.Show("龙门机构（天车）需要进行原点回归，请手动操作原点回归。"); return; }

            buttonAuto.BackColor = systemBlue;
            buttonManual.BackColor = Color.LightGray;
            GenericOp.AutoMode = true;
        }

        private void buttonManual_Click(object sender, EventArgs e)
        {
            buttonAuto.BackColor = Color.LightGray;
            buttonManual.BackColor = systemBlue;
            GenericOp.AutoMode = false;
        }

        private bool expandPanel = true;

        private void pictureBoxExpand_Click(object sender, EventArgs e)
        {
            if (expandPanel)
            {
                expandPanel = false;// 关闭面板
                pictureBoxExpand.Image = HY_PIP.Properties.Resources.button_down_2;
                this.Dock = DockStyle.None;
                this.Height = 580;// 高度
            }
            else
            {
                expandPanel = true;// 展开面板
                pictureBoxExpand.Image = HY_PIP.Properties.Resources.button_up_2;
                //this.Height = MainFrame.mainForm.ClientRectangle.Height;
                this.Dock = DockStyle.Fill;
            }
        }

        private void buttonX_ZRN_Click(object sender, EventArgs e)
        {
            if (GenericOp.AutoMode)
            {
                MessageBox.Show("当前为自动模式，请切换为手动模式进行操作！");
                return;
            }
            DialogResult dr = MessageBox.Show("确定执行：原点回归。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.X_ZRN;
            }
        }

        private void buttonZ_ZRN_Click(object sender, EventArgs e)
        {
            if (GenericOp.AutoMode)
            {
                MessageBox.Show("当前为自动模式，请切换为手动模式进行操作！");
                return;
            }
            DialogResult dr = MessageBox.Show("确定执行：原点回归。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.Z_ZRN;
            }
        }

        private void panelX_Paint(object sender, PaintEventArgs e)
        {
        }

        private void buttonOpen1_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定执行：预热炉炉盖打开。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialManual.OpenStoveLid(0);//打开预热炉炉盖
            }
        }

        private void buttonClose1_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定执行：预热炉炉盖关闭。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialManual.CloseStoveLid(0);//关闭预热炉炉盖
            }
        }

        private void buttonOpen2_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定执行：1#氮化炉炉盖打开。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialManual.OpenStoveLid(1);//打开1#氮化炉炉盖
            }
        }

        private void buttonClose2_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定执行：1#氮化炉炉盖关闭。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialManual.CloseStoveLid(1);//关闭1#氮化炉炉盖
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SerialManual.ClearStoveCmd();
        }

        private void buttonOpen3_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定执行：2#氮化炉炉盖打开。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialManual.OpenStoveLid(2);//打开2#氮化炉炉盖
            }
        }

        private void buttonClose3_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定执行：2#氮化炉炉炉盖关闭。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialManual.CloseStoveLid(2);//关闭2#氮化炉炉盖
            }
        }

        private void buttonOpen4_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定执行：氧化炉炉盖打开。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialManual.OpenStoveLid(3);//打开氧化炉炉盖
            }
        }

        private void buttonClose4_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定执行：氧化炉炉盖关闭。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialManual.CloseStoveLid(3);//关闭氧化炉炉盖
            }
        }

        private void buttonOpen5_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定执行：离子稳定炉炉盖打开。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialManual.OpenStoveLid(4);//打开离子稳定炉炉盖
            }
        }

        private void buttonClose5_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定执行：离子稳定炉炉盖关闭。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialManual.CloseStoveLid(4);//关闭离子稳定炉炉盖
            }
        }

        private void buttonOpen6_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定执行：油炉炉盖打开。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialManual.OpenStoveLid(5);//打开油炉炉盖
            }
        }

        private void buttonClose6_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定执行：油炉炉盖关闭。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialManual.CloseStoveLid(5);//关闭油炉炉盖
            }
        }

        private void buttonOpen7_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定执行：除渣炉炉盖打开。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialManual.OpenStoveLid(6);//打开除渣炉炉盖
            }
        }

        private void buttonClose7_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定执行：除渣炉炉盖关闭。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialManual.CloseStoveLid(6);//关闭除渣炉炉盖
            }
        }

        private GenericOp.GTRY_ACTION_TYPE manual_gtry_action_last;
        private bool mode_last;
        private bool manual_cancel_flag = false;
        private int manual_cancel_cntr = 0;

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (GenericOp.AutoMode)
            {
                if (mode_last != true)
                {// 上次为手动，这次为自动，那么就急停
                    GenericOp.EStop_Soft = true;
                }

                mode_last = GenericOp.AutoMode;
                return;// 返回，不要执行后边的手动程序。
            }
            else
            {
                mode_last = GenericOp.AutoMode;
            }

            if (GenericOp.manual_gtry_action == GenericOp.GTRY_ACTION_TYPE.FORWARD)
            {
                // 前进
                SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.X_RELATIVE;
                SerialWireless.gtryPos = 30000;// 30000mm
                SerialWireless.gtrySpeed = (trackBarSpeedX.Value < 50) ? trackBarSpeedX.Value : 50;
            }
            else if (GenericOp.manual_gtry_action == GenericOp.GTRY_ACTION_TYPE.BAKWARD)
            {
                // 后退
                SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.X_RELATIVE;
                SerialWireless.gtryPos = -30000;// 30米
                SerialWireless.gtrySpeed = (trackBarSpeedX.Value < 50) ? trackBarSpeedX.Value : 50;
            }
            else if (GenericOp.manual_gtry_action == GenericOp.GTRY_ACTION_TYPE.UP)
            {
                // 上升
                SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.Z_RELATIVE;
                SerialWireless.gtryPos = -3000;// 3米
                SerialWireless.gtrySpeed = (trackBarSpeedZ.Value < 20) ? trackBarSpeedZ.Value : 20;
            }
            else if (GenericOp.manual_gtry_action == GenericOp.GTRY_ACTION_TYPE.DOWN)
            {
                // 下降
                SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.Z_RELATIVE;
                SerialWireless.gtryPos = 3000;// 3米
                SerialWireless.gtrySpeed = (trackBarSpeedZ.Value < 20) ? trackBarSpeedZ.Value : 20;
            }
            else if (GenericOp.manual_gtry_action == GenericOp.GTRY_ACTION_TYPE.CLAMP)
            {
                SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.CLAMP;// 抓
            }
            else if (GenericOp.manual_gtry_action == GenericOp.GTRY_ACTION_TYPE.CLAMP_RELAX)
            {
                SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.CLAMP_RELAX;// 放
            }

            //
            if (GenericOp.manual_gtry_action == GenericOp.GTRY_ACTION_TYPE.NONE)
            {
                if (manual_gtry_action_last != GenericOp.GTRY_ACTION_TYPE.NONE)
                {
                    manual_cancel_flag = true;
                    manual_cancel_cntr = 0;
                }
            }
            manual_gtry_action_last = GenericOp.manual_gtry_action;

            if (manual_cancel_flag)
            {
                manual_cancel_cntr++;
                if (manual_cancel_cntr < 20)
                {
                    SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.CMD_CANCEL;// 龙门命令取消
                }
                else
                {
                    manual_cancel_cntr = 0;
                    manual_cancel_flag = false;
                }
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void pictureBoxBack_Click(object sender, EventArgs e)
        {
        }

        private void button6_Click(object sender, EventArgs e)
        {
        }

        private void button3_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定执行：前门打开。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialManual.CloseStoveLid(7);//打开前门
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定执行：前门关闭。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialManual.OpenStoveLid(7);//关闭后门
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定执行：后门打开。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialManual.OpenStoveLid(8);//打开后门
            }
        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定执行：后门关闭。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialManual.CloseStoveLid(8);//关闭后门
            }
        }

        private void transparentPanel28_Click(object sender, EventArgs e)
        {
        }

        private void transparentPanel27_Click(object sender, EventArgs e)
        {
        }

        private void labelPosX_Click(object sender, EventArgs e)
        {
        }

        private void button2_Click(object sender, EventArgs e)
        {
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            SerialManual.OpenStoveLid(9);//同环线
            SerialManual.OpenStoveLid(10);
            SerialManual.OpenStoveLid(11);
            SerialManual.OpenStoveLid(12);
            SerialManual.OpenStoveLid(13);
            SerialManual.OpenStoveLid(14);
            SerialManual.OpenStoveLid(15);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            SerialManual.CloseStoveLid(9);
            SerialManual.CloseStoveLid(10);
            SerialManual.CloseStoveLid(11);
            SerialManual.CloseStoveLid(12);
            SerialManual.CloseStoveLid(13);
            SerialManual.CloseStoveLid(14);
            SerialManual.CloseStoveLid(15);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定执行：温度设置。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                GenericOp.temperature4 = Convert.ToInt32(textBox1.Text);
                GenericOp.temperature5 = Convert.ToInt32(textBox2.Text);
                GenericOp.temperature6 = Convert.ToInt32(textBox3.Text);
                GenericOp.temperature7 = Convert.ToInt32(textBox4.Text);
                GenericOp.temperature11 = Convert.ToInt32(textBox5.Text);
                GenericOp.temperature12 = Convert.ToInt32(textBox6.Text);
                GenericOp.temperature1_1 = Convert.ToInt32(textBox7.Text);
                GenericOp.temperature2_1 = Convert.ToInt32(textBox8.Text);
                GenericOp.temperature3_1 = Convert.ToInt32(textBox9.Text);
                SerialTemp.commState = SerialTemp.COMM_STATE.IDLE;
                Thread.Sleep(100);
                SerialTemp.commState = SerialTemp.COMM_STATE.IDLE;
                Thread.Sleep(100);
                SerialTemp.commState = SerialTemp.COMM_STATE.IDLE;
                Thread.Sleep(100);
                SerialTemp.commState = SerialTemp.COMM_STATE.IDLE;
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定执行：润滑z轴。", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.GREASE;
            }
        }
    }
}