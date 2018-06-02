using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace HY_PIP
{
    public partial class NewProcessForm : Form
    {
        private string[] dataGridViewTitle = new string[] { "1#前清洗", "2#前清洗", "3#前清洗", "4#预热", "5#氮碳钇渗入", "6#离子活化", "7#阳离子渗入", "8#后清洗", "9#后清洗", "10#后清洗", "11#空冷", "12#稳定炉", "13#油炉" };

        public hyProcessGroup processGroup;
        private DataTable dt;

        private class ProcessLabel
        {
            public TextBox workTimeLabel;
            public TextBox workTemperatureLabel;
            public TextBox formulaLabel;

            public ProcessLabel()
            {
                workTimeLabel = new TextBox();
                workTimeLabel.Width = 80;
                workTimeLabel.Font = new Font("微软雅黑", 12, workTimeLabel.Font.Style);
                workTimeLabel.BorderStyle = BorderStyle.None;

                workTemperatureLabel = new TextBox();
                workTemperatureLabel.Width = 80;
                workTemperatureLabel.Font = new Font("微软雅黑", 12, workTimeLabel.Font.Style);
                workTemperatureLabel.BorderStyle = BorderStyle.None;

                formulaLabel = new TextBox();
                formulaLabel.Width = 80;
                formulaLabel.Font = new Font("微软雅黑", 12, workTimeLabel.Font.Style);
                formulaLabel.BorderStyle = BorderStyle.None;
            }
        }

        private List<ProcessLabel> labelList = new List<ProcessLabel>();

        public NewProcessForm()
        {
            InitializeComponent();
        }

        private void NewProcessForm_Load(object sender, EventArgs e)
        {
            this.BackColor = MainForm.sysBackColor;// 背景色

            //dataGridView1的初始化
            RowMergeViewInit();

            // Label数组阵列初始化
            LabelArrayInit();

            LayoutInit();
        }

        private void LayoutInit()
        {
            //float dpi = getLogPiex();
            //label1.Text = this.Width.ToString() + "," + this.Height.ToString() + "," + dpi.ToString();

            panelX.AutoScroll = MainFrame.SCREEN_AUTO_SCROLL;// 如果屏幕尺寸符合 1920*1080，就不用加滚动条。

            // 下面的布局总览图的尺寸设置
            panelX.Width = this.Width;
            panelX.Height = this.Height;

            rowMergeView1.Width = this.ClientSize.Width - 10;// 表格宽度
            rowMergeView1.Left = 0;
            rowMergeView1.Top = 0;

            panel2.Top = this.rowMergeView1.Height;
            panel2.Left = (this.Width - panel2.Width) / 2;
        }

        private void RowMergeViewInit()
        {
            dt = new DataTable();

            // 添加第二级表头
            dt.Columns.Add("工艺号");
            for (int i = 0; i < dataGridViewTitle.Length; i++)
            {
                dt.Columns.Add((i * 2).ToString());
                dt.Columns.Add((i * 2 + 1).ToString());
            }

            this.processGroup = MainForm.processGroup;
            // 添加行
            //dt.Rows.Add(new string[] { "工艺号"+i.ToString(),i.ToString("D4"), j.ToString("D4"), m.ToString("D4"), "1", "1", "1", "1", "1", "1", "1", "1", "1" });
            foreach (hyProcess process in processGroup.processList)
            {
                string[] strArr = new string[27];// 1 + 13*2 = 27
                strArr[0] = process.process_name;//process.process_id.ToString();
                for (int i = 0; i < hyProcess.stationNum; i++)
                {
                    strArr[i * 2 + 1] = process.stationParaList[i].workingTemp.ToString();// 工作温度
                    strArr[i * 2 + 2] = process.stationParaList[i].workingTime.ToString();// 工作时间
                }
                dt.Rows.Add(strArr);
            }

            this.rowMergeView1.DataSource = dt;
            this.rowMergeView1.ColumnHeadersHeight = 80;
            this.rowMergeView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            //this.rowMergeView1.MergeColumnNames.Add("Column1");

            rowMergeView1.Columns[0].Width = 150;
            // 添加第一级表头
            for (int i = 0; i < dataGridViewTitle.Length; i++)
            {
                this.rowMergeView1.AddSpanHeader(i * 2 + 1, 2, dataGridViewTitle[i]);
            }
            // 高度
            this.rowMergeView1.Height = this.rowMergeView1.ColumnHeadersHeight + this.rowMergeView1.RowTemplate.Height * 12;
        }

        private void LabelArrayInit()
        {
            //
            for (int i = 0; i < hyProcess.stationNum; i++)
            {
                ProcessLabel processLabel = new ProcessLabel();
                labelList.Add(processLabel);
                panel1.Controls.Add(processLabel.workTemperatureLabel);
                panel1.Controls.Add(processLabel.workTimeLabel);
                panel1.Controls.Add(processLabel.formulaLabel);
                processLabel.workTemperatureLabel.Text = "0";
                processLabel.workTimeLabel.Text = "0";
                processLabel.formulaLabel.Text = "0";

                int left_off = 420;
                int top_off = 50;
                if (i > 9) { top_off = 78; }
                int row_height = 37;
                // 左
                processLabel.workTemperatureLabel.Left = left_off;
                processLabel.workTimeLabel.Left = left_off + 117;
                processLabel.formulaLabel.Left = left_off + 230;
                // 上
                processLabel.workTemperatureLabel.Top = top_off + i * row_height;
                processLabel.workTimeLabel.Top = top_off + i * row_height;
                processLabel.formulaLabel.Top = top_off + i * row_height;
                processLabel.formulaLabel.Width = 160;
            }
            pictureBox1.SendToBack();// 将PictureBox调整到最底层。
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // 下一条工艺
            int i = rowMergeView1.SelectedRows[0].Index + 1;
            if (i >= rowMergeView1.RowCount) i = rowMergeView1.RowCount - 1;
            //rowMergeView1.Rows[i].Selected = true;
            rowMergeView1.CurrentCell = rowMergeView1.Rows[i].Cells[0]; // 设置当前行（焦点）
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // 上一条工艺
            int i = rowMergeView1.SelectedRows[0].Index - 1;
            if (i < 0) i = 0;
            //rowMergeView1.Rows[i].Selected = true;
            rowMergeView1.CurrentCell = rowMergeView1.Rows[i].Cells[0]; // 设置当前行（焦点）
        }

        private void rowMergeView1_SelectionChanged(object sender, EventArgs e)
        {
            if (rowMergeView1.SelectedRows.Count > 0)
            {
                //textBoxS1Formula.Text = rowMergeView1.SelectedRows[0].Cells["工艺号"].Value.ToString();
                //textBoxS1Time.Text = rowMergeView1.CurrentCell.RowIndex.ToString();;
                //textBoxS1Time.Text = rowMergeView1.SelectedRows[0].Index.ToString();

                PushDataToTextBox();// 往 TextBox 中推送数据
            }
        }

        private void PushDataToTextBox()
        {
            // dataGridView 排序后会引起索引变化，那么如何找到该索引呢，使用下面的方法。
            DataRowView drv = rowMergeView1.SelectedRows[0].DataBoundItem as DataRowView;
            int index = dt.Rows.IndexOf(drv.Row);
            if (deleteFlag)
            {
                deleteFlag = false;
                index--;//// 特殊处理。需要增加一个deleteFlag，观察发现，每次删除后，选中的行的内容有错位。
                if (index < 0) index = 0;
            }

            if (labelList.Count <= 0) return;
            for (int i = 0; i < hyProcess.stationNum; i++)
            {
                labelList[i].workTemperatureLabel.Text = processGroup.processList[index].stationParaList[i].workingTemp.ToString();
                labelList[i].workTimeLabel.Text = processGroup.processList[index].stationParaList[i].workingTime.ToString();
                labelList[i].formulaLabel.Text = processGroup.processList[index].stationParaList[i].formula.ToString();
            }

            textBoxProcessName.Text = processGroup.processList[index].process_name;// 工艺名称
        }

        private void PullDataFromTextBox(hyProcess process)
        {
            if (labelList.Count <= 0) return;
            for (int i = 0; i < hyProcess.stationNum; i++)
            {
                process.stationParaList[i].workingTemp = Convert.ToInt32(labelList[i].workTemperatureLabel.Text);
                process.stationParaList[i].workingTime = Convert.ToInt32(labelList[i].workTimeLabel.Text);
                process.stationParaList[i].formula = labelList[i].formulaLabel.Text;
            }

            process.process_name = textBoxProcessName.Text;

            return;
        }

        private void buttonNewProcess_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定新增工艺吗？", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.Cancel)
            {
                return;
            }
            try
            {
                // 新增工艺
                // 将新增的 process 添加到 XML 文件中
                hyProcess process = new hyProcess();

                PullDataFromTextBox(process);// 从 TextBox 中抽取数据

                processGroup.NewProcess(process);// XML 文件会保存，同时 processGroup的列表会增加

                // 更新 dataGridView
                // 只需要更新数据源 DataSource 就可以了。也就是 dt.
                string[] strArr = new string[27];// 1 + 13*2 = 27
                strArr[0] = process.process_name;// process.process_id.ToString();
                for (int i = 0; i < hyProcess.stationNum; i++)
                {
                    strArr[i * 2 + 1] = process.stationParaList[i].workingTemp.ToString();// 工作温度
                    strArr[i * 2 + 2] = process.stationParaList[i].workingTime.ToString();// 工作时间
                }
                dt.Rows.Add(strArr);

                // 跳转选择新增的那一行
                // dataGridView 排序后会引起索引变化，那么如何找到该索引呢，使用下面的方法。
                for (int i = 0; i < rowMergeView1.Rows.Count; i++)
                {
                    if (rowMergeView1.Rows[i].Cells[0].Value.ToString() == strArr[0])
                    {
                        rowMergeView1.CurrentCell = rowMergeView1.Rows[i].Cells[0]; // 设置当前行（焦点）
                    }
                }
            }
            catch (Exception e1)
            {
                MessageBox.Show("工作时间和工作温度必须是整数，不能为其他字符");
            }
            finally
            {
            }
        }

        private bool deleteFlag = false;

        private void buttonDeleteProcess_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定要删除当前工艺吗？", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.Cancel)
            {
                return;
            }
            // 删除工艺

            // 删除 dataGridView 中的工艺
            // dataGridView 排序后会引起索引变化，那么如何找到该索引呢，使用下面的方法。
            DataRowView drv = rowMergeView1.SelectedRows[0].DataBoundItem as DataRowView;
            int index = dt.Rows.IndexOf(drv.Row);

            // 第一步：删除 XML 文件内容，同时删除 processGroup 中的工艺列表 processList
            processGroup.DeleteProcess(index, processGroup.processList[index].process_id);
            // 第二步：删除 dataGridView 该行数据。
            deleteFlag = true;// 特殊处理。需要增加一个deleteFlag，观察发现，每次删除后，选中的行的内容有错位。
            dt.Rows[index].Delete();
        }

        private void buttonUpdateProcess_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定更新当前工艺数据吗？", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.Cancel)
            {
                return;
            }
            // 修改 dataGridView 中的数据
            try
            {
                // dataGridView 排序后会引起索引变化，那么如何找到该索引呢，使用下面的方法。
                DataRowView drv = rowMergeView1.SelectedRows[0].DataBoundItem as DataRowView;
                int index = dt.Rows.IndexOf(drv.Row);

                // 新增工艺
                // 将新增的 process 添加到 XML 文件中
                hyProcess process = new hyProcess();

                PullDataFromTextBox(process);// 从 TextBox 中抽取数据
                process.process_id = processGroup.processList[index].process_id;

                processGroup.UpdateProcess(index, process);// XML 文件会保存，同时 processGroup 的列表会增加

                // 更新 dataGridView
                // 只需要更新数据源 DataSource 就可以了。也就是 dt.
                for (int i = 0; i < hyProcess.stationNum; i++)// 1 + 13*2 = 27
                {
                    dt.Rows[index][i * 2 + 1] = process.stationParaList[i].workingTemp.ToString();// 工作温度
                    dt.Rows[index][i * 2 + 2] = process.stationParaList[i].workingTime.ToString();// 工作时间
                }
            }
            catch (Exception e1)
            {
                MessageBox.Show("工作时间和工作温度必须是整数，不能为其他字符");
            }
            finally
            {
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            MainFrame.personForm.StartPosition = FormStartPosition.CenterScreen; //居中显示
            MainFrame.personForm.ShowDialog(this);
        }

        private void pictureBoxProcessExport_Click(object sender, EventArgs e)
        {
            // 导出工艺文件
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "xml files (*.xml)|*.xml";
            dlg.FileName = "hyProcess.xml";
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists("hyProcess.xml"))
                {
                    File.Delete(dlg.FileName);
                    File.Copy("hyProcess.xml", dlg.FileName);
                    MessageBox.Show("导出成功！");
                }
            }
        }

        private void pictureBoxProcessImport_Click(object sender, EventArgs e)
        {
            // 导入工艺文件
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "xml files (*.xml)|*.xml";
            dlg.FileName = "hyProcess.xml";
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                File.Delete("hyProcess.xml");
                File.Copy(dlg.FileName, "hyProcess.xml");
                MessageBox.Show("导入成功！");
            }
        }
    }
}