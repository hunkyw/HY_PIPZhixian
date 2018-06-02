namespace HY_PIP
{
    partial class SequentialCtrl
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.labelMonitor = new System.Windows.Forms.Label();
            this.pictureBoxTimeLine = new System.Windows.Forms.PictureBox();
            this.pictureBoxMonitorLine = new System.Windows.Forms.PictureBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxTimeLine)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxMonitorLine)).BeginInit();
            this.SuspendLayout();
            // 
            // labelMonitor
            // 
            this.labelMonitor.AutoSize = true;
            this.labelMonitor.Location = new System.Drawing.Point(372, 4);
            this.labelMonitor.Name = "labelMonitor";
            this.labelMonitor.Size = new System.Drawing.Size(41, 12);
            this.labelMonitor.TabIndex = 6;
            this.labelMonitor.Text = "label1";
            this.labelMonitor.Visible = false;
            // 
            // pictureBoxTimeLine
            // 
            this.pictureBoxTimeLine.BackColor = System.Drawing.SystemColors.Highlight;
            this.pictureBoxTimeLine.Location = new System.Drawing.Point(130, 20);
            this.pictureBoxTimeLine.Name = "pictureBoxTimeLine";
            this.pictureBoxTimeLine.Size = new System.Drawing.Size(2, 510);
            this.pictureBoxTimeLine.TabIndex = 4;
            this.pictureBoxTimeLine.TabStop = false;
            // 
            // pictureBoxMonitorLine
            // 
            this.pictureBoxMonitorLine.BackColor = System.Drawing.SystemColors.ScrollBar;
            this.pictureBoxMonitorLine.Location = new System.Drawing.Point(363, 20);
            this.pictureBoxMonitorLine.Name = "pictureBoxMonitorLine";
            this.pictureBoxMonitorLine.Size = new System.Drawing.Size(2, 510);
            this.pictureBoxMonitorLine.TabIndex = 5;
            this.pictureBoxMonitorLine.TabStop = false;
            this.pictureBoxMonitorLine.Visible = false;
            // 
            // timer1
            // 
            this.timer1.Interval = 500;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // SequentialCtrl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.Transparent;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.pictureBoxTimeLine);
            this.Controls.Add(this.pictureBoxMonitorLine);
            this.Controls.Add(this.labelMonitor);
            this.Name = "SequentialCtrl";
            this.Size = new System.Drawing.Size(694, 331);
            this.Load += new System.EventHandler(this.SequentialCtrl_Load);
            this.Scroll += new System.Windows.Forms.ScrollEventHandler(this.SequentialCtrl_Scroll);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.SequentialCtrl_MouseDown);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxTimeLine)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxMonitorLine)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBoxTimeLine;
        private System.Windows.Forms.PictureBox pictureBoxMonitorLine;
        private System.Windows.Forms.Label labelMonitor;
        private System.Windows.Forms.Timer timer1;
    }
}
