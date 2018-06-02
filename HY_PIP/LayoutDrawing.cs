using HY_PIP;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Xml;

public class LayoutDrawing : DrawingArea
{
    public hyWorkGroup workGroup;// ������(ƽ����)
    public static bool drawOnly = true;

    public int ii = 0;

    public int xpos;
    public int zpos;
    public int cxpos;
    public int czpos;

    //
    public static ServoPoint[] loadPoints = new ServoPoint[hyProcess.loadPointsNum];// �� n ��װ�ص�

    public static ServoPoint[] logicPoints = new ServoPoint[hyProcess.loadPointsNum];

    public List<hyCarrier> carrierList = new List<hyCarrier>();// �ؾ�

    public List<ABTask> taskList = new List<ABTask>();// �����б�
    public ABTask currTask;// ��ǰ����
    public hyCarrier currCarrier;

    private Thread threadAB;
    private Thread threadOpenStoveLid;
    private Thread threadCloseStoveLid;

    private ServoPoint destPosDraw;
    public ServoPoint currPosDraw;

    public ServoPoint zeroPos = new ServoPoint(0, 0);// ��ʼװ�ص�����Ϸ�

    // ¯�Ƕ���ָ��
    public enum STOVE_LID_ACTION { NO_ACTION = 0, OPEN = 1, CLOSE = 2 };

    public static STOVE_LID_ACTION[] stoveLidAction = new STOVE_LID_ACTION[7];

    public LayoutDrawing()
    {
        // -----------------------------------------------------------------------------
        //
        //  ������װ�ص�λ
        //
        logicPoints[0] = loadPoints[hyWorkFlow.POS_LOAD] = new ServoPoint(0, 100);// װ�ص�
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


        logicPoints[12] = loadPoints[hyWorkFlow.POS_IDLE] = new ServoPoint(954, 156);// �յ�
        logicPoints[11] = loadPoints[10] = new ServoPoint(964, 156);
        logicPoints[13] = loadPoints[11] = new ServoPoint(1093, 156);
        logicPoints[14] = loadPoints[12] = new ServoPoint(1188, 156);
        logicPoints[15] = loadPoints[hyWorkFlow.POS_UNLOAD] = new ServoPoint(1380, 100);// ж�ص�
        //loadPoints[hyWorkFlow.POS_ZERO] = zeroPos;// ���
        //
        destPosDraw = new ServoPoint(0, 0);
        currPosDraw = new ServoPoint(zeroPos.X, zeroPos.Z);
        // -----------------------------------------------------------------------------
        //
        //  �����߳�
        //
        threadAB = new Thread(new ThreadStart(ThreadAB)); //Ҳ�ɼ�дΪ new Thread(ThreadMethod);
        threadAB.Start(); //�����߳�

        //
        threadOpenStoveLid = new Thread(new ThreadStart(ThreadOpenStoveLid));
        threadOpenStoveLid.Start();// �򿪡��ر�¯������

        if (drawOnly)
        {// ģ�����
            for (int i = 0; i < 7; i++)
            {
                SerialManual.stoveLidState[i] = SerialManual.STOVE_LID_STATE.CLOSED;
            }
        }
    }

