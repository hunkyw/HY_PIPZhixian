using HY_PIP;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Xml;

public class LayoutDrawing : DrawingArea
{
    public hyWorkGroup workGroup;// 工作组(平齐类)
    public static bool drawOnly = true;

    public int ii = 0;

    public int xpos;
    public int zpos;
    public int cxpos;
    public int czpos;

    //
    public static ServoPoint[] loadPoints = new ServoPoint[hyProcess.loadPointsNum];// 共 n 个装载点

    public static ServoPoint[] logicPoints = new ServoPoint[hyProcess.loadPointsNum];

    public List<hyCarrier> carrierList = new List<hyCarrier>();// 载具

    public List<ABTask> taskList = new List<ABTask>();// 任务列表
    public ABTask currTask;// 当前任务
    public hyCarrier currCarrier;

    private Thread threadAB;
    private Thread threadOpenStoveLid;
    private Thread threadCloseStoveLid;

    private ServoPoint destPosDraw;
    public ServoPoint currPosDraw;

    public ServoPoint zeroPos = new ServoPoint(0, 0);// 初始装载点的正上方

    // 炉盖动作指令
    public enum STOVE_LID_ACTION { NO_ACTION = 0, OPEN = 1, CLOSE = 2 };

    public static STOVE_LID_ACTION[] stoveLidAction = new STOVE_LID_ACTION[7];

    public LayoutDrawing()
    {
        // -----------------------------------------------------------------------------
        //
        //  动画的装载点位
        //
        logicPoints[0] = loadPoints[hyWorkFlow.POS_LOAD] = new ServoPoint(0, 100);// 装载点
        logicPoints[1] = loadPoints[0] = new ServoPoint(145, 171);
        logicPoints[2] = loadPoints[1] = new ServoPoint(213, 171);
        logicPoints[3] = loadPoints[2] = new ServoPoint(283, 171);

        logicPoints[4] = loadPoints[3] = new ServoPoint(370, 156);
        logicPoints[5] = loadPoints[4] = new ServoPoint(466, 156);
        logicPoints[6] = loadPoints[5] = new ServoPoint(562, 156);
        logicPoints[7] = loadPoints[6] = new ServoPoint(650, 156);

        //logicPoints[8] = loadPoints[7] = new ServoPoint(880, 171);
        logicPoints[8] = loadPoints[7] = new ServoPoint(733, 171);
        logicPoints[9] = loadPoints[8] = new ServoPoint(806, 171);

        //logicPoints[10] = loadPoints[9] = new ServoPoint(733, 171);
        logicPoints[10] = loadPoints[9] = new ServoPoint(880, 171);


        logicPoints[12] = loadPoints[hyWorkFlow.POS_IDLE] = new ServoPoint(954, 156);// 空地
        logicPoints[11] = loadPoints[10] = new ServoPoint(964, 156);
        logicPoints[13] = loadPoints[11] = new ServoPoint(1093, 156);
        logicPoints[14] = loadPoints[12] = new ServoPoint(1188, 156);
        logicPoints[15] = loadPoints[hyWorkFlow.POS_UNLOAD] = new ServoPoint(1380, 100);// 卸载点
        //loadPoints[hyWorkFlow.POS_ZERO] = zeroPos;// 零点
        //
        destPosDraw = new ServoPoint(0, 0);
        currPosDraw = new ServoPoint(zeroPos.X, zeroPos.Z);
        // -----------------------------------------------------------------------------
        //
        //  任务线程
        //
        threadAB = new Thread(new ThreadStart(ThreadAB)); //也可简写为 new Thread(ThreadMethod);
        threadAB.Start(); //启动线程

        //
        threadOpenStoveLid = new Thread(new ThreadStart(ThreadOpenStoveLid));
        threadOpenStoveLid.Start();// 打开、关闭炉盖任务

        if (drawOnly)
        {// 模拟操作
            for (int i = 0; i < 7; i++)
            {
                SerialManual.stoveLidState[i] = SerialManual.STOVE_LID_STATE.CLOSED;
            }
        }
    }

    /**
     *      打开炉盖的任务
     *
     * */
    private int openStoveLidPos = 0;
    private int closeStoveLidPos = 0;

