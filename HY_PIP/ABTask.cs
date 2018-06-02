namespace HY_PIP
{
    public class ABTask
    {
        public hyWorkFlow workFlow;// 引用类
        public hyCarrier carrier;// 引用类 (save)

        public enum STATUS { IDLE, PREA, CLAMPA, PREB, RELAXB, AFTERB, STOP };

        public STATUS taskStatus = STATUS.IDLE;             // 任务状态 (save)
        public int AB_TASK_A = hyProcess.stationNum + 0;    // 任务A点 (save)
        public int AB_TASK_B;                               // 任务B点 (save)

        public static int xSpeed = 200;
        public static int zSpeed = 40;

        public static ServoPoint[] loadPoints;// 共 n 个装载点
        public static ServoPoint[] logicPoints;
        public static ServoPoint zeroPos = new ServoPoint(0, 0);// 初始装载点的正上方

        public ABTask(hyWorkFlow wf)
        {
            this.workFlow = wf;
        }

        public static void Init()
        {
            if (loadPoints == null)
            {
                // -----------------------------------------------------------------------------
                //
                //  实际装载点位
                //
                loadPoints = new ServoPoint[hyProcess.loadPointsNum];
                logicPoints = new ServoPoint[hyProcess.loadPointsNum];
                logicPoints[0] = loadPoints[hyWorkFlow.POS_LOAD] = new ServoPoint(0, 1360);// 装载点

                logicPoints[1] = loadPoints[0] = new ServoPoint(2795, 2810);
                logicPoints[2] = loadPoints[1] = new ServoPoint(4215, 2810);
                logicPoints[3] = loadPoints[2] = new ServoPoint(5560, 2810);

                logicPoints[4] = loadPoints[3] = new ServoPoint(7250, 2670);
                logicPoints[5] = loadPoints[4] = new ServoPoint(9130, 2685);
                logicPoints[6] = loadPoints[5] = new ServoPoint(10920, 2720);
                logicPoints[7] = loadPoints[6] = new ServoPoint(12765, 2700);

                logicPoints[8] = loadPoints[7] = new ServoPoint(14460, 2805);
                //logicPoints[8] = loadPoints[7] = new ServoPoint(17120, 2820);//第6号水槽作为第4个位置

                logicPoints[9] = loadPoints[8] = new ServoPoint(15790, 2810);

                //logicPoints[10] = loadPoints[9] = new ServoPoint(14460, 2805);//第4号水槽作为第6个位置
                logicPoints[10] = loadPoints[9] = new ServoPoint(17120, 2820);

                logicPoints[11] = loadPoints[hyWorkFlow.POS_IDLE] = new ServoPoint(1100, 100);// 空地
                //暂未标定12 13 14，且12号变为空冷，不要除渣炉
                logicPoints[12] = loadPoints[10] = new ServoPoint(18260, 1365);


                logicPoints[13] = loadPoints[11] = new ServoPoint(24728, 500);
                logicPoints[14] = loadPoints[12] = new ServoPoint(26878, 500);


                logicPoints[15] = loadPoints[hyWorkFlow.POS_UNLOAD] = new ServoPoint(18260, 1365);//空地卸载点
                //logicPoints[15] = loadPoints[hyWorkFlow.POS_UNLOAD] = new ServoPoint(28150, 1370);// 标准卸载点
                //loadPoints[hyWorkFlow.POS_ZERO] = zeroPos;// 零点
            }
        }

        private void A_TO_B(int a, int b)
        {
            return;
            /*
            CLAMP_RELAX_DO();   // 释放夹爪
            Z_TO(true, 100, 50);       // 上点

            // 进入 A 点区域
            X_TO(true, -200, 50);     // 平移 --> A
            Z_TO(true, -100, 50);     // 下点
            CLAMP_DO();         // 抓取
            Z_TO(true, 100, 50);      // 上点

            // 进入 B 点区域
            X_TO(true, 200, 50);      // 平移 --> B
            Z_TO(true, -100, 50);     // 下点
             * */
        }

        public static bool Z_TO(bool absolute, int zPos, int zSpeed)
        {
            if (GenericOp.EStop || !GenericOp.AutoMode)
            {
                return false;// 若果按下急停或进入手动模式，返回false，代表 X_TO 命令没有执行完毕。
            }
            int zbase = absolute ? 0 : GenericOp.zPos;
            SerialWireless.gtryCmd = absolute ? SerialWireless.GTYRY_CMD.Z_ABSOLUTE : SerialWireless.GTYRY_CMD.Z_RELATIVE;
            SerialWireless.gtryPos = zPos;
            SerialWireless.gtrySpeed = zSpeed;// 50mm/s的速度。

            if (zPos != (GenericOp.zPos - zbase))
            {
                // 等待，直到命令结束
                return false;
            }
            return true;//
        }

        public static bool X_TO(bool absolute, int xPos, int xSpeed)
        {
            if (GenericOp.EStop || !GenericOp.AutoMode)
            {
                return false;// 若果按下急停或进入手动模式，返回false，代表 X_TO 命令没有执行完毕。
            }

            int xbase = absolute ? 0 : GenericOp.xPos;
            SerialWireless.gtryCmd = absolute ? SerialWireless.GTYRY_CMD.X_ABSOLUTE : SerialWireless.GTYRY_CMD.X_RELATIVE;
            SerialWireless.gtryPos = xPos;
            SerialWireless.gtrySpeed = xSpeed;// 100mm/s的速度。

            if (xPos != (GenericOp.xPos - xbase))
            {
                // 等待，直到命令结束
                return false;
            }

            return true;
        }

        // ----------------------------------------------------------
        // 手动绝对定位操作命令
        public static bool Z_TO_MANUAL(bool absolute, int zPos, int zSpeed)
        {
            if (GenericOp.EStop || GenericOp.AutoMode)
            {
                return true;// 若果按下急停或进入自动模式，返回true，结束命令操作。
            }
            int zbase = absolute ? 0 : GenericOp.zPos;
            SerialWireless.gtryCmd = absolute ? SerialWireless.GTYRY_CMD.Z_ABSOLUTE : SerialWireless.GTYRY_CMD.Z_RELATIVE;
            SerialWireless.gtryPos = zPos;
            SerialWireless.gtrySpeed = zSpeed;// 50mm/s的速度。

            if (zPos != (GenericOp.zPos - zbase))
            {
                // 等待，直到命令结束
                return false;
            }
            return true;//
        }

        // ----------------------------------------------------------
        // 手动绝对定位操作命令
        public static bool X_TO_MANUAL(bool absolute, int xPos, int xSpeed)
        {
            if (GenericOp.EStop || GenericOp.AutoMode)
            {
                return true;// 若果按下急停或进入自动模式，返回true，结束命令操作。
            }

            int xbase = absolute ? 0 : GenericOp.xPos;
            SerialWireless.gtryCmd = absolute ? SerialWireless.GTYRY_CMD.X_ABSOLUTE : SerialWireless.GTYRY_CMD.X_RELATIVE;
            SerialWireless.gtryPos = xPos;
            SerialWireless.gtrySpeed = xSpeed;// 100mm/s的速度。

            if (xPos != (GenericOp.xPos - xbase))
            {
                // 等待，直到命令结束
                return false;
            }

            return true;
        }

        public static bool CLAMP_DO()
        {
            if (GenericOp.EStop)
            {
                return false;
            }
            // 重要步骤：上升到最高点（H点）
            SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.CLAMP;
            if (!SerialWireless.GetGtryState(SerialWireless.GTRY_STATE_CLAMP))
            {
                // 等待，直到命令结束
                return false;
            }

            return true;
        }

        public static bool CLAMP_RELAX_DO()
        {
            if (GenericOp.EStop || !GenericOp.AutoMode)
            {
                return false;
            }

            // ---------------------------------------
            //  TODO:   松开夹爪时，一定要判定有无夹具。最好是显示屏给出提示，确认操作。
            //

            // 重要步骤：上升到最高点（H点）
            SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.CLAMP_RELAX;
            if (!SerialWireless.GetGtryState(SerialWireless.GTRY_STATE_CLAMP_RELAX))
            {
                // 等待，直到命令结束
                return false;
            }
            return true;
        }
    }
}