    /**
     *      ��¯�ǵ�����
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
            // ֻ�������Ų���¯�Ǹ���ʱ��������򿪣������Σ�ա�
            if (ABTask.loadPoints == null) continue;

            // ��ȫ�����ж�
            if ((GenericOp.xPos < ABTask.loadPoints[openStoveLidPos].X + 1200) &&
                    (GenericOp.xPos > ABTask.loadPoints[openStoveLidPos].X - 1200))
            {
                SerialManual.StopStoveLidCmd(openStoveLidPos);// ��������Ÿ����򿪸��ӣ���ֹͣ���и��ӵĶ�����
            }
            if ((GenericOp.xPos < ABTask.loadPoints[closeStoveLidPos].X + 1200) &&
                    (GenericOp.xPos > ABTask.loadPoints[closeStoveLidPos].X - 1200))
            {
                SerialManual.StopStoveLidCmd(closeStoveLidPos);// ��������Ÿ����򿪸��ӣ���ֹͣ���и��ӵĶ�����
            }

            // --------------------------------------------------------------
            // �򿪡��رա�ֹͣ¯�Ƕ���
            //
            // ��Ҫ��¯�ǵ�¯����    _ _ _ 3��4��5��6 _ _ _ _��11��12
            int tmpPos;
            for (int i = 0; i < 7; i++)
            {
                if (stoveLidAction[i] == STOVE_LID_ACTION.CLOSE)
                {
                    tmpPos = GetPos_fromStoveLidIndex(i);
                    if ((GenericOp.xPos < ABTask.loadPoints[tmpPos].X + 1200) &&
                    (GenericOp.xPos > ABTask.loadPoints[tmpPos].X - 1200))
                    {
                        continue;// ��צ��¯���Ϸ����ͺ��Թر�����
                    }
                    SerialManual.CloseStoveLid(i);// �ر�¯��
                }
                else if (stoveLidAction[i] == STOVE_LID_ACTION.OPEN)
                {
                    tmpPos = GetPos_fromStoveLidIndex(i);
                    if ((GenericOp.xPos < ABTask.loadPoints[tmpPos].X + 1200) &&
                    (GenericOp.xPos > ABTask.loadPoints[tmpPos].X - 1200))
                    {
                        continue;// ��צ��¯���Ϸ����ͺ��Դ�����
                    }
                    SerialManual.OpenStoveLid(i);// ��¯��
                }
                if (stoveLidAction[i] == STOVE_LID_ACTION.NO_ACTION)
                {
                    SerialManual.StopStoveLidCmd(i);// ֹͣ¯�Ƕ���
                }
            }
        }
    }

    private int GetStoveLidIndex(int pos)
    {
        if ((pos >= 3) && (pos <= 6))
        {
            return (pos - 3);// ��3��4��5��6¯��
        }
        else if ((pos >= 11) && (pos <= 12))
        {
            return (pos - 7);// ��10��11��12¯��
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

            // ִ�������б��е�����ִ����ɵģ��ʹ������б���ɾ����
            if (currTask != null)
            {
                // ---------------------------------------------------
                // ��������״ִ̬������
                //
                if (currTask.taskStatus == ABTask.STATUS.IDLE)
                {//
                    if (currTask.AB_TASK_A == hyWorkFlow.POS_LOAD)
                    {// ������µ������µļоߣ�����ô����ӵ��о��б�
                        shareCarrierRes.mutex.WaitOne();//����(���⣬����)
                        carrierList.Add(currTask.carrier);
                        shareCarrierRes.mutex.ReleaseMutex();// �ͷ�
                    }
                    //
                    currTask.taskStatus = ABTask.STATUS.PREA;// ��������״̬
                    currCarrier = currTask.carrier;
                    // ����������Ϣ����¼��xml��
                    UpdateXmlNodeTaskInfo(ABTask.STATUS.PREA, currTask.AB_TASK_A, currTask.AB_TASK_B, currTask.carrier.id);
                }
                else if (currTask.taskStatus == ABTask.STATUS.PREA)
                {
                   /* if (((GenericOp.xPos < 4360)) || ((GenericOp.xPos > 13965) && (GenericOp.xPos < 21478)))
                    {
                        for (int a = 0; a < 7; a++)    //���¯���Ƿ��Ѿ��ر� �رղ���������
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
                    // ¯�Ǵ�����
                    int i = GetStoveLidIndex(currTask.AB_TASK_A);
                    if (i >= 0)
                    {
                        stoveLidAction[i] = STOVE_LID_ACTION.OPEN;// Ҫ�� A pos ��¯��
                    }
                    //SerialManual.CloseStoveLid(8);//�رպ���
                    /*if (currTask.AB_TASK_A == hyWorkFlow.POS_LOAD)//��ǰ��
                    {
                        SerialManual.STOVE_LID_STATE state;
                        SerialManual.CloseStoveLid(7);//��ǰ��
                        state = SerialManual.stoveLidState[8];// �鿴ǰ�ŵ�״̬
                        while (state != SerialManual.STOVE_LID_STATE.ERROR)
                        {// �ȴ�¯�Ǵ򿪵�λ�źš�
                            Thread.Sleep(100);// �ȴ�ǰ�Ŵ�
                            state = SerialManual.stoveLidState[8];// �鿴ǰ��״̬

                            // ��ʱ����
                            // TODO: ���60�붼û�д򿪵Ļ�����Ҫ������ʾ��Ϣ�������û�¯�ǹ��ϣ��ӽ����ع��ϡ�
                            timeout_openStoveLid++;
                            if (timeout_openStoveLid > 600)
                            {
                                GenericOp.errCode |= 0x01;
                            }
                        }
                    }*/
                    if (GenericOp.GREASEnum > 7)//ע�� ��
                    {
                        SerialWireless.gtryCmd = SerialWireless.GTYRY_CMD.GREASE;
                    }
                    //
                    PRE_A_DOING();