    private void ThreadOpenStoveLid()
    {
        while (true)
        {
            Thread.Sleep(100);

            if (!GenericOp.AutoMode) continue;
            // 只有在龙门不再炉盖附近时，才允许打开，否则很危险。
            if (ABTask.loadPoints == null) continue;

            // 安全规则判断
            if ((GenericOp.xPos < ABTask.loadPoints[openStoveLidPos].X + 1200) &&
                    (GenericOp.xPos > ABTask.loadPoints[openStoveLidPos].X - 1200))
            {
                SerialManual.StopStoveLidCmd(openStoveLidPos);// 如果在龙门附近打开盖子，就停止所有盖子的动作。
            }
            if ((GenericOp.xPos < ABTask.loadPoints[closeStoveLidPos].X + 1200) &&
                    (GenericOp.xPos > ABTask.loadPoints[closeStoveLidPos].X - 1200))
            {
                SerialManual.StopStoveLidCmd(closeStoveLidPos);// 如果在龙门附近打开盖子，就停止所有盖子的动作。
            }

            // --------------------------------------------------------------
            // 打开、关闭、停止炉盖动作
            //
            // 需要打开炉盖的炉子有    _ _ _ 3、4、5、6 _ _ _ _、11、12
            int tmpPos;
            for (int i = 0; i < 7; i++)
            {
                if (stoveLidAction[i] == STOVE_LID_ACTION.CLOSE)
                {
                    tmpPos = GetPos_fromStoveLidIndex(i);
                    if ((GenericOp.xPos < ABTask.loadPoints[tmpPos].X + 1200) &&
                    (GenericOp.xPos > ABTask.loadPoints[tmpPos].X - 1200))
                    {
                        continue;// 夹爪在炉子上方，就忽略关闭命令
                    }
                    SerialManual.CloseStoveLid(i);// 关闭炉盖
                }
                else if (stoveLidAction[i] == STOVE_LID_ACTION.OPEN)
                {
                    tmpPos = GetPos_fromStoveLidIndex(i);
                    if ((GenericOp.xPos < ABTask.loadPoints[tmpPos].X + 1200) &&
                    (GenericOp.xPos > ABTask.loadPoints[tmpPos].X - 1200))
                    {
                        continue;// 夹爪在炉子上方，就忽略打开命令
                    }
                    SerialManual.OpenStoveLid(i);// 打开炉盖
                }
                if (stoveLidAction[i] == STOVE_LID_ACTION.NO_ACTION)
                {
                    SerialManual.StopStoveLidCmd(i);// 停止炉盖动作
                }
            }
        }
    }

    private int GetStoveLidIndex(int pos)
    {
        if ((pos >= 3) && (pos <= 6))
        {
            return (pos - 3);// 打开3、4、5、6炉盖
        }
        else if ((pos >= 11) && (pos <= 12))
        {
            return (pos - 7);// 打开10、11、12炉盖
        }
        else
        {
            return -1;
        }
    }

    private int GetPos_fromStoveLidIndex(int index)
    {
        if (index < 4)
        {
            return index + 3;
        }
        else
        {
            return index + 7;
        }
    }

