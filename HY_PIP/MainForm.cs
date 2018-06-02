using Microsoft.Win32;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

//解除指纹识别，设置串口
namespace HY_PIP
{
    public partial class MainForm : Form
    {
        public static int SystemMinutes;
        public static int SystemSeconds;

        //DateTime baseDateTime;
        private hyWorkGroup workGroup;

        public static hyProcessGroup processGroup;
        public static hyPersonGroup personGroup;
        private SerialWireless serialWireless;
        public static SerialManual serialManual;
        private SerialTemp serialTemp;

        public static Color sysBackColor = Color.FromArgb(36, 36, 36);//(50, 44, 40);

        public enum PERMITION { None = 0, Validate, Operator, Technician, Manager }

        public static PERMITION Permition = PERMITION.None;
        public static int currPersonId = -1;
        public hyPerson currPerson;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.BackColor = MainForm.sysBackColor;

            // Layout 设置
            LayoutInit();

            timerLoadData.Start(); // 加载日志文件

            //
            serialWireless = new SerialWireless("COM3", 19200);
            try
            {
                serialWireless.Open();// 打开串口
            }
            catch (Exception e1)
            {
                MessageBox.Show("无线通信串口打开失败");
            }
            finally { }

            //
            serialManual = new SerialManual("COM5", 19200);
            try
            {
                serialManual.Open();// 打开串口
            }
            catch (Exception e1)
            {
                MessageBox.Show("手动操作台串口打开失败");
            }
            finally { }
            serialTemp = new SerialTemp("COM8", 9600);
            try
            {
                serialTemp.Open();// 打开串口
            }
            catch (Exception e1)
            {
                MessageBox.Show("温度串口打开失败");
            }
            finally { }
        }

        public void LoadPersonInfo()
        {
            foreach (hyPerson person in personGroup.personList)
            {
                if (currPersonId == person.id)
                {
                    currPerson = person;

                    //
                    labelPersonName.Text = currPerson.name;// 姓名
                    labelPersonJobNumber.Text = currPerson.job_number;// 工号
                    labelPersonPosition.Text = currPerson.position;// 岗位

                    // 操作权限
                    switch (currPerson.position)
                    {
                        case "操作员":
                            MainForm.Permition = MainForm.PERMITION.Operator;
                            break;

                        case "技术员":
                            MainForm.Permition = MainForm.PERMITION.Technician;
                            break;

                        case "管理员":
                            MainForm.Permition = MainForm.PERMITION.Manager;
                            break;
                    }

                    // 头像
                    int id = currPerson.id;
                    string fname = "Person\\" + id.ToString() + ".jpg";
                    if (File.Exists(fname))
                    {
                        Stream s = File.Open(fname, FileMode.Open);
                        pictureBoxPotrait.Image = Image.FromStream(s);
                        s.Close();
                    }
                    else
                    {
                        pictureBoxPotrait.Image = HY_PIP.Properties.Resources.portrait;
                    }
                }
            }
        }

        private void LayoutInit()
        {
            float dpi = getLogPiex();
            //label1.Text = this.Width.ToString() + "," + this.Height.ToString() + "," + dpi.ToString();

            panelX.AutoScroll = MainFrame.SCREEN_AUTO_SCROLL;// 如果屏幕尺寸符合 1920*1080，就不用加滚动条。

            // 下面的布局总览图的尺寸设置
            panelX.Width = this.Width;
            panelX.Height = this.Height;

            panel1.Left = 15;
            panel1.Top = 5;
            processPanelCtrl1.Left = 490;
            processPanelCtrl1.Top = 5;

            layoutDrawing1.Width = HY_PIP.Properties.Resources.layout.Width;
            layoutDrawing1.Height = 450;
            layoutDrawing1.Left = 15;//(this.Width - layoutDrawing1.Width)/2;
            layoutDrawing1.Top = 580;//this.Height - 450;// 左下角对齐
        }

