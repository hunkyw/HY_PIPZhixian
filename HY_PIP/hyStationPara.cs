using System.Windows.Forms;

namespace HY_PIP
{
    /**
     *
     * 工位参数 Station Para
     *
     * 层次关系：
     *          ProcessList工艺列表，包含许多Process工艺
     *          每一条Process工艺，包含13个水槽，炉子StationPara 工位参数的定义
     *          Station 工位参数，包含了工位温度，工作时间等的定义。
     *
     * ProcessList --> Process --> StationPara
     * */

    public class hyStationPara
    {
        public int station_id = 0;
        public int workingTime = 0;  // 工作时间
        public int workingTemp = 0;  // 工作温度
        public string formula = "";  // 配方

        public int endingTime;  // 结束时间
        public int startingTimeWithHead;// 开始时间

        public int startingTime
        {
            get { return startingTimeWithHead + hyProcess.interval_m; }
            set { startingTimeWithHead = value - hyProcess.interval_m; }
        }

        public int workingTimeWithHead { get { if (enabled) return workingTime + hyProcess.interval_m; else return 0; } }

        public bool enabled { get { return workingTime != 0; } }

        public Label label = new Label();//
    }
}