    private void ThreadAB()
    {
        while (true)
        {
            Thread.Sleep(100);

            // 执行任务列表中的任务，执行完成的，就从任务列表中删除。
            if (currTask != null)
            {
                // ---------------------------------------------------
                // 根据任务状态执行任务
                //
                if (currTask.taskStatus == ABTask.STATUS.IDLE)
                {//
                    if (currTask.AB_TASK_A == hyWorkFlow.POS_LOAD)
                    {// 如果是新的任务（新的夹具），那么就添加到夹具列表
                        shareCarrierRes.mutex.WaitOne();//申请(互斥，互锁)
                        carrierList.Add(currTask.carrier);
                        shareCarrierRes.mutex.ReleaseMutex();// 释放
                    }
                    //
                    currTask.taskStatus = ABTask.STATUS.PREA;// 更新任务状态
                    currCarrier = currTask.carrier;
                    // 更新任务信息，记录到xml中
                    UpdateXmlNodeTaskInfo(ABTask.STATUS.PREA, currTask.AB_TASK_A, currTask.AB_TASK_B, currTask.carrier.id);
                }
                else if (currTask.taskStatus == ABTask.STATUS.PREA)
                {
                   /* if (((GenericOp.xPos < 4360)) || ((GenericOp.xPos > 13965) && (GenericOp.xPos < 21478)))
                    {
                        for (int a = 0; a < 7; a++)    //检查炉盖是否都已经关闭 关闭才允许运行
                        {
                            SerialManual.STOVE_LID_STATE state;
                            state = SerialManual.stoveLidState[a];
                            if (state != SerialManual.STOVE_LID_STATE.CLOSED)
                            {
                                SerialManual.CloseStoveLid(a);
                            }
                            while (state != SerialManual.STOVE_LID_STATE.CLOSED)
                            {
                                Thread.Sleep(500);
                                state = SerialManual.stoveLidState[a];
                                
                            }
                            
                        }
                    }
                    */
                    // 炉盖打开任务
                    int i = GetStoveLidIndex(currTask.AB_TASK_A);
                    if (i >= 0)
                    {
                        stoveLidAction[i] = STOVE_LID_ACTION.OPEN;// 要打开 A pos 的炉盖
                    }
                    //SerialManual.CloseStoveLid(8);//关闭后门
                    /*if (currTask.AB_TASK_A == hyWorkFlow.POS_LOAD)//打开前门
                    {
                        SerialManual.STOVE_LID_STATE state;
                        SerialManual.CloseStoveLid(7);//打开前门
                        state = SerialManual.stoveLidState[8];// 查看前门的状态
                        while (state != SerialManual.STOVE_LID_STATE.ERROR)
                        {// 等待炉盖打开到位信号。
                            Thread.Sleep(100);// 等待前门打开
                            state = SerialManual.stoveLidState[8];// 查看前门状态

                            // 超时处理
                            // TODO: 如果60秒都没有打开的话，需要给出提示信息。告诉用户炉盖故障：接近开关故障。
                            timeout_openStoveLid++;
                            if (timeout_openStoveLid > 600)
                            {
                                GenericOp.errCode |= 0x01;
                            }
                        }
                    }*/
                    if (GenericOp.GREASEnum > 7)//注油 润滑
                    {
                        SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.GREASE;
                    }
                    //
                    PRE_A_DOING();

                    currTask.taskStatus = ABTask.STATUS.CLAMPA;// 更新任务状态
                    UpdateXmlNodeTaskInfo(ABTask.STATUS.CLAMPA, currTask.AB_TASK_A, currTask.AB_TASK_B, currTask.carrier.id);
                }
                else if (currTask.taskStatus == ABTask.STATUS.CLAMPA)
                {
                    CLAMPA_DOING();
                    currTask.taskStatus = ABTask.STATUS.PREB;// 更新任务状态
                    UpdateXmlNodeTaskInfo(ABTask.STATUS.PREB, currTask.AB_TASK_A, currTask.AB_TASK_B, currTask.carrier.id);
                }
                else if (currTask.taskStatus == ABTask.STATUS.PREB)
                {
                    // 炉盖关闭任务
                    int i = GetStoveLidIndex(currTask.AB_TASK_A);
                    if (i >= 0)
                    {
                        stoveLidAction[i] = STOVE_LID_ACTION.CLOSE;

                        // 要关闭 A pos 的炉盖

                    }
                    /*if (currTask.AB_TASK_B == hyWorkFlow.POS_UNLOAD) //打开后门
                    {
                        SerialManual.OpenStoveLid(8);
                        SerialManual.STOVE_LID_STATE state;
                        state = SerialManual.stoveLidState[7];// 查看后门的状态
                        while (state != SerialManual.STOVE_LID_STATE.ERROR)
                        {// 等待炉盖打开到位信号。
                            Thread.Sleep(100);// 等待前门打开
                            state = SerialManual.stoveLidState[7];// 查看hou门状态

                            // 超时处理
                            // TODO: 如果60秒都没有打开的话，需要给出提示信息。告诉用户炉盖故障：接近开关故障。
                            timeout_openStoveLid++;
                            if (timeout_openStoveLid > 600)
                            {
                                GenericOp.errCode |= 0x01;
                            }
                        }
                    }
                    */
                    // 炉盖打开任务
                    i = GetStoveLidIndex(currTask.AB_TASK_B);
                    if (i >= 0)
                    {
                        stoveLidAction[i] = STOVE_LID_ACTION.OPEN;// 要打开 B pos 的炉盖
                    }

                    PRE_B_DOING();
                    currTask.taskStatus = ABTask.STATUS.RELAXB;// 更新任务状态
                    UpdateXmlNodeTaskInfo(ABTask.STATUS.RELAXB, currTask.AB_TASK_A, currTask.AB_TASK_B, currTask.carrier.id);
                }
                else if (currTask.taskStatus == ABTask.STATUS.RELAXB)
                {
                    if (currTask.AB_TASK_A == hyWorkFlow.POS_LOAD)//关闭前门
                    {
                        SerialManual.OpenStoveLid(7);
                    }
                    RELAXB_DOING();
                    currTask.taskStatus = ABTask.STATUS.AFTERB;// 更新任务状态
                    UpdateXmlNodeTaskInfo(ABTask.STATUS.AFTERB, currTask.AB_TASK_A, currTask.AB_TASK_B, currTask.carrier.id);
                }
                else if (currTask.taskStatus == ABTask.STATUS.AFTERB)
                {
                    // 炉盖关闭任务
                    int i = GetStoveLidIndex(currTask.AB_TASK_B);
                    if (i >= 0)
                    {
                        switch (i)
                        {
                            case 3:
                                stoveLidAction[i] = STOVE_LID_ACTION.OPEN;
                                break;
                            default:
                                stoveLidAction[i] = STOVE_LID_ACTION.CLOSE;
                                break;
                        }
                        // 要关闭 B pos 的炉盖 4号炉不关闭
                    }

                    AFTER_B_DOING();
                    //if (currTask.AB_TASK_B == hyWorkFlow.POS_UNLOAD)//到达卸载位卸货
                    if(currTask.AB_TASK_B==9)//到达9号水槽及卸货
                    {// 如果到达了卸载位，就移除夹具
                        shareCarrierRes.mutex.WaitOne();//申请(互斥，互锁)
                        carrierList.Remove(currTask.carrier);
                        shareCarrierRes.mutex.ReleaseMutex();//  释放
                    }

                    currTask.taskStatus = ABTask.STATUS.STOP;// 更新任务状态
                    UpdateXmlNodeTaskInfo(ABTask.STATUS.STOP, currTask.AB_TASK_A, currTask.AB_TASK_B, currTask.carrier.id);
                }
            }
        }
    }

