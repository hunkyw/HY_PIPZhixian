using D5ScannerClassLibrary;
using System;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace HY_PIP
{
    public partial class PersonForm : Form
    {
        private ushort dev_id = 0;

        public static readonly int DEV_MAX_NUM = 5;

        public static readonly int RAW_WIDTH = 256;
        public static readonly int RAW_HEIGHT = 296;
        public static readonly int RAW_SIZE = (256 * 296);

        public static readonly int LED_R = 0x01;
        public static readonly int LED_G = 0x02;
        public static readonly int LED_B = 0x04;
        public static readonly int LED_SENSOR = 0x80;

        private D5ScannerClass D5Scanner = new D5ScannerClass();

        private byte[] gpImage = new byte[RAW_SIZE];
        private byte[] gpImageCap = new byte[RAW_SIZE];
        private byte[] gpImageShow = new byte[RAW_SIZE * 3];
        private byte[] gpFeature = new byte[256];
        private byte[] gpFeature2 = new byte[256];
        private byte[,] gpFeaBuf = new byte[1000, 256];

        private ushort gnScale = 97;
        private bool bFingerOn = false;
        private ushort uCircle = 0;

        public bool detectedFinger = false;

        private Bitmap bitmap = new Bitmap(RAW_WIDTH, RAW_HEIGHT);
        private BitmapData bmpData;

        public hyPersonGroup personGroup;
        private DataTable dt;

        public hyPerson currPerson;

        public PersonForm()
        {
            InitializeComponent();
        }

        private void PersonForm_Load(object sender, EventArgs e)
        {
            //dataGridView1的初始化
            DataGridViewInit();

            //
            comboBoxPosition.Items.Add("操作员");
            comboBoxPosition.Items.Add("技术员");
            comboBoxPosition.Items.Add("管理员");
            comboBoxPosition.SelectedIndex = 0;

            // 打开指纹设备
            OpenFingerPrintDevice();
        }

        private void DataGridViewInit()
        {
            dt = new DataTable();

            // 添加表头
            dt.Columns.Add("识别码");// ID，识别码唯一，不隐藏该列
            dt.Columns.Add("姓名");
            dt.Columns.Add("工号");
            dt.Columns.Add("岗位");

            // 添加行
            foreach (hyPerson person in personGroup.personList)
            {
                string[] strArr = new string[4];//
                strArr[0] = person.id.ToString();// id 是唯一的。
                strArr[1] = person.name;//
                strArr[2] = person.job_number;//
                strArr[3] = person.position;//
                dt.Rows.Add(strArr);
            }

            // 绑定
            this.dataGridView1.DataSource = dt;
            //this.dataGridView1.Columns[0].Visible = false;// 不显示person.id这一列。
            //this.dataGridView1.ColumnHeadersHeight = 30;
            this.dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            // 高度
            this.dataGridView1.Height = this.dataGridView1.ColumnHeadersHeight + this.dataGridView1.RowTemplate.Height * 14;
        }

        public void btnOpenDevice_Click(object sender, EventArgs e)
        {
        }

        private int a;

        public void OpenFingerPrintDevice()
        {
            String[] pDevName = new string[5];
            ushort uNum;

            uNum = D5Scanner.EnumDevice(pDevName);
            if (uNum <= 0)
            {
                MessageBox.Show("没有检测到指纹采集设备!");
            }

            int lRet;

            lRet = D5Scanner.OpenDevice(dev_id);// 打开设备0
            a = D5Scanner.OpenLED(dev_id, (ushort)LED_SENSOR);

            if (lRet == 0)
            {
                D5Scanner.Beep(dev_id, 100);
                timer1.Start();
                // 设备打开成功
                //MessageBox.Show("设备打开成功！");
            }
            else
            {
                MessageBox.Show("设备打开失败！");
            }
        }

        public void CloseFingerPrintDevice()
        {
            // 关闭指纹灯
            D5Scanner.CloseLED(dev_id, (ushort)0xffff);
            D5Scanner.CloseDevice(dev_id);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            ScanFingerPrint();
        }

        public void ScanFingerPrint()
        {
            bool bFP;
            int i;

            // 获取指纹图像，读取图像
            int rc = D5Scanner.GetImage(dev_id, gpImage, ref gnScale);
            if (rc != 0) return;

            // 检测某图像是否有指纹
            bFP = D5Scanner.CheckFP(gpImage);

            if (bFP)
            {//检测到指纹
                if (!bFingerOn)
                {
                    uCircle++;
                    if (uCircle >= 2)
                    {// 从没有指纹，到检测到有指纹，需要校验检测2次，避免错误检测。
                        bFingerOn = true;

                        D5Scanner.Beep(dev_id, 40);
                        System.Threading.Thread.Sleep(100);

                        for (i = 0; i < RAW_SIZE; i++)
                        {
                            gpImageCap[i] = gpImage[i];
                        }

                        // 对输入的指纹图像数据提取特征值
                        // D5Scanner.Process(gpImageCap, gpFeature, gnScale);
                        detectedFinger = true;
                        // 绘图
                        DrawFingerPrint();
                    }
                }
            }
            else
            {//无指纹
                bFingerOn = false;
                uCircle = 0;
            }
        }

        private void DrawFingerPrint()
        {
            IntPtr ptr;
            uint lIndx;
            uint lIndx2;
            int i, j;

            bmpData = bitmap.LockBits(new Rectangle(0, 0, RAW_WIDTH, RAW_HEIGHT),
                                        ImageLockMode.ReadWrite,
                                        PixelFormat.Format24bppRgb);
            ptr = bmpData.Scan0;

            for (i = 0; i < RAW_HEIGHT; i++)
            {
                lIndx = (uint)(3 * RAW_WIDTH * (RAW_HEIGHT - 1 - i));
                lIndx2 = (uint)(RAW_WIDTH * i);

                for (j = 0; j < RAW_WIDTH; j++)
                {
                    gpImageShow[lIndx++] = gpImageCap[lIndx2];
                    gpImageShow[lIndx++] = gpImageCap[lIndx2];
                    gpImageShow[lIndx++] = gpImageCap[lIndx2++];
                }
            }

            Marshal.Copy(gpImageShow, 0, ptr, RAW_SIZE * 3);

            bitmap.UnlockBits(bmpData);

            this.pictureBoxFingerPrint.Image = bitmap;
            this.pictureBoxFingerPrint.SizeMode = PictureBoxSizeMode.Zoom;
        }

        public int LoadFeature(String fName, byte[] pFea)
        {
            FileStream stream = null;
            BinaryReader reader = null;

            stream = new FileStream(fName, FileMode.Open);
            if (stream == null)
            {
                return -1;
            }
            reader = new BinaryReader(stream, Encoding.Default);
            if (reader == null)
            {
                return -1;
            }

            reader.Read(pFea, 0, 256);

            reader.Close();
            stream.Close();

            return 0;
        }

        public int MatchN()
        {
            String pPathName;
            DirectoryInfo fdir;
            FileInfo[] file;
            int uFeaCnt;
            int lRet;
            int i;

            D5Scanner.Process(gpImageCap, gpFeature, gnScale);

            pPathName = System.Environment.CurrentDirectory;
            fdir = new DirectoryInfo(pPathName);
            file = fdir.GetFiles("*.fea");

            // 将所有需要比对的指纹全部都提取出来。
            uFeaCnt = 0;// 清零
            foreach (hyPerson person in personGroup.personList)
            {
                string fname = "Person\\" + person.id.ToString() + ".fea";
                if (File.Exists(fname))
                {
                    lRet = LoadFeature(fname, gpFeature2);// 读取所有特征文件
                    if (lRet == -1)
                    {
                        MessageBox.Show("无效指纹特征文件");
                        //return;
                    }
                    for (i = 0; i < 256; i++)
                    {
                        gpFeaBuf[uFeaCnt, i] = gpFeature2[i];
                    }
                    uFeaCnt++;
                }
            }

            // 开始比对
            lRet = D5Scanner.MatchN(gpFeature, gpFeaBuf, (uint)uFeaCnt, 180, 5);

            if (lRet >= 0)
            {
                D5Scanner.Beep(dev_id, 40);
                return personGroup.personList[lRet].id;
            }
            else
            {
                D5Scanner.Beep(dev_id, 200);
                //MessageBox.Show("失败");
                return -1;
                /*
                D5Scanner.CloseLED((ushort)comboDevice.SelectedIndex, (ushort)LED_B);
                D5Scanner.OpenLED((ushort)comboDevice.SelectedIndex, (ushort)LED_R);
                D5Scanner.Beep((ushort)comboDevice.SelectedIndex, 160);
                D5Scanner.CloseLED((ushort)comboDevice.SelectedIndex, (ushort)LED_R);
                D5Scanner.OpenLED((ushort)comboDevice.SelectedIndex, (ushort)LED_B);*/
            }
        }

        private string jpg_fname = "";

        private void pictureBoxPotrait_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.RestoreDirectory = true;
            //openFileDialog.InitialDirectory = "d:\\";
            openFileDialog.Filter = "JPG|*.jpg|JPEG|*.jpeg";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                jpg_fname = openFileDialog.FileName;
                Stream s = File.Open(jpg_fname, FileMode.Open);
                pictureBoxPotrait.Image = Image.FromStream(s);
                s.Close();
            }
        }

        /*********************************************************
         *
         * 新增人员信息
         *
         * 保存特征
         *
         * */

        private void buttonNew_Click(object sender, EventArgs e)
        {
            // 一般性检查
            if (textBoxName.Text == "") { MessageBox.Show(this, "人名不能为空！"); return; }
            if (textBoxJobNumber.Text == "") { MessageBox.Show(this, "工号不能为空！"); return; }
            if (textBoxPwd.Text == "") { MessageBox.Show(this, "密码不能为空！"); return; }
            // 检测某图像是否有指纹
            if (detectedFinger != true) { MessageBox.Show(this, "请输入有效指纹！"); return; }

            // 操作确认
            DialogResult dr = MessageBox.Show("确定新增人员信息吗？", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.Cancel)
            {
                return;
            }

            // ----------------------------------------------------
            // 从 textBox 中获取信息
            hyPerson person = new hyPerson();
            // 记录信息(xml）
            person.name = textBoxName.Text;// 姓名
            person.job_number = textBoxJobNumber.Text;// 工号
            person.position = comboBoxPosition.SelectedItem.ToString();// 岗位
            person.password = textBoxPwd.Text;// 密码
            personGroup.NewPerson(person);// person.id 会自动分配

            // ----------------------------------------------------
            // 根据得到的ID号，保存相关文件，指纹图像文件，指纹特征文件，人物头像文件
            // 创建目录
            string person_path = "Person\\";
            string fname_light = person.id.ToString();
            if (!Directory.Exists(person_path))
            {// 检查LOG目录是否存在
                Directory.CreateDirectory(person_path);
            }

            // ----------------------------------------------------
            // 保存人物头像
            if (jpg_fname != "")
            {
                File.Copy(jpg_fname, person_path + fname_light + ".jpg");
            }

            // ----------------------------------------------------
            // 保存指纹图像  保存指纹特征
            try
            {
                // 对输入的指纹图像数据提取特征值
                D5Scanner.Process(gpImageCap, gpFeature, gnScale);

                FileStream stream = null;
                BinaryWriter writer = null;
                // 保存指纹图像
                D5Scanner.SaveBMPFile(person_path + fname_light + ".bmp", gpImageCap);
                // 保存指纹特征
                stream = new FileStream(person_path + fname_light + ".fea", FileMode.OpenOrCreate);
                if (stream == null) return;
                writer = new BinaryWriter(stream, Encoding.Default);
                if (writer == null) return;

                writer.Write(gpFeature, 0, 256);

                writer.Close();
                stream.Close();
            }
            catch (Exception e1)
            {
            }

            // 更新 dataGridView
            // 只需要更新数据源 DataSource 就可以了。也就是 dt.
            string[] strArr = new string[4];//
            strArr[0] = person.id.ToString();//
            strArr[1] = person.name;//
            strArr[2] = person.job_number;//
            strArr[3] = person.position;//
            dt.Rows.Add(strArr);

            // 跳转选择新增的那一行
            // dataGridView 排序后会引起索引变化，那么如何找到该索引呢，使用下面的方法。
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                if (dataGridView1.Rows[i].Cells[0].Value.ToString() == strArr[0])
                {
                    dataGridView1.CurrentCell = dataGridView1.Rows[i].Cells[0]; // 设置当前行（焦点）
                    break;
                }
            }
        }

        private void buttonPrev_Click(object sender, EventArgs e)
        {
            detectedFinger = false;

            // 上一条工艺
            int i = dataGridView1.SelectedRows[0].Index - 1;
            if (i < 0) i = 0;
            //rowMergeView1.Rows[i].Selected = true;
            dataGridView1.CurrentCell = dataGridView1.Rows[i].Cells[0]; // 设置当前行（焦点）
        }

        private void buttonNext_Click(object sender, EventArgs e)
        {
            detectedFinger = false;

            // 下一条工艺
            int i = dataGridView1.SelectedRows[0].Index + 1;
            if (i >= dataGridView1.RowCount) i = dataGridView1.RowCount - 1;
            //rowMergeView1.Rows[i].Selected = true;
            dataGridView1.CurrentCell = dataGridView1.Rows[i].Cells[0]; // 设置当前行（焦点）
        }

        private bool deleteFlag = false;

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定要删除当前人员吗？", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.Cancel)
            {
                return;
            }

            detectedFinger = false;

            // 删除 dataGridView 中的人员
            // dataGridView 排序后会引起索引变化，那么如何找到该索引呢，使用下面的方法。
            DataRowView drv = dataGridView1.SelectedRows[0].DataBoundItem as DataRowView;
            int index = dt.Rows.IndexOf(drv.Row);

            int id = personGroup.personList[index].id;// 或者选择这一行的ID，识别码

            // 删除指纹特征文件
            string fname = "Person\\" + id.ToString() + ".fea";
            if (File.Exists(fname)) File.Delete(fname);

            // 删除指纹图像文件
            //pictureBoxFingerPrint.Image = HY_PIP.Properties.Resources.fingerprint;// 将指纹图像变回默认
            fname = "Person\\" + id.ToString() + ".bmp";
            if (File.Exists(fname)) File.Delete(fname);/*try { File.Delete(fname); }
                catch (Exception e1) { }*/

            // 删除人物头像文件
            //pictureBoxPotrait.Image = HY_PIP.Properties.Resources.portrait;// 将人物头像变回默认
            fname = "Person\\" + id.ToString() + ".jpg";
            if (File.Exists(fname)) File.Delete(fname); /*try { File.Delete(fname); }
                catch (Exception e1) { }*/

            // 第一步：删除 XML 文件内容，同时删除 processGroup 中的工艺列表 processList
            personGroup.DeletePerson(index, id);
            // 第二步：删除 dataGridView 该行数据。
            deleteFlag = true;// 特殊处理。需要增加一个deleteFlag，观察发现，每次删除后，选中的行的内容有错位。
            dt.Rows[index].Delete();
        }

        private void PushDataToTextBox()
        {
            // dataGridView 排序后会引起索引变化，那么如何找到该索引呢，使用下面的方法。
            DataRowView drv = dataGridView1.SelectedRows[0].DataBoundItem as DataRowView;
            int index = dt.Rows.IndexOf(drv.Row);
            if (deleteFlag)
            {
                deleteFlag = false;
                index--;//// 特殊处理。需要增加一个deleteFlag，观察发现，每次删除后，选中的行的内容有错位。
                if (index < 0) index = 0;
            }

            textBoxName.Text = personGroup.personList[index].name;// 姓名
            textBoxJobNumber.Text = personGroup.personList[index].job_number;// 工号
            comboBoxPosition.SelectedIndex = comboBoxPosition.Items.IndexOf(personGroup.personList[index].position);// 岗位
            textBoxPwd.Text = "";

            // 头像
            int id = personGroup.personList[index].id;
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

            // 指纹
            fname = "Person\\" + id.ToString() + ".bmp";
            if (File.Exists(fname))
            {
                Stream s = File.Open(fname, FileMode.Open);
                pictureBoxFingerPrint.Image = Image.FromStream(s);
                s.Close();
            }
            else
            {
                pictureBoxFingerPrint.Image = HY_PIP.Properties.Resources.fingerprint;
            }

            gpImage = new byte[RAW_SIZE]; ;// 清空指纹，要求重新输入指纹。
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                //textBoxS1Formula.Text = rowMergeView1.SelectedRows[0].Cells["工艺号"].Value.ToString();
                //textBoxS1Time.Text = rowMergeView1.CurrentCell.RowIndex.ToString();;
                //textBoxS1Time.Text = rowMergeView1.SelectedRows[0].Index.ToString();

                detectedFinger = false;

                PushDataToTextBox();// 往 TextBox 中推送数据
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MatchN();
        }

        private void PersonForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 关闭指纹设备
            CloseFingerPrintDevice();
        }
    }
}