using System.Collections.Generic;

namespace HY_PIP
{
    /**
     *
     * 工艺 Process
     *
     * 层次关系：
     *          ProcessList工艺列表，包含许多Process工艺
     *          每一条Process工艺，包含13个水槽，炉子StationPara 工位参数的定义
     *          Station 工位参数，包含了工位温度，工作时间等的定义。
     *
     *  ProcessList --> Process --> StationPara
     * */

    public class hyProcess
    {
        public const int stationNum = 13;
        public const int loadPointsNum = stationNum + 3;
        public const int interval_l = 10;   // 10 分钟的间隔时间
        public const int interval_m = 5;  // 5 分钟的间隔时间
        public const int interval_s = 2; // 2 分钟的间隔时间

        public int process_id;      // 工艺ID
        public string process_name; // 工艺名

        public List<hyStationPara> stationParaList;

        public hyProcess()
        {
            /*
             * 创建新的工艺
             *          新的工艺，需要对每个水槽，炉子设定工作温度、工作时间、配方等信息。
             *
             */
            stationParaList = new List<hyStationPara>();
            for (int i = 0; i < stationNum; i++)
            {
                hyStationPara stationPara = new hyStationPara();

                stationPara.station_id = i;// station id 从 0 - 12 排序
                
                stationParaList.Add(stationPara);
            }
        }
    }
}