    #region 自动控制

    private void PRE_A_DOING()
    {
        bool isOver = false;
        // ----------------------------------------------------
        // 松夹爪
        while (!isOver)
        {
            isOver = CLAMP_RELAX_DO();// 松开
            if (!drawOnly)
            {
                isOver &= ABTask.CLAMP_RELAX_DO();
            }
            Thread.Sleep(10);
        }
        PRE_DOING(currTask.AB_TASK_A);
    }

    private void PRE_B_DOING()
    {
        PRE_DOING(currTask.AB_TASK_B);
    }

    private int timeout_openStoveLid;

    private void PRE_DOING(int pos_index)
    {
        // ----------------------------------------------------
        // 至上点
        bool isOver = false;
        while (!isOver)
        {
            isOver = Z_TO(zeroPos.Z, 1);       // 上点
            if (!drawOnly)
            {
                isOver &= ABTask.Z_TO(true, ABTask.zeroPos.Z, ABTask.zSpeed);
            }
            Thread.Sleep(10);
        }
        if (!drawOnly)
        {
            int a = currTask.AB_TASK_A;
            if ((a == 2) && SerialWireless.GetGtryState(SerialWireless.GTRY_STATE_CLAMP))//前清洗吹干等待

            {
                Thread.Sleep(30000);
            }
        }
        // 判断炉盖的状态，如果炉盖已经打开，则允许前行，否则不允许前行
        // 需要打开炉盖的炉子有    _ _ _ 3、4、5、6 _ _ _ _、11、12
        if (!drawOnly)
        {
            int openStoveLidIndex;
            for (openStoveLidIndex = 0; openStoveLidIndex < 7; openStoveLidIndex++)
            {
                if (stoveLidAction[openStoveLidIndex] == STOVE_LID_ACTION.OPEN)
                {
                    break;// 发现有炉盖需要打开
                }
            }

            //
            SerialManual.STOVE_LID_STATE state;
            if (openStoveLidIndex < 7)
            {
                // 有炉盖需要打开，则不断等待炉盖打开到位信号。
                state = SerialManual.stoveLidState[openStoveLidIndex];// 查看当前这个炉盖的状态
                while (state != SerialManual.STOVE_LID_STATE.OPENED)
                {// 等待炉盖打开到位信号。
                    Thread.Sleep(100);// 等待炉盖打开
                    state = SerialManual.stoveLidState[openStoveLidIndex];// 查看当前这个炉盖的状态

                    // 超时处理
                    // TODO: 如果60秒都没有等到炉盖打开的话，需要给出提示信息。告诉用户炉盖故障：接近开关故障。
                    timeout_openStoveLid++;
                    if (timeout_openStoveLid > 600)
                    {
                        GenericOp.errCode |= 0x01;
                    }
                }
                GenericOp.errCode &= ~0x01;
            }
        }

        // ----------------------------------------------------
        // 进入 A或者B 点区域
        isOver = false;
        while (!isOver)
        {
            isOver = X_TO(loadPoints[pos_index].X, 50);     // 平移 --> A
            if (!drawOnly)
            {
                isOver &= ABTask.X_TO(true, ABTask.loadPoints[pos_index].X, ABTask.xSpeed);     // 平移 --> A
            }
            Thread.Sleep(10);
        }
    }

