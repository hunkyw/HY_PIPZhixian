using System.Drawing;
using System.Windows.Forms;

namespace HY_PIP
{
    public partial class TabControlEx : TabControl
    {
        //Image backImage;
        public TabControlEx()
        {
            base.SetStyle(
             ControlStyles.UserPaint |                      // 控件将自行绘制，而不是通过操作系统来绘制
             ControlStyles.OptimizedDoubleBuffer |          // 该控件首先在缓冲区中绘制，而不是直接绘制到屏幕上，这样可以减少闪烁
             ControlStyles.AllPaintingInWmPaint |           // 控件将忽略 WM_ERASEBKGND 窗口消息以减少闪烁
             ControlStyles.ResizeRedraw |                   // 在调整控件大小时重绘控件
             ControlStyles.SupportsTransparentBackColor,    // 控件接受 alpha 组件小于 255 的 BackColor 以模拟透明
             true);                                         // 设置以上值为 true
            base.UpdateStyles();

            this.SizeMode = TabSizeMode.Fixed;  // 大小模式为固定
            //this.ItemSize = new Size(255, 79);   // 设定每个标签的尺寸

            //backImage = new Bitmap("E:\\stm32FreeRTOS\\IMU_Config\\IMU_Config\\resource\\tabbg.bmp");   // 从资源文件（嵌入到程序集）里读取图片
            //backImage = new Bitmap(Properties.Resources.tabbg);   // 从资源文件（嵌入到程序集）里读取图片  ,tabbg.bmp

            InitializeComponent();
        }

        private Rectangle mainRec;

        protected override void OnPaint(PaintEventArgs e)
        {
            mainRec = this.ClientRectangle;
            SolidBrush brush = new SolidBrush(MainForm.sysBackColor);
            e.Graphics.FillRectangle(brush, mainRec);

            for (int i = 0; i < this.TabCount; i++)
            {
                //e.Graphics.DrawRectangle(Pens.Red, this.GetTabRect(i));// 画红色轮廓
                if (this.SelectedIndex == i)
                {// 对选中的标签，进行处理
                    //e.Graphics.DrawImage(backImage, this.GetTabRect(i));
                }

                // Calculate text position
                Rectangle bounds = this.GetTabRect(i);
                PointF textPoint = new PointF();
                SizeF textSize = TextRenderer.MeasureText(this.TabPages[i].Text, this.Font);

                // 注意要加上每个标签的左偏移量X
                textPoint.X
                    = bounds.X + (bounds.Width - textSize.Width) / 2;
                textPoint.Y
                    = bounds.Bottom - textSize.Height - this.Padding.Y;

                // Draw highlights
                e.Graphics.DrawString(
                    this.TabPages[i].Text,
                    this.Font,
                    SystemBrushes.ControlLightLight,    // 高光颜色
                    textPoint.X,
                    textPoint.Y);

                // 绘制正常文字
                textPoint.Y--;
                e.Graphics.DrawString(
                    this.TabPages[i].Text,
                    this.Font,
                    SystemBrushes.ControlText,    // 正常颜色
                    textPoint.X,
                    textPoint.Y);

                // 绘制图标
                if (this.ImageList != null)
                {
                    int index = this.TabPages[i].ImageIndex;
                    string key = this.TabPages[i].ImageKey;
                    Image icon = new Bitmap(32, 32);

                    if (index > -1)
                    {
                        icon = this.ImageList.Images[index];
                    }
                    if (!string.IsNullOrEmpty(key))
                    {
                        icon = this.ImageList.Images[key];
                    }
                    e.Graphics.DrawImage(
                        icon,
                        bounds.X + (bounds.Width - icon.Width) / 2,
                        bounds.Top + this.Padding.Y);
                }
            }

            // 打开了文字抗锯齿
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        }
    }
}