                    currTask.taskStatus = ABTask.STATUS.CLAMPA;// ��������״̬
                    UpdateXmlNodeTaskInfo(ABTask.STATUS.CLAMPA, currTask.AB_TASK_A, currTask.AB_TASK_B, currTask.carrier.id);
                }
                else if (currTask.taskStatus == ABTask.STATUS.CLAMPA)
                {
                    CLAMPA_DOING();
                    currTask.taskStatus = ABTask.STATUS.PREB;// ��������״̬
                    UpdateXmlNodeTaskInfo(ABTask.STATUS.PREB, currTask.AB_TASK_A, currTask.AB_TASK_B, currTask.carrier.id);
                }
                else if (currTask.taskStatus == ABTask.STATUS.PREB)
                {
                    // ¯�ǹر�����
                    int i = GetStoveLidIndex(currTask.AB_TASK_A);
                    if (i >= 0)
                    {
                        stoveLidAction[i] = STOVE_LID_ACTION.CLOSE;

                        // Ҫ�ر� A pos ��¯��

                    }
                    /*if (currTask.AB_TASK_B == hyWorkFlow.POS_UNLOAD) //�򿪺���
                    {
                        SerialManual.OpenStoveLid(8);
                        SerialManual.STOVE_LID_STATE state;
                        state = SerialManual.stoveLidState[7];// �鿴���ŵ�״̬
                        while (state != SerialManual.STOVE_LID_STATE.ERROR)
                        {// �ȴ�¯�Ǵ򿪵�λ�źš�
                            Thread.Sleep(100);// �ȴ�ǰ�Ŵ�
                            state = SerialManual.stoveLidState[7];// �鿴hou��״̬

                            // ��ʱ����
                            // TODO: ���60�붼û�д򿪵Ļ�����Ҫ������ʾ��Ϣ�������û�¯�ǹ��ϣ��ӽ����ع��ϡ�
                            timeout_openStoveLid++;
                            if (timeout_openStoveLid > 600)
                            {
                                GenericOp.errCode |= 0x01;
                            }
                        }
                    }
                    */
                    // ¯�Ǵ�����
                    i = GetStoveLidIndex(currTask.AB_TASK_B);
                    if (i >= 0)
                    {
                        stoveLidAction[i] = STOVE_LID_ACTION.OPEN;// Ҫ�� B pos ��¯��
                    }

                    PRE_B_DOING();
                    currTask.taskStatus = ABTask.STATUS.RELAXB;// ��������״̬
                    UpdateXmlNodeTaskInfo(ABTask.STATUS.RELAXB, currTask.AB_TASK_A, currTask.AB_TASK_B, currTask.carrier.id);
                }
                else if (currTask.taskStatus == ABTask.STATUS.RELAXB)
                {
                    if (currTask.AB_TASK_A == hyWorkFlow.POS_LOAD)//�ر�ǰ��
                    {
                        SerialManual.OpenStoveLid(7);
                    }
                    RELAXB_DOING();
                    currTask.taskStatus = ABTask.STATUS.AFTERB;// ��������״̬
                    UpdateXmlNodeTaskInfo(ABTask.STATUS.AFTERB, currTask.AB_TASK_A, currTask.AB_TASK_B, currTask.carrier.id);
                }
                else if (currTask.taskStatus == ABTask.STATUS.AFTERB)
                {
                    // ¯�ǹر�����
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
                        // Ҫ�ر� B pos ��¯�� 4��¯���ر�
                    }

                    AFTER_B_DOING();
                    //if (currTask.AB_TASK_B == hyWorkFlow.POS_UNLOAD)//����ж��λж��
                    if(currTask.AB_TASK_B==9)//����9��ˮ�ۼ�ж��
                    {// ���������ж��λ�����Ƴ��о�
                        shareCarrierRes.mutex.WaitOne();//����(���⣬����)
                        carrierList.Remove(currTask.carrier);
                        shareCarrierRes.mutex.ReleaseMutex();//  �ͷ�
                    }

                    currTask.taskStatus = ABTask.STATUS.STOP;// ��������״̬
                    UpdateXmlNodeTaskInfo(ABTask.STATUS.STOP, currTask.AB_TASK_A, currTask.AB_TASK_B, currTask.carrier.id);
                }
            }
        }
    }

    #region �Զ�����

    private void PRE_A_DOING()
    {
        bool isOver = false;
        // ----------------------------------------------------
        // �ɼ�צ
        while (!isOver)
        {
            isOver = CLAMP_RELAX_DO();// �ɿ�
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
        // ���ϵ�
        bool isOver = false;
        while (!isOver)
        {
            isOver = Z_TO(zeroPos.Z, 1);       // �ϵ�
            if (!drawOnly)
            {
                isOver &= ABTask.Z_TO(true, ABTask.zeroPos.Z, ABTask.zSpeed);
            }
            Thread.Sleep(10);
        }
        if (!drawOnly)
        {
            int a = currTask.AB_TASK_A;
            if ((a == 2) && SerialWireless.GetGtryState(SerialWireless.GTRY_STATE_CLAMP))//ǰ��ϴ���ɵȴ�

            {
                Thread.Sleep(30000);
            }
        }
        // �ж�¯�ǵ�״̬�����¯���Ѿ��򿪣�������ǰ�У���������ǰ��
        // ��Ҫ��¯�ǵ�¯����    _ _ _ 3��4��5��6 _ _ _ _��11��12
        if (!drawOnly)
        {
            int openStoveLidIndex;
            for (openStoveLidIndex = 0; openStoveLidIndex < 7; openStoveLidIndex++)
            {
                if (stoveLidAction[openStoveLidIndex] == STOVE_LID_ACTION.OPEN)
                {
                    break;// ������¯����Ҫ��
                }
            }

            //
            SerialManual.STOVE_LID_STATE state;
            if (openStoveLidIndex < 7)
            {
                // ��¯����Ҫ�򿪣��򲻶ϵȴ�¯�Ǵ򿪵�λ�źš�
                state = SerialManual.stoveLidState[openStoveLidIndex];// �鿴��ǰ���¯�ǵ�״̬
                while (state != SerialManual.STOVE_LID_STATE.OPENED)
                {// �ȴ�¯�Ǵ򿪵�λ�źš�
                    Thread.Sleep(100);// �ȴ�¯�Ǵ�
                    state = SerialManual.stoveLidState[openStoveLidIndex];// �鿴��ǰ���¯�ǵ�״̬

                    // ��ʱ����
                    // TODO: ���60�붼û�еȵ�¯�Ǵ򿪵Ļ�����Ҫ������ʾ��Ϣ�������û�¯�ǹ��ϣ��ӽ����ع��ϡ�
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
        // ���� A����B ������
        isOver = false;
        while (!isOver)
        {
            isOver = X_TO(loadPoints[pos_index].X, 50);     // ƽ�� --> A
            if (!drawOnly)
            {
                isOver &= ABTask.X_TO(true, ABTask.loadPoints[pos_index].X, ABTask.xSpeed);     // ƽ�� --> A
            }
            Thread.Sleep(10);
        }
    }

    private void CLAMPA_DOING()
    {
        // ----------------------------------------------------
        // ���µ�
        bool isOver = false;
        while (!isOver)
        {
            isOver = Z_TO(loadPoints[currTask.AB_TASK_A].Z, 1);       // �µ�
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
            isOver = CLAMP_DO();       // �н�
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
        // ���µ�
        bool isOver = false;
        while (!isOver)
        {
            isOver = Z_TO(loadPoints[currTask.AB_TASK_B].Z, 1);       // �µ�
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
            isOver = CLAMP_RELAX_DO();       // �ɿ�
            if (!drawOnly)
            {
                isOver &= ABTask.CLAMP_RELAX_DO();
            }
            Thread.Sleep(10);
        }
    }

    private void AFTER_B_DOING()
    {
        // ���ϵ�
        bool isOver = false;
        while (!isOver)
        {
            isOver = Z_TO(zeroPos.Z, 1); // �ϵ�
            GenericOp.GREASEnum++; //ÿ��AB�����л�������z�������˶�������
            if (!drawOnly)
            {
                isOver &= ABTask.Z_TO(true, ABTask.zeroPos.Z, ABTask.zSpeed);
            }
            Thread.Sleep(10);
        }

        int b = currTask.AB_TASK_B;
        int rest_p = currTask.AB_TASK_B;
        if (taskList.Count == 1)
        {// X �����ƶ�����Ϣλ��Ϣ
            if (b == 3 || b == 4) rest_p = 2;
            if (b == 5 || b == 6) rest_p = 7;
            if (b >= 9) rest_p = 2;

            isOver = false;
            while (!isOver)
            {
                isOver = X_TO(loadPoints[rest_p].X, 50);     // ƽ������Ϣλ
                if (!drawOnly)
                {
                    isOver &= ABTask.X_TO(true, ABTask.loadPoints[rest_p].X, ABTask.xSpeed);     // ƽ������Ϣλ
                }
                Thread.Sleep(10);
            }
            /*if (currTask.AB_TASK_B == hyWorkFlow.POS_UNLOAD)//�رպ���
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
            // �ȴ���ֱ���������
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
            // �ȴ���ֱ���������
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
        // ����
        if (PointsInRange(currCarrier.currPosDraw, currPosDraw, 20))
        {
            currCarrier.isClamped = true;// ��ȡ
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
        // ����
        currCarrier.isClamped = false;// �ͷ�
        return true;
    }

    public bool PointsInRange(ServoPoint a, ServoPoint b, int range)
    {
        return (Math.Abs(a.X - b.X) <= range) && (Math.Abs(a.Z - b.Z) <= range);
    }

    #endregion �Զ�����

    protected override void OnDraw()
    {
        // Gets the image from the global resources
        Image imageBeam = HY_PIP.Properties.Resources.beam;
        Image imageWalkingSystem = HY_PIP.Properties.Resources.walkingSystem;
        Image imageOverView = HY_PIP.Properties.Resources.layout;
        Image imageClamp = HY_PIP.Properties.Resources.clamp;
        Image imageCarrier = HY_PIP.Properties.Resources.carrier;

        // Sets the text's font and style and draws it
        // ��������
        float fontSize = 18.0f;
        Point textPosition = new Point(50 + currPosDraw.X, 150);
        string text = "0T";
        //if(currCarrierIndex != -1)
        {
            text = "0.97T";
        }
        DrawText(text, "Calibri", fontSize, FontStyle.Italic, Brushes.White, textPosition);

        //
        // λ��У׼������
        //currPosDraw.X = test_x;
        //currPosDraw.Z = test_z;

        // Sets the images' sizes and positions
        // ��������
        Rectangle recOverView = new Rectangle(0, 0, imageOverView.Size.Width, imageOverView.Size.Height);
        this.graphics.DrawImage(imageOverView, recOverView);

        // ���߻���
        Rectangle recWalkingSystem = new Rectangle(194 + currPosDraw.X, 22, imageWalkingSystem.Size.Width, imageWalkingSystem.Size.Height);
        this.graphics.DrawImage(imageWalkingSystem, recWalkingSystem);

        // ��צ
        Rectangle recClamp = new Rectangle(210 + currPosDraw.X, 65 + currPosDraw.Z, imageClamp.Size.Width, imageClamp.Size.Height);
        this.graphics.DrawImage(imageClamp, recClamp);

        /*  �о�λ��У׼������
        Rectangle rec1 = new Rectangle();// ���֧�� 5 ���ؾߣ�ͬʱ���֡�
        rec1 = new Rectangle(198 + test_x, 130+test_z, imageCarrier.Size.Width, imageCarrier.Size.Height);
        this.graphics.DrawImage(imageCarrier, rec1);
        */

        //����(���⣬����)
        shareCarrierRes.mutex.WaitOne();
        // �ؾ�
        foreach (hyCarrier cr in carrierList)
        {
            Rectangle rec = new Rectangle();// ���֧�� 5 ���ؾߣ�ͬʱ���֡�
            rec = new Rectangle(198 + cr.currPosDraw.X, 130 + cr.currPosDraw.Z, imageCarrier.Size.Width, imageCarrier.Size.Height);
            this.graphics.DrawImage(imageCarrier, rec);
            // �ؾ�����
            if (!PointsInRange(cr.currPosDraw, loadPoints[hyWorkFlow.POS_LOAD], 2))
            {
                fontSize = 10.0f;
                textPosition = new Point(205 + cr.currPosDraw.X, 180 + cr.currPosDraw.Z);
                DrawText("�о�:" + cr.name.ToString(), "Calibri", fontSize
                    , FontStyle.Italic | FontStyle.Bold, Brushes.Black, textPosition);
                textPosition.Y += 20;
                DrawText("��ʱ:" + cr.remainingTime.ToString(), "Calibri", fontSize
                    , FontStyle.Italic | FontStyle.Bold, Brushes.White, textPosition);
            }
        }
        //  �ͷ�
        shareCarrierRes.mutex.ReleaseMutex();

        // ����
        Rectangle recBeam = new Rectangle(178, 91, imageBeam.Size.Width, imageBeam.Size.Height);
        this.graphics.DrawImage(imageBeam, recBeam);
    }

    ///<summary>
    /// ����ͼ�¼��ǰ����״̬��Ϣ
    ///</summary>
    private XmlDocument xmlDoc;

    private XmlNode xroot;
    private string xmlName = "hyTask.xml";

    public void UpdateXmlNodeTaskInfo(ABTask.STATUS status, int A, int B, int carrier_id)
    {
        //����(���⣬����)
        shareRes.mutex.WaitOne();
        if (xmlDoc == null)
        {
            xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlName); //����xml�ļ�
            xroot = xmlDoc.SelectSingleNode("task");
        }

        XmlNodeList xnl = xroot.ChildNodes;

        xnl[0].InnerText = ((int)status).ToString();// task_status
        xnl[1].InnerText = A.ToString();// A
        xnl[2].InnerText = B.ToString();// B
        xnl[3].InnerText = carrier_id.ToString();// carrier_id

        xmlDoc.Save(xmlName);//���档
        //  �ͷ�
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
            xmlDoc.Load(xmlName); //����xml�ļ�
            xroot = xmlDoc.SelectSingleNode("task");
        }

        XmlNodeList xnl = xroot.ChildNodes;
        ABTask task = null;
        foreach (hyWorkFlow wf in workGroup.workFlowList)
        {
            if (wf.carrier.status == hyCarrier.STATUS.DOING_CHANGE_OVER)
            {// Ѱ�ҵ�ǰ���ڻ��͵ļо�
                if (wf.carrier.id == Convert.ToInt32(xnl[3].InnerText))
                {// ȷ���о�ID��һ�µģ����飩
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

    #region �ֶ�����

    /**
     *
     *       �ֶ�����
     *
     * */

    private ServoPoint manualDestPosDraw = new ServoPoint(0, 0);

    public void ManualUp()
    {
        // ----------------------------------------------------
        // ���ϵ�
        bool isOver = false;
        while (!isOver)
        {
            isOver = ManualZ_TO(zeroPos.Z, 1);       // �ϵ�
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
        // ���µ�
        bool isOver = false;
        while (!isOver)
        {
            isOver = ManualZ_TO(loadPoints[pos_index].Z, 1);       // �µ�
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
        // ˮƽ�����˶�
        bool isOver = false;
        while (!isOver)
        {
            isOver = ManualX_TO(loadPoints[pos_index].X, 50);     // ƽ�� --> A
            if (!drawOnly)
            {
                isOver &= ABTask.X_TO_MANUAL(true, ABTask.loadPoints[pos_index].X, ABTask.xSpeed);     // ƽ�� --> A
            }
            Thread.Sleep(10);
        }
    }

    private int PointsMap(int x, int x0, int x1, int y0, int y1)//����ģ��ͼ     �����1�޷����ôӺ���ǰ�Ĺ���*********

    {
        return (y1 - y0) * (x - x0) / (x1 - x0) + y0;
    }

    public void Pos2Pos()
    {
        // -----------------------------------------------------------------------------
        //  ʵ��װ�ص�λ
        int currPosX = GenericOp.xPos;
        int currPosZ = GenericOp.zPos;
        int i;
        for (i = 0; i < hyProcess.loadPointsNum; i++)
        {
            if (currPosX < ABTask.logicPoints[i].X)
            {// ������ǰ���Ŵ���ʲôλ��
                if (i == 0)
                {
                    currPosDraw.X = logicPoints[0].X;// ��Сλ��
                    currPosDraw.Z = 0;
                }
                else
                {
                    currPosDraw.X = PointsMap(currPosX, ABTask.logicPoints[i - 1].X, ABTask.logicPoints[i].X, logicPoints[i - 1].X, logicPoints[i].X);
                    currPosDraw.Z = 0;
                }
                break;// �ҵ���
            }
            else if (currPosX == ABTask.logicPoints[i].X)
            {
                currPosDraw.X = logicPoints[i].X;
                currPosDraw.Z = PointsMap(currPosZ, 0, ABTask.logicPoints[i].Z, 0, logicPoints[i].Z);
                break;// �ҵ���
            }
        }
        if (i == hyProcess.loadPointsNum)
        {
            currPosDraw.X = logicPoints[i - 1].X;// ���λ��
            currPosDraw.Z = 0;
        }
    }

    public void ManualClampRelax()
    {
        //
        bool isOver = false;
        while (!isOver)
        {
            //isOver = CLAMP_RELAX_DO();       // �ɿ�
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
            //isOver = CLAMP_DO();       // �н�
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
            return true;// �ֶ�����������ģ�⣩ʱ����ͣ����true ����������ִ�С�
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
            // �ȴ���ֱ���������
            return false;
        }
        return true;
    }

    private bool ManualX_TO(int xPos, int xSpeed)
    {
        if (GenericOp.EStop)
        {
            return true;// �ֶ�����������ģ�⣩ʱ����ͣ����true ����������ִ�С�
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
            // �ȴ���ֱ���������
            return false;
        }
        return true;
    }

    #endregion �ֶ�����
}