    private void CLAMPA_DOING()
    {
        // ----------------------------------------------------
        // 至下点
        bool isOver = false;
        while (!isOver)
        {
            isOver = Z_TO(loadPoints[currTask.AB_TASK_A].Z, 1);       // 下点
            if (!drawOnly)
            {
                isOver &= ABTask.Z_TO(true, ABTask.loadPoints[currTask.AB_TASK_A].Z, ABTask.zSpeed);
            }
            Thread.Sleep(10);
        }

        //
        isOver = false;
        while (!isOver)
        {
            isOver = CLAMP_DO();       // 夹紧
            if (!drawOnly)
            {
                isOver &= ABTask.CLAMP_DO();
            }
            Thread.Sleep(10);
        }
    }

    private void RELAXB_DOING()
    {
        // ----------------------------------------------------
        // 至下点
        bool isOver = false;
        while (!isOver)
        {
            isOver = Z_TO(loadPoints[currTask.AB_TASK_B].Z, 1);       // 下点
            if (!drawOnly)
            {
                isOver &= ABTask.Z_TO(true, ABTask.loadPoints[currTask.AB_TASK_B].Z, ABTask.zSpeed);
            }
            Thread.Sleep(10);
        }
        //
        isOver = false;
        while (!isOver)
        {
            isOver = CLAMP_RELAX_DO();       // 松开
            if (!drawOnly)
            {
                isOver &= ABTask.CLAMP_RELAX_DO();
            }
            Thread.Sleep(10);
        }
    }

    private void AFTER_B_DOING()
    {
        // 至上点
        bool isOver = false;
        while (!isOver)
        {
            isOver = Z_TO(zeroPos.Z, 1); // 上点
            GenericOp.GREASEnum++; //每个AB流程中会有两次z轴上升运动，计数
            if (!drawOnly)
            {
                isOver &= ABTask.Z_TO(true, ABTask.zeroPos.Z, ABTask.zSpeed);
            }
            Thread.Sleep(10);
        }

        int b = currTask.AB_TASK_B;
        int rest_p = currTask.AB_TASK_B;
        if (taskList.Count == 1)
        {// X 横向移动到休息位休息
            if (b == 3 || b == 4) rest_p = 2;
            if (b == 5 || b == 6) rest_p = 7;
            if (b >= 9) rest_p = 2;

            isOver = false;
            while (!isOver)
            {
                isOver = X_TO(loadPoints[rest_p].X, 50);     // 平移至休息位
                if (!drawOnly)
                {
                    isOver &= ABTask.X_TO(true, ABTask.loadPoints[rest_p].X, ABTask.xSpeed);     // 平移至休息位
                }
                Thread.Sleep(10);
            }
            /*if (currTask.AB_TASK_B == hyWorkFlow.POS_UNLOAD)//关闭后门
            {
                SerialManual.CloseStoveLid(8);
            }*/
        }
    }

    private bool UpdatePos()
    {
        bool isGoing = false;

        if (drawOnly)
        {
            Thread.Sleep(100);
            // Z
            if (currPosDraw.Z < destPosDraw.Z)
            {
                currPosDraw.Z += 1;
                if (currCarrier.isClamped)
                {
                    currCarrier.currPosDraw.Z += 1;
                }
                isGoing = true;
            }
            else if (currPosDraw.Z > destPosDraw.Z)
            {
                currPosDraw.Z -= 1;
                if (currCarrier.isClamped)
                {
                    currCarrier.currPosDraw.Z -= 1; 
                }
                isGoing = true;
            }

            // X
            if (currPosDraw.X < destPosDraw.X)
            {
                currPosDraw.X += 1;
                if (currCarrier.isClamped)
                {
                    currCarrier.currPosDraw.X += 1;
                }
                isGoing = true;
            }
            else if (currPosDraw.X > destPosDraw.X)
            {
                currPosDraw.X -= 1;
                if (currCarrier.isClamped)
                {
                    currCarrier.currPosDraw.X -= 1;
                }
                isGoing = true;
            }
        }
        else
        {
            Pos2Pos();
            if (currCarrier.isClamped)
            {
                currCarrier.currPosDraw.Z = currPosDraw.Z;
                currCarrier.currPosDraw.X = currPosDraw.X;
            }
        }

        return isGoing;
    }

    private bool Z_TO(int zPos, int zSpeed)
    {
        if (GenericOp.EStop || !GenericOp.AutoMode)
        {
            return false;
        }
        destPosDraw.Z = zPos;
        destPosDraw.X = currPosDraw.X;

        UpdatePos();
        UpdatePos();
        UpdatePos();
        UpdatePos();
        UpdatePos();
        if (UpdatePos())
        {
            // 等待，直到命令结束
            return false;
        }
        return true;
    }