        private void ReadDataFile()
        {
            throw new NotImplementedException();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // 更新系统时间
            if (workGroup != null)
            {
                temperature1.Text = GenericOp.temperature1.ToString("") + "°C";
                temperature2.Text = GenericOp.temperature2.ToString("") + "°C";
                temperature3.Text = GenericOp.temperature3.ToString("") + "°C";

                temperature41.Text = GenericOp.temperature41.ToString("") + "°C";
                temperature42.Text = GenericOp.temperature42.ToString("") + "°C";
                temperature51.Text = GenericOp.temperature51.ToString("") + "°C";
                temperature52.Text = GenericOp.temperature52.ToString("") + "°C";
                temperature61.Text = GenericOp.temperature61.ToString("") + "°C";
                temperature62.Text = GenericOp.temperature62.ToString("") + "°C";
                temperature71.Text = GenericOp.temperature71.ToString("") + "°C";
                temperature72.Text = GenericOp.temperature72.ToString("") + "°C";

                temperature111.Text = GenericOp.temperature111.ToString("") + "°C";
                temperature112.Text = GenericOp.temperature112.ToString("") + "°C";
                temperature121.Text = GenericOp.temperature121.ToString("") + "°C";
                temperature122.Text = GenericOp.temperature122.ToString("") + "°C";
                // 处理系统时间（分钟为单位）
                TimeSpan span = DateTime.Now - workGroup.createdTime;
                SystemSeconds = (int)span.TotalSeconds;
                SystemMinutes = (int)span.TotalMinutes + GenericOp.tmpTimeOffset;
                if (LayoutDrawing.drawOnly)
                {
                    //SystemMinutes = SystemSeconds;
                }
                label1.Text = "当前时间： " + SystemMinutes.ToString() + "  当前偏移量： " + GenericOp.tmpTimeOffset.ToString();

                // 换型时间计算
                ChangeOverTask();

                // 动画更新
                layoutDrawing1.Invalidate();// 更新动画
            }
        }

        /*
         *
         *     换型任务
         *
         * */

