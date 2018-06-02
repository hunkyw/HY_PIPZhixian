using System;
using System.Drawing;
using System.Windows.Forms;

namespace HY_PIP
{
    public class TransparentPanel : Control
    {
        public TransparentPanel()
        {
            this.Click += new System.EventHandler(this.TransparentPanel_Click);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            //不进行背景的绘制
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000020; //WS_EX_TRANSPARENT
                return cp;
            }
        }

        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            //绘制panel的背景图像
            Rectangle rec = new Rectangle(0, 0, this.BackgroundImage.Size.Width, this.BackgroundImage.Size.Height);
            if (BackgroundImage != null) e.Graphics.DrawImage(this.BackgroundImage, rec);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            //
            // TransparentPanel
            //
            this.ResumeLayout(false);
        }

        private void TransparentPanel_Click(object sender, EventArgs e)
        {
            m_click(sender, e);
        }

        public delegate void GeneralClick(object sender, EventArgs e);

        public static GeneralClick m_click;

        ////为控件添加自定义属性值num1
        //private int num1 = 1;

        //[Bindable(true), Category("自定义属性栏"), DefaultValue(1), Description("此处为自定义属性Attr1的说明信息！")]
        //public int Attr1
        //{
        //    get { return num1; }
        //    set { this.Invalidate(); }
        //}
    }
}