using System;
using System.Windows.Forms;

namespace HY_PIP
{
    public partial class ProcessPanelCtrl : UserControl
    {
        public hyWorkGroup workGroup// 工作组(平齐类)
        {
            get { return sequentialCtrl1.workGroup; }
            set { sequentialCtrl1.workGroup = value; }
        }

        public SequentialCtrl SeqCtrl
        {
            get { return sequentialCtrl1; }
        }

        public ProcessPanelCtrl()
        {
            InitializeComponent();
        }

        private void ProcessPanelCtrl_Load(object sender, EventArgs e)
        {
            // 尺寸设置
            pictureBoxLeftHeader.Width = pictureBoxLeftHeader.Image.Width;// 由于不同显示屏dpi的不同，导致尺寸出现偏差。这里强行设置pictureBox宽度与image宽度相等。看似多余，实则用处很大。
            pictureBoxLeftHeader.Top = 1;

            // ProcessPanelCtrl的总尺寸为：1412*540，如果加上下面的水平滚动条，尺寸则设置为1412*554
            this.Width = 1412;
            this.Height = 560;
            sequentialCtrl1.Width = this.Width - pictureBoxLeftHeader.Width;// 强行设置sequentialCtrl1是另外一半。

            sequentialCtrl1.HorizontalScroll.Visible = true;
        }

        public void UpdateSequentialChart()
        {
            sequentialCtrl1.UpdateSequentialChart();
        }
    }
}