        private void ChangeOverTask()
        {
            if (GenericOp.EStop || !GenericOp.AutoMode)
            {
                return;// 如果急停，或者手动模式，则不做换型检查。
            }

            // 做换型检查
            foreach (hyWorkFlow wf in workGroup.workFlowList)
            {
                if (wf.carrier.status == hyCarrier.STATUS.IN_STATION)
                {// 只对夹具状态为  IN_STATION  的夹具，进行换型检查。
                    // 查看当前是否有需要换型的夹具，主要是检查 endingTime
                    if (SystemMinutes >= wf.carrier.endingTime)
                    {// 系统时间到了夹具需要切换的时间后，需要进行切换处理。
                        wf.carrier.status = hyCarrier.STATUS.REQUEST_CHANGE_OVER;// 请求换型。请求换型状态不用保存到XML，只有正在换型才保存到XML。
                        //workGroup.UpdateXmlNodeCarrierStatus(wf.carrier.id, wf.carrier.status, wf.carrier.pos);// 更新夹具XML信息

                        // 创建任务
                        ABTask abTask = new ABTask(wf);
                        abTask.AB_TASK_A = wf.carrier_pos;// 起点

                        abTask.AB_TASK_B = wf.NextCarrierPos();// 终点

                        abTask.taskStatus = ABTask.STATUS.IDLE;// 任务状态
                        abTask.carrier = wf.carrier;//载具
                        layoutDrawing1.taskList.Add(abTask);// 添加到任务列表中去
                    }
                }
            }

            // -----------------------------------------------------------------
            //
            //      以下部分就是：执行换型任务
            //
            // 查询是否有需要换型的任务，并添加到任务列表中去。
            // 查询夹具的状态
            if (layoutDrawing1.currTask == null)
            {
                if (layoutDrawing1.taskList.Count != 0)
                {
                    layoutDrawing1.currTask = layoutDrawing1.taskList[0];// 获取当前任务
                    layoutDrawing1.currTask.carrier.status = hyCarrier.STATUS.DOING_CHANGE_OVER;//正在换型
                    workGroup.UpdateXmlNodeCarrierStatus(layoutDrawing1.currTask.carrier.id, layoutDrawing1.currTask.carrier.status, layoutDrawing1.currTask.carrier.pos);// 更新夹具XML信息
                    // 更新任务信息，记录到xml中
                    layoutDrawing1.currTask.carrier.pos = layoutDrawing1.currTask.AB_TASK_A;
                    layoutDrawing1.UpdateXmlNodeTaskInfo(ABTask.STATUS.IDLE, layoutDrawing1.currTask.AB_TASK_A, layoutDrawing1.currTask.AB_TASK_B, layoutDrawing1.currTask.carrier.id);
                }
            }
            else
            {
                if (layoutDrawing1.currTask.taskStatus == ABTask.STATUS.STOP)
                {// 执行任务列表中的任务，执行完成的，就从任务列表中删除。
                    //
                    layoutDrawing1.currTask.carrier.UpdateCarrierInfo(layoutDrawing1.currTask.AB_TASK_B);// 更新夹具位置（已经到达B点）
                    layoutDrawing1.currTask.carrier.pos = layoutDrawing1.currTask.AB_TASK_B;
                    workGroup.UpdateXmlNodeCarrierStatus(layoutDrawing1.currTask.carrier.id, layoutDrawing1.currTask.carrier.status, layoutDrawing1.currTask.carrier.pos);// 更新夹具XML信息.
                    layoutDrawing1.taskList.Remove(layoutDrawing1.currTask);// 任务执行结束，就移除这个项目，在下一次循环的时候就会执行下一个任务。
                    layoutDrawing1.currTask = null;// 清空
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
        }

        public static float getLogPiex()
        {
            float returnValue = 96;
            try
            {
                RegistryKey key = Registry.CurrentUser;
                RegistryKey pixeKey = key.OpenSubKey("Control Panel\\Desktop");
                if (pixeKey != null)
                {
                    var pixels = pixeKey.GetValue("LogPixels");
                    if (pixels != null)
                    {
                        returnValue = float.Parse(pixels.ToString());
                    }
                    pixeKey.Close();
                }
                else
                {
                    pixeKey = key.OpenSubKey("Control Panel\\Desktop\\WindowMetrics");
                    if (pixeKey != null)
                    {
                        var pixels = pixeKey.GetValue("AppliedDPI");
                        if (pixels != null)
                        {
                            returnValue = float.Parse(pixels.ToString());
                        }
                        pixeKey.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                returnValue = 96;
            }
            return returnValue;
        }

        private void timerLoadData_Tick(object sender, EventArgs e)
        {
            timerLoadData.Stop();

            bool newflag = false;
            string path = "LOG\\";
            string fname = path + "hyLog.log";
            string dfname = "";// path + "hyData" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".xml";
            byte[] dataByte = new byte[13];
            try
            {
                if (!Directory.Exists(path))
                {// 检查LOG目录是否存在
                    Directory.CreateDirectory(path);
                }
                if (!File.Exists(fname))
                {// 检查LOG文件是否存在
                    FileStream fs = File.Create(fname);
                    fs.Close();
                }
                FileStream logfile = new FileStream(fname, FileMode.Open);
                logfile.Seek(0, SeekOrigin.Begin);
                logfile.Read(dataByte, 0, dataByte.Length);
                logfile.Close();

                string date = System.Text.Encoding.ASCII.GetString(dataByte);
                dfname = path + "hyData" + date + ".xml";

                if (!File.Exists(dfname))
                {// 检查数据文件是否存在
                    newflag = true;// 新建数据文件
                }
                else
                {
                    DialogResult dr = MessageBox.Show("是否新建一个生产数据文件？", "提示", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.OK)
                    {
                        // 新建数据文件
                        newflag = true;
                    }
                }

                if (newflag)
                {// 新建数据文件
                    date = DateTime.Now.ToString("yyyyMMdd-HHmm");
                    dfname = path + "hyData" + date + ".xml";
                    logfile = new FileStream(fname, FileMode.Create);
                    dataByte = System.Text.Encoding.Default.GetBytes(date);
                    logfile.Write(dataByte, 0, dataByte.Length);
                    logfile.Flush();
                    logfile.Close();

                    ABTask.Init();
                    workGroup = new hyWorkGroup();
                    layoutDrawing1.workGroup = workGroup;
                    processPanelCtrl1.workGroup = workGroup;
                    workGroup.NewWorkGroup(dfname);// 新建工作组
                }
                else
                {// 加载原始数据
                    // 加载 workgroup 数据
                    ABTask.Init();
                    workGroup = new hyWorkGroup();
                    layoutDrawing1.workGroup = workGroup;
                    processPanelCtrl1.workGroup = workGroup;
                    workGroup.LoadWorkGroup(dfname);// 加载已有工作组
                    processPanelCtrl1.UpdateSequentialChart();// 更新时序图
                    //TODO: 加载未完成任务信息
                    layoutDrawing1.LoadXmlNodeTaskInfo();
                    // 绘制尚未结束的夹具信息
                    foreach (hyWorkFlow wf in workGroup.workFlowList)
                    {
                        if ((wf.carrier.pos >= hyWorkFlow.POS_FIRST_STATION) && (wf.carrier.pos <= hyWorkFlow.POS_LAST_STATION))
                        {
                            wf.carrier.currPosDraw = new ServoPoint(LayoutDrawing.loadPoints[wf.carrier.pos].X, LayoutDrawing.loadPoints[wf.carrier.pos].Z);
                            layoutDrawing1.carrierList.Add(wf.carrier);
                        }
                    }
                    layoutDrawing1.Invalidate();// 更新动画
                }

                ManualForm.layoutDrawing1 = this.layoutDrawing1;// 手动界面绘图用。

                timer1.Start();// 加载完成后开启。
                timer2.Start();

                // 加载工艺列表
                LoadProcessGroup();
                // 加载人员列表
                LoadPersonGroup();
            }
            catch
            {
            }
        }

        private void LoadProcessGroup()
        {
            // 加载工艺列表
            processGroup = new hyProcessGroup();
            processGroup.LoadProcessList();

            // ComboBox 的显示
            DataTable dt = new DataTable();
            dt.Columns.Add("process_name");
            dt.Columns.Add("process_id");
            foreach (hyProcess process in processGroup.processList)
            {
                DataRow row = dt.NewRow();
                row["process_name"] = process.process_name;
                row["process_id"] = process.process_id.ToString();
                dt.Rows.Add(row);
            }
            comboBoxProcessName.DataSource = dt;
            comboBoxProcessName.DisplayMember = "process_name";// 显示名
            comboBoxProcessName.ValueMember = "process_id";// 值

            for (int i = 1; i < 31; i++)
            {
                comboBoxCarrierName.Items.Add(i.ToString());
            }
            comboBoxCarrierName.SelectedIndex = 0;
        }

        private void LoadPersonGroup()
        {
            // 加载工艺列表
            personGroup = new hyPersonGroup();
            personGroup.LoadPersonList();

            MainFrame.personForm.personGroup = personGroup;//
        }

        public void pictureBoxEStop_Click(object sender, EventArgs e)
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

        public bool CheckPermition()
        {
            bool rc = true;// 默认允许进入

            MainForm.Permition = MainForm.PERMITION.Manager; //调试使用，mark:dengkan,  解除指纹使用

            if (MainForm.Permition == MainForm.PERMITION.None)
            {// 在这里判断不允许进入的条件
                MainForm.Permition = MainForm.PERMITION.Validate;// 进入验证权限阶段
                PermitForm permitForm = new PermitForm();
                permitForm.StartPosition = FormStartPosition.CenterScreen; //居中显示
                permitForm.ShowDialog(this);
                if (MainForm.Permition <= MainForm.PERMITION.Validate)
                {
                    MainForm.Permition = MainForm.PERMITION.None;// 如果验证权限后，权限依然还是没有升级，那么就依然拦截鼠标消息。
                    rc = false;// 不允许进入。
                }
            }

            return rc;
        }

        private void pictureBoxStart_Click(object sender, EventArgs e)
        {
            if (!CheckPermition()) return;
            if (GenericOp.RequestZRN) { MessageBox.Show("龙门机构（天车）需要进行原点回归，请在手动控制页面操作"); return; }

            pictureBoxStart.Image = HY_PIP.Properties.Resources.starting;
            DialogResult dr = MessageBox.Show("确定新增工艺？", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                DataRowView dataRowProcessName = (DataRowView)(comboBoxProcessName.SelectedItem);
                int process_id;
                int carrier_name;
                try
                {
                    carrier_name = Convert.ToInt32(comboBoxCarrierName.Text);
                    process_id = Convert.ToInt32(dataRowProcessName["process_id"].ToString());
                }
                catch (Exception e1)
                {
                    MessageBox.Show("请检查夹具号，工艺号是否正确。");
                    return;
                }
                workGroup.NewWorkFlow(process_id, carrier_name, 25736, 0);
                processPanelCtrl1.UpdateSequentialChart();
            }
            pictureBoxStart.Image = HY_PIP.Properties.Resources.start;
        }

        private bool EStop_last_state = false;

        private void timer2_Tick(object sender, EventArgs e)
        {
            foreach (hyWorkFlow wf in workGroup.workFlowList)
            {
                if (SystemMinutes < wf.carrier.endingTime)
                {
                    wf.carrier.remainingTime = wf.carrier.endingTime - SystemMinutes;
                }
            }

            // 状态信息处理
            labelWarnning.Text = "状态：";
            if (GenericOp.EStop_Manual)
            {
                labelWarnning.Text += "硬急停按下";
            }
            if (GenericOp.EStop_Soft)
            {
                labelWarnning.Text += "，软急停按下";
            }
            if (GenericOp.EStop)
            {
                labelWarnning.Text += "，总停按下";
            }

            if (GenericOp.EStop_Soft)
            {
                pictureBoxEStop.Image = HY_PIP.Properties.Resources.estop_down;
            }
            else
            {
                pictureBoxEStop.Image = HY_PIP.Properties.Resources.estop_up;
            }

            GenericOp.EStop = GenericOp.EStop_Manual || GenericOp.EStop_Soft;
            if (GenericOp.EStop)
            {
                SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.STOP;
            }
            else
            {
                if (EStop_last_state == true)
                {
                    SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.STOP_RELEASE;
                }
            }
            EStop_last_state = GenericOp.EStop;

            // -------------------------------------------------------
            //  在 请求原点回归 情况下，要强制为手动模式，不允许自动模式运行，也不允许绝对定位，绝对位置运动。
            if (GenericOp.RequestZRN)
            {
                GenericOp.AutoMode = false;// 强制手动模式。
            }
        }

        private void pictureBoxLock_Click(object sender, EventArgs e)
        {
            if (MainForm.currPersonId < 0)
            {
                return;
            }
            DialogResult dr = MessageBox.Show("即将锁屏...", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                MainForm.Permition = MainForm.PERMITION.None;
                MainForm.currPersonId = -1;
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            int value;
            value = Convert.ToInt32(textBox1.Text);
            GenericOp.tmpTimeOffset += value - SystemMinutes;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }

        private void panelX_Paint(object sender, PaintEventArgs e)
        {
        }
    }
}