    private bool X_TO(int xPos, int xSpeed)
    {
        if (GenericOp.EStop || !GenericOp.AutoMode)
        {
            return false;
        }

        destPosDraw.X = xPos;
        destPosDraw.Z = currPosDraw.Z;

        UpdatePos();
        UpdatePos();
        UpdatePos();
        UpdatePos();
        UpdatePos();
        if (UpdatePos())
        {
            // 等待，直到命令结束
            return false;
        }
        return true;
    }

    private bool CLAMP_DO()
    {
        if (GenericOp.EStop || !GenericOp.AutoMode)
        {
            return false;
        }
        // 动画
        if (PointsInRange(currCarrier.currPosDraw, currPosDraw, 20))
        {
            currCarrier.isClamped = true;// 夹取
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool CLAMP_RELAX_DO()
    {
        if (GenericOp.EStop || !GenericOp.AutoMode)
        {
            return false;
        }
        // 动画
        currCarrier.isClamped = false;// 释放
        return true;
    }

    public bool PointsInRange(ServoPoint a, ServoPoint b, int range)
    {
        return (Math.Abs(a.X - b.X) <= range) && (Math.Abs(a.Z - b.Z) <= range);
    }

    #endregion 自动控制

    protected override void OnDraw()
    {
        // Gets the image from the global resources
        Image imageBeam = HY_PIP.Properties.Resources.beam;
        Image imageWalkingSystem = HY_PIP.Properties.Resources.walkingSystem;
        Image imageOverView = HY_PIP.Properties.Resources.layout;
        Image imageClamp = HY_PIP.Properties.Resources.clamp;
        Image imageCarrier = HY_PIP.Properties.Resources.carrier;

        // Sets the text's font and style and draws it
        // 重量文字
        float fontSize = 18.0f;
        Point textPosition = new Point(50 + currPosDraw.X, 150);
        string text = "0T";
        //if(currCarrierIndex != -1)
        {
            text = "0.97T";
        }
        DrawText(text, "Calibri", fontSize, FontStyle.Italic, Brushes.White, textPosition);

        //
        // 位置校准测试用
        //currPosDraw.X = test_x;
        //currPosDraw.Z = test_z;

        // Sets the images' sizes and positions
        // 背景场景
        Rectangle recOverView = new Rectangle(0, 0, imageOverView.Size.Width, imageOverView.Size.Height);
        this.graphics.DrawImage(imageOverView, recOverView);

        // 行走机构
        Rectangle recWalkingSystem = new Rectangle(194 + currPosDraw.X, 22, imageWalkingSystem.Size.Width, imageWalkingSystem.Size.Height);
        this.graphics.DrawImage(imageWalkingSystem, recWalkingSystem);

        // 夹爪
        Rectangle recClamp = new Rectangle(210 + currPosDraw.X, 65 + currPosDraw.Z, imageClamp.Size.Width, imageClamp.Size.Height);
        this.graphics.DrawImage(imageClamp, recClamp);

        /*  夹具位置校准测试用
        Rectangle rec1 = new Rectangle();// 最多支持 5 个载具，同时出现。
        rec1 = new Rectangle(198 + test_x, 130+test_z, imageCarrier.Size.Width, imageCarrier.Size.Height);
        this.graphics.DrawImage(imageCarrier, rec1);
        */

        //申请(互斥，互锁)
        shareCarrierRes.mutex.WaitOne();
        // 载具
        foreach (hyCarrier cr in carrierList)
        {
            Rectangle rec = new Rectangle();// 最多支持 5 个载具，同时出现。
            rec = new Rectangle(198 + cr.currPosDraw.X, 130 + cr.currPosDraw.Z, imageCarrier.Size.Width, imageCarrier.Size.Height);
            this.graphics.DrawImage(imageCarrier, rec);
            // 载具文字
            if (!PointsInRange(cr.currPosDraw, loadPoints[hyWorkFlow.POS_LOAD], 2))
            {
                fontSize = 10.0f;
                textPosition = new Point(205 + cr.currPosDraw.X, 180 + cr.currPosDraw.Z);
                DrawText("夹具:" + cr.name.ToString(), "Calibri", fontSize
                    , FontStyle.Italic | FontStyle.Bold, Brushes.Black, textPosition);
                textPosition.Y += 20;
                DrawText("余时:" + cr.remainingTime.ToString(), "Calibri", fontSize
                    , FontStyle.Italic | FontStyle.Bold, Brushes.White, textPosition);
            }
        }
        //  释放
        shareCarrierRes.mutex.ReleaseMutex();

        // 横梁
        Rectangle recBeam = new Rectangle(178, 91, imageBeam.Size.Width, imageBeam.Size.Height);
        this.graphics.DrawImage(imageBeam, recBeam);
    }

    ///<summary>
    /// 保存和记录当前任务状态信息
    ///</summary>
    private XmlDocument xmlDoc;

    private XmlNode xroot;
    private string xmlName = "hyTask.xml";

    public void UpdateXmlNodeTaskInfo(ABTask.STATUS status, int A, int B, int carrier_id)
    {
        //申请(互斥，互锁)
        shareRes.mutex.WaitOne();
        if (xmlDoc == null)
        {
            xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlName); //加载xml文件
            xroot = xmlDoc.SelectSingleNode("task");
        }

        XmlNodeList xnl = xroot.ChildNodes;

        xnl[0].InnerText = ((int)status).ToString();// task_status
        xnl[1].InnerText = A.ToString();// A
        xnl[2].InnerText = B.ToString();// B
        xnl[3].InnerText = carrier_id.ToString();// carrier_id

        xmlDoc.Save(xmlName);//保存。
        //  释放
        shareRes.mutex.ReleaseMutex();
    }

    private class shareRes
    {
        public static int count = 0;
        public static Mutex mutex = new Mutex();
    }

    private class shareCarrierRes
    {
        public static int count = 0;
        public static Mutex mutex = new Mutex();
    }

    public ABTask LoadXmlNodeTaskInfo()
    {
        if (xmlDoc == null)
        {
            xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlName); //加载xml文件
            xroot = xmlDoc.SelectSingleNode("task");
        }

        XmlNodeList xnl = xroot.ChildNodes;
        ABTask task = null;
        foreach (hyWorkFlow wf in workGroup.workFlowList)
        {
            if (wf.carrier.status == hyCarrier.STATUS.DOING_CHANGE_OVER)
            {// 寻找当前正在换型的夹具
                if (wf.carrier.id == Convert.ToInt32(xnl[3].InnerText))
                {// 确定夹具ID是一致的（复查）
                    task = new ABTask(wf);
                    task.taskStatus = (ABTask.STATUS)Convert.ToInt32(xnl[0].InnerText);// task_status
                    task.AB_TASK_A = Convert.ToInt32(xnl[1].InnerText);// A
                    task.AB_TASK_B = Convert.ToInt32(xnl[2].InnerText);// B

                    task.carrier = wf.carrier;
                    currCarrier = wf.carrier;
                    carrierList.Add(currCarrier);
                    int p = task.AB_TASK_A;
                    switch (task.taskStatus)
                    {
                        case ABTask.STATUS.IDLE:
                        case ABTask.STATUS.PREA:
                        case ABTask.STATUS.CLAMPA:
                            p = task.AB_TASK_A;
                            break;

                        default:
                            p = task.AB_TASK_B;
                            break;
                    }
                    currCarrier.currPosDraw = new ServoPoint(loadPoints[p].X, loadPoints[p].Z);
                    currPosDraw = new ServoPoint(currCarrier.currPosDraw.X, currCarrier.currPosDraw.Z);
                    taskList.Add(task);
                }
            }
        }
        return task;
    }

