namespace HY_PIP
{
    /**
     *
     * 工作流
     *
     * 层次关系：
     *          ProcessList工艺列表，包含许多Process工艺
     *          每一条Process工艺，包含13个水槽，炉子StationPara 工位参数的定义
     *          Station 工位参数，包含了工位温度，工作时间等的定义。
     *
     *  ProcessList --> Process --> StationPara
     * */

    public class hyWorkFlow
    {
        public hyWorkGroup workGroup;// 父类
        public hyCarrier carrier;    // 夹具（子类）

        // 每一个工作流，都包含关联一个工艺
        // 每个工艺中，都包含了13个station(炉子，水槽）的参数.station para
        public int workflow_id;      // 工作流ID

        public int person_id;        // 创建人员ID
        public int minimumPermitTime;   // 最小允许启动时间
        public int startingTime;        // 启动时间
        public int loading_station_id;// 取料工位ID

        public int process_id        // 工艺ID
        {
            // 工作流的工艺ID就是Process工艺的ID。
            get { return process.process_id; }
            set { process.process_id = value; }
        }

        public int carrier_id
        {       // 夹具ID
            get { return carrier.id; }
            set { carrier.id = value; }
        }

        public int carrier_pos
        {
            get { return carrier.pos; }
            set { carrier.pos = value; }
        }

        public hyProcess process;// 工艺
        public int max_workflow_endingtime = POS_LOAD;

        public const int POS_LOAD = hyProcess.stationNum;
        public const int POS_IDLE = hyProcess.stationNum + 1;
        public const int POS_UNLOAD = hyProcess.stationNum + 2;

        //public const int POS_ZERO = hyProcess.stationNum + 3;
        public const int POS_INVALID = -1;

        public const int POS_FIRST_STATION = 0;
        public const int POS_LAST_STATION = hyProcess.stationNum - 1;
        //public const int

        public hyWorkFlow(hyWorkGroup wgroup)
        {
            this.workGroup = wgroup;
            process = new hyProcess();// 新增一个Process工艺
            carrier = new hyCarrier(this);// 新增一个夹具。
            carrier.pos = POS_LOAD;
        }

        public void NewWorkFlow()
        {
            //MainForm.SystemMinutes;
        }

        /*
         *
         * 夹具的下一个位置应该放置在哪里
         *
         * */

        public int NextCarrierPos()
        {
            int next_carrier_pos = POS_UNLOAD;// 默认为到卸载点卸载，如果下面的程序找不到合适的夹具点，就直接到卸载点卸载。注意！！很重要。
            if (carrier_pos == POS_LOAD)
            {
                next_carrier_pos = POS_FIRST_STATION;
                for (int i = POS_FIRST_STATION; i < hyProcess.stationNum; i++)
                {
                    if (this.process.stationParaList[i].enabled)
                    {
                        next_carrier_pos = i;
                        break;
                    }
                }
            }
            else if (carrier_pos == POS_IDLE)
            {
                next_carrier_pos = POS_INVALID;
            }
            else if (carrier_pos == POS_UNLOAD)
            {
                next_carrier_pos = POS_INVALID;
            }
            else
            {
                for (int i = carrier_pos + 1; i < hyProcess.stationNum; i++)
                {
                    if (this.process.stationParaList[i].enabled)
                    {
                        next_carrier_pos = i;
                        break;
                    }
                }
            }

            return next_carrier_pos;
        }
    }
}