using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace HY_PIP
{
    public partial class PermitForm : Form
    {
        public PermitForm()
        {
            InitializeComponent();
        }

        private void PermitForm_Load(object sender, EventArgs e)
        {
            // 打开指纹设备
            MainFrame.personForm.OpenFingerPrintDevice();

            // 启动定时器
            timer1.Start();

            // 搜集人员信息
            // ComboBox 的显示
            DataTable dt = new DataTable();
            dt.Columns.Add("id");
            dt.Columns.Add("name");
            dt.Columns.Add("job_number");
            dt.Columns.Add("password");
            foreach (hyPerson person in MainForm.personGroup.personList)
            {
                DataRow row = dt.NewRow();
                row["id"] = person.id;
                row["name"] = person.name;
                row["job_number"] = person.job_number;
                row["password"] = person.password;
                dt.Rows.Add(row);
            }
            comboBoxJobNumber.DataSource = dt;
            comboBoxJobNumber.DisplayMember = "job_number";// 显示名
            if (comboBoxJobNumber.Items.Count > 0)
            {
                comboBoxJobNumber.SelectedIndex = 1;
            }
        }

        private void PermitForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            MainFrame.personForm.CloseFingerPrintDevice();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            MainFrame.personForm.ScanFingerPrint();
            if (MainFrame.personForm.detectedFinger)
            {
                MainFrame.personForm.detectedFinger = false;
                MainForm.currPersonId = MainFrame.personForm.MatchN();// 返回 person id
            }
            if (MainForm.currPersonId >= 0)
            {
                timer1.Stop();// 停止检验
                label1.Text = "有效指纹";
                MainFrame.mainForm.LoadPersonInfo();// 加载人物身份信息

                this.Close();// 关闭
            }
            else
            {
                label1.Text = "无效指纹";
                label1.ForeColor = Color.Red;
                MainForm.Permition = MainForm.PERMITION.None;
            }
        }

        private void buttonPwd_Click(object sender, EventArgs e)
        {
            DataRowView dataRow = (DataRowView)(comboBoxJobNumber.SelectedItem);
            if (textBoxPwd.Text == dataRow["password"].ToString())
            {
                MainForm.currPersonId = Convert.ToInt32(dataRow["id"].ToString());
            }
        }

        private void comboBoxJobNumber_SelectedIndexChanged(object sender, EventArgs e)
        {
            DataRowView dataRow = (DataRowView)(comboBoxJobNumber.SelectedItem);
            textBoxName.Text = dataRow["name"].ToString();
        }
    }
}