    private int get_rest_p(int b)
    {
        int rest_p = b;
        if (b == 3 || b == 4) rest_p = 2;
        if (b == 5 || b == 6) rest_p = 7;
        if (b >= 10) rest_p = 9;
        return rest_p;
    }

    #region 手动控制

    /**
     *
     *       手动控制
     *
     * */

    private ServoPoint manualDestPosDraw = new ServoPoint(0, 0);

    public void ManualUp()
    {
        // ----------------------------------------------------
        // 至上点
        bool isOver = false;
        while (!isOver)
        {
            isOver = ManualZ_TO(zeroPos.Z, 1);       // 上点
            if (!drawOnly)
            {
                isOver &= ABTask.Z_TO_MANUAL(true, ABTask.zeroPos.Z, ABTask.zSpeed);
            }
            Thread.Sleep(10);
        }
    }

    public void ManualDown(int pos_index)
    {
        // ----------------------------------------------------
        // 至下点
        bool isOver = false;
        while (!isOver)
        {
            isOver = ManualZ_TO(loadPoints[pos_index].Z, 1);       // 下点
            if (!drawOnly)
            {
                isOver &= ABTask.Z_TO_MANUAL(true, ABTask.loadPoints[pos_index].Z, ABTask.zSpeed);
            }
            Thread.Sleep(10);
        }
    }

