using System;
using System.Drawing;
using System.Windows.Forms;

namespace HY_PIP
{
    public partial class SequentialCtrl : UserControl
    {
        public hyWorkGroup workGroup;
        private int middle_width;

        private static uint[] rgb_arr = new uint[]{
            0xFF43A102,0xFFA2B700,0xFFFE9D01,0xFF712704,
            0xFFBD7803,0xFF04477C,0xFF065FB9,0xFF049FF1,
            0xFF1291A9,0xFFFF6600,0xFF036803,0xFFDA891E,
            0xFFF6BF1C,0xFF74A474,0xFF00CCFF,0xFF4C4C4C,
            0xFF990066,0xFFFFCC00,0xFFCC0033,0xFF333399,
            0xFF99CC00,0xFF003399,0xFF009999,0xFF336699,
            0xFFCC3399,0xFF99CC66,0xFFFF33CC,0xFF66CC99};

        public SequentialCtrl()
        {
            InitializeComponent();
        }

        private void SequentialCtrl_Load(object sender, EventArgs e)
        {
            timer1.Start();
            pictureBoxTimeLine.Visible = false;
            this.HorizontalScroll.Maximum = 1;
        }

        /**
         *
         * 更新  workGroup 内容。
         *
         * workGroup中记录了所有的工作流信息。
         *
         * */

        public void UpdateSequentialChart()
        {
            // 绘制工艺时序图形
            int colorIndex = 0;
            int stationIndex = 0;
            foreach (hyWorkFlow wf in workGroup.workFlowList)
            {// 工作流
                // 工作流色彩
                colorIndex++;
                if (colorIndex >= rgb_arr.Length) colorIndex = 0;// 颜色索引，不同的工作流采用不同的颜色表示。

                stationIndex = 0;
                foreach (hyStationPara stationPara in wf.process.stationParaList)
                {// 工位
                    // 标签的色彩
                    Label label = stationPara.label;
                    label.Enabled = false;
                    label.AutoSize = false;
                    label.ForeColor = Color.White;      // 前景色
                    label.BackColor = Color.FromArgb((int)rgb_arr[colorIndex]); // 背景色
                    label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;//文字对齐

                    // 标签的位置
                    int index = stationIndex;
                    if (stationIndex > 9) { index += 1; }
                    int h = (int)(36.7f * index) + 25;// 行间距：37，起始高度：25
                    int height = 33;// 标签高度
                    label.Location = new System.Drawing.Point(stationPara.startingTime - this.HorizontalScroll.Value, h);
                    label.Size = new System.Drawing.Size(stationPara.workingTime, height);
                    // 标签显示文本
                    //label.Text = (stationPara.station_id + 1).ToString() + ",载具" + wf.carrier.name.ToString();
                    label.Text = "夹具" + wf.carrier.name.ToString();

                    // 添加标签到控件
                    if (this.Controls.IndexOf(label) < 0)
                    {
                        this.Controls.Add(label);
                    }

                    stationIndex++;
                }
            }
        }

        private void SequentialCtrl_Scroll(object sender, ScrollEventArgs e)
        {
        }

        private void SequentialCtrl_MouseDown(object sender, MouseEventArgs e)
        {
            pictureBoxMonitorLine.Visible = true;
            pictureBoxMonitorLine.Location = new System.Drawing.Point(e.X, timeLineZpos);

            labelMonitor.Visible = true;
            labelMonitor.Location = pictureBoxMonitorLine.Location;

            labelMonitor.Text = (e.X + this.HorizontalScroll.Value).ToString();// 加上滚动条偏移量后的时间值。

            timer1.Stop();
            timer1.Interval = 5000;
            timer1.Start();
        }

        private int timeLineZpos = 24;

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Interval = 500;
            int MaxScrollValue = 0;
            if (workGroup != null)
            {
                middle_width = this.ClientSize.Width / 2;
                pictureBoxTimeLine.Visible = true;
                if (MainForm.SystemMinutes < middle_width)
                {// 前半段
                    this.HorizontalScroll.Value = 0;
                    pictureBoxTimeLine.Location = new System.Drawing.Point(MainForm.SystemMinutes - this.HorizontalScroll.Value, timeLineZpos);
                }
                else
                {
                    if (this.HorizontalScroll.Maximum < this.ClientSize.Width)
                    {// 正常情况下，没有ScrollBar时，Maximum = 100.一旦有ScrollBar时，Maximum表示的是整个内容的宽度，这个宽度大于等于ClientSize.宽度的。
                        MaxScrollValue = 0;//表示没有滚动条
                    }
                    else
                    {
                        MaxScrollValue = this.HorizontalScroll.Maximum - this.ClientSize.Width;
                    }

                    if (MainForm.SystemMinutes < middle_width + MaxScrollValue)
                    {// 中间段
                        this.HorizontalScroll.Value = MainForm.SystemMinutes - middle_width;
                        pictureBoxTimeLine.Location = new System.Drawing.Point(middle_width, timeLineZpos);
                    }
                    else
                    {// 后半段
                        this.HorizontalScroll.Value = MaxScrollValue;// 这句要放在前面，主要是因为pictureBoxTimeLine到达最末端时，会增加ScrollBar的长度。
                        pictureBoxTimeLine.Location = new System.Drawing.Point(MainForm.SystemMinutes - MaxScrollValue, timeLineZpos);// 这句要放在后面
                    }
                }
            }
        }
    }
}