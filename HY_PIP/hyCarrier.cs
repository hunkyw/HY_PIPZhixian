namespace HY_PIP
{
    public class hyCarrier
    {
        public hyWorkFlow workFlow;// 上级类

        public int id = 0;                      // 夹具号
        public int startingTime;                // 夹具停留段（开始时间）
        public int endingTime;                  // 夹具停留段（结束时间）
        public int name;                        // 夹具名称

        public enum STATUS { IN_STATION, REQUEST_CHANGE_OVER, DOING_CHANGE_OVER, OVER };

        public STATUS status = STATUS.IN_STATION;

        public bool isClamped; // 是否处于被夹持状态
        public int remainingTime;// 剩余时间

        private int _pos;

        public int pos
        {
            set { currPosDraw = new ServoPoint(LayoutDrawing.loadPoints[value].X, LayoutDrawing.loadPoints[value].Z); _pos = value; }
            get { return _pos; }
        }

        public ServoPoint currPosDraw;// 当前位置
        public ServoPoint currPosReal;// 当前位置

        public bool currLoadPointIndex;// 当前落脚点

        public hyCarrier(hyWorkFlow wf)
        {
            this.workFlow = wf;
            pos = hyWorkFlow.POS_LOAD;
            currPosDraw = new ServoPoint(LayoutDrawing.loadPoints[hyWorkFlow.POS_LOAD].X, LayoutDrawing.loadPoints[hyWorkFlow.POS_LOAD].Z);
            currPosReal = new ServoPoint(ABTask.loadPoints[hyWorkFlow.POS_LOAD].X, ABTask.loadPoints[hyWorkFlow.POS_LOAD].Z);
        }

        /*
         *
         *   更新夹具的位置，时间段（开始时间，停止时间）
         *
         */

        public void UpdateCarrierInfo(int currPos)
        {
            this.pos = currPos;// 更新夹具位置
            if (currPos == hyWorkFlow.POS_UNLOAD)
            {
                status = STATUS.OVER;// 如果当前位置在卸载位置，那么就将状态切换为“结束”状态。
            }
            else if (currPos == hyWorkFlow.POS_LOAD)
            {
                status = STATUS.IN_STATION;
                int nextPos = this.workFlow.NextCarrierPos();
                endingTime = this.workFlow.process.stationParaList[nextPos].startingTimeWithHead;// 更新夹具时间 夹具停留段（结束时间）
            }
            else
            {
                status = STATUS.IN_STATION;
                startingTime = this.workFlow.process.stationParaList[currPos].startingTimeWithHead;// 更新夹具时间 夹具停留段（开始时间）
                endingTime = this.workFlow.process.stationParaList[currPos].endingTime;// 更新夹具时间 夹具停留段（结束时间）
            }
        }
    }
}