    public void ManualHorizontalMove(int pos_index)
    {
        // ----------------------------------------------------
        // 水平方向运动
        bool isOver = false;
        while (!isOver)
        {
            isOver = ManualX_TO(loadPoints[pos_index].X, 50);     // 平移 --> A
            if (!drawOnly)
            {
                isOver &= ABTask.X_TO_MANUAL(true, ABTask.loadPoints[pos_index].X, ABTask.xSpeed);     // 平移 --> A
            }
            Thread.Sleep(10);
        }
    }

    private int PointsMap(int x, int x0, int x1, int y0, int y1)//绘制模拟图     问题点1无法适用从后向前的工艺*********

    {
        return (y1 - y0) * (x - x0) / (x1 - x0) + y0;
    }

    public void Pos2Pos()
    {
        // -----------------------------------------------------------------------------
        //  实际装载点位
        int currPosX = GenericOp.xPos;
        int currPosZ = GenericOp.zPos;
        int i;
        for (i = 0; i < hyProcess.loadPointsNum; i++)
        {
            if (currPosX < ABTask.logicPoints[i].X)
            {// 看看当前龙门处在什么位置
                if (i == 0)
                {
                    currPosDraw.X = logicPoints[0].X;// 最小位置
                    currPosDraw.Z = 0;
                }
                else
                {
                    currPosDraw.X = PointsMap(currPosX, ABTask.logicPoints[i - 1].X, ABTask.logicPoints[i].X, logicPoints[i - 1].X, logicPoints[i].X);
                    currPosDraw.Z = 0;
                }
                break;// 找到了
            }
            else if (currPosX == ABTask.logicPoints[i].X)
            {
                currPosDraw.X = logicPoints[i].X;
                currPosDraw.Z = PointsMap(currPosZ, 0, ABTask.logicPoints[i].Z, 0, logicPoints[i].Z);
                break;// 找到了
            }
        }
        if (i == hyProcess.loadPointsNum)
        {
            currPosDraw.X = logicPoints[i - 1].X;// 最大位置
            currPosDraw.Z = 0;
        }
    }

    public void ManualClampRelax()
    {
        //
        bool isOver = false;
        while (!isOver)
        {
            //isOver = CLAMP_RELAX_DO();       // 松开
            if (!drawOnly)
            {
                isOver &= ABTask.CLAMP_RELAX_DO();
            }
            Thread.Sleep(10);
        }
    }

    public void ManualClamp()
    {
        //
        bool isOver = false;
        while (!isOver)
        {
            //isOver = CLAMP_DO();       // 夹紧
            if (!drawOnly)
            {
                isOver &= ABTask.CLAMP_DO();
            }
            Thread.Sleep(10);
        }
    }

    private bool ManualUpdatePos()
    {
        bool isGoing = false;

        if (drawOnly)
        {
            // Z
            if (currPosDraw.Z < manualDestPosDraw.Z)
            {
                currPosDraw.Z += 1;
                isGoing = true;
            }
            else if (currPosDraw.Z > manualDestPosDraw.Z)
            {
                currPosDraw.Z -= 1;
                isGoing = true;
            }

            // X
            if (currPosDraw.X < manualDestPosDraw.X)
            {
                currPosDraw.X += 1;
                isGoing = true;
            }
            else if (currPosDraw.X > manualDestPosDraw.X)
            {
                currPosDraw.X -= 1;
                isGoing = true;
            }
        }
        else
        {
            Pos2Pos();
        }
        return isGoing;
    }

    private bool ManualZ_TO(int zPos, int zSpeed)
    {
        if (GenericOp.EStop)
        {
            return true;// 手动操作（动画模拟）时，急停返回true ，用来结束执行。
        }
        manualDestPosDraw.Z = zPos;
        manualDestPosDraw.X = currPosDraw.X;

        ManualUpdatePos();
        ManualUpdatePos();
        ManualUpdatePos();
        ManualUpdatePos();
        ManualUpdatePos();
        if (ManualUpdatePos())
        {
            // 等待，直到命令结束
            return false;
        }
        return true;
    }

    private bool ManualX_TO(int xPos, int xSpeed)
    {
        if (GenericOp.EStop)
        {
            return true;// 手动操作（动画模拟）时，急停返回true ，用来结束执行。
        }

        manualDestPosDraw.X = xPos;
        manualDestPosDraw.Z = currPosDraw.Z;

        ManualUpdatePos();
        ManualUpdatePos();
        ManualUpdatePos();
        ManualUpdatePos();
        ManualUpdatePos();
        if (ManualUpdatePos())
        {
            // 等待，直到命令结束
            return false;
        }
        return true;
    }

    #endregion 手动控制
}