using System;
using System.IO.Ports;
using System.Threading;

namespace HY_PIP
{
    public class SerialManual : SerialPort
    {
        private Thread threadRecv;
        private Thread threadSend;

        private enum COMM_STATE
        { IDLE, SEND, SENDING, RECV, RECVING };

        private COMM_STATE commState = COMM_STATE.SEND;// 默认处于发送状态

        public enum STOVE_LID_STATE { UNKNOWN = 0, OPENED = 1, CLOSED = 2, ERROR = 3 };

        public static STOVE_LID_STATE[] stoveLidState = new STOVE_LID_STATE[9];

        public SerialManual(string portName, int baudRate)
        {
            this.PortName = portName;// 端口名
            this.BaudRate = baudRate;// 通信波特率

            threadRecv = new Thread(new ThreadStart(ThreadRecv)); //也可简写为new Thread(ThreadMethod);
            threadRecv.Start(); //启动线程

            threadSend = new Thread(new ThreadStart(ThreadSend)); //也可简写为new Thread(ThreadMethod);
            threadSend.Start(); //启动线程
        }

        public void CloseSerial()
        {
            this.Close();
            if (threadRecv != null)
            {
                threadRecv.Abort();
            }
            if (threadSend != null)
            {
                threadSend.Abort();
            }
        }

        /**********************************************************
        *      数据接收线程
        *
         *
         *      串口发送接收的数据格式
         *              手动操作柜 --> 显示屏（将柜子上的各个按钮，接近开关的信号发出来，实际上发送的是X0 - X47)
         *              10字节：  AA 55 _ _  _ _   _ _ A5 5A
         *                        AA 55 (X0-7) (X10-17) (X20-27) (X30-37) (X40-47) (X50-X57) A5 5A
         *
         *
         *              显示屏 --> 手动操作柜
         *              8字节：
         *                      AA 55 _  _  _  _ A5 5A
         *
         *                      数据内容用于控制炉盖开、关。
         *                      按bit位：
         *                      bit0:1#炉盖开          M100
         *                      bit1:1#炉盖关          M101
         *                      bit2:2#    开          M102
         *                      bit3:2#    关。        M103
         *
         *
         *
        */

        private enum RECV_STATUS
        { STATE_IDLE, STATE_GOT_HEAD1, STATE_GOT_HEAD2, STATE_GOT_DATA, STATE_GOT_END1 }

        private RECV_STATUS recvStatus = RECV_STATUS.STATE_IDLE;

        private void ThreadRecv()
        {
            int rc = -1;
            while (true)
            {
                try
                {
                    if (this.IsOpen)
                    {
                        // 如果正在接收，那么就开始接收处理。
                        if ((commState == COMM_STATE.RECV) || (commState == COMM_STATE.RECVING))
                        {
                            rc = this.ReadByte();// 读取数据字节
                            RecvDataDecode((byte)(rc & 0xff));// 数据处理(解码）
                        }
                        else
                        {
                            Thread.Sleep(10);// 如果不处于正在接收状态，则休眠一段时间
                        }
                    }
                }
                catch (Exception e1)
                {
                    Console.WriteLine(e1.ToString());
                }
                finally
                {
                }
            }
        }

        /***********************************************************************************************
         * RS232 解码
         *
         * 总共 20 字节
         */
        private Byte[] recvBuff = new Byte[40];
        private int RecvDataIndex = 0;
        private const int SendDataNum = 8;
        private const int RecvDataNum = 10;// 总数据长度。
        private const int PayloadDataNum = RecvDataNum - 4;// 除去 AA 55 ... A5 5A 头尾4字节后的有效数据长度。

        private int automode_change_cntr_auto = 0;
        private int automode_change_cntr_manual = 0;

        private void RecvDataDecode(Byte inData)
        {
            // 逐个字节进行解析
            switch (recvStatus)
            {
                case RECV_STATUS.STATE_IDLE:
                    if (inData == 0xAA)
                    {
                        recvStatus = RECV_STATUS.STATE_GOT_HEAD1;// 获得了 HEAD1
                        commState = COMM_STATE.RECVING;// 状态转变为正在接收
                    }
                    break;

                case RECV_STATUS.STATE_GOT_HEAD1:
                    if (inData == 0x55)
                    {
                        recvStatus = RECV_STATUS.STATE_GOT_HEAD2;// 获得了 HEAD2
                        RecvDataIndex = 0;
                    }
                    else
                    {
                        recvStatus = RECV_STATUS.STATE_IDLE;// 错误，返回初始状态
                    }
                    break;

                case RECV_STATUS.STATE_GOT_HEAD2:
                    recvBuff[RecvDataIndex + 2] = inData;
                    RecvDataIndex++;
                    if (RecvDataIndex >= PayloadDataNum)
                    {
                        recvStatus = RECV_STATUS.STATE_GOT_DATA;// 获得了 数据
                        RecvDataIndex = 0;
                    }
                    break;

                case RECV_STATUS.STATE_GOT_DATA:
                    if (inData == 0xA5)
                    {
                        recvStatus = RECV_STATUS.STATE_GOT_END1;// 获得了 END1
                    }
                    else
                    {
                        recvStatus = RECV_STATUS.STATE_IDLE;// 错误，返回初始状态
                    }
                    break;

                case RECV_STATUS.STATE_GOT_END1:
                    if (inData == 0x5A)
                    {
                        recvStatus = RECV_STATUS.STATE_IDLE;// 重新恢复到IDLE状态
                        // 成功获得了所有数据
                        //recvBuff[0]
                        commState = COMM_STATE.IDLE;// 成功收到数据，切换为通信空闲状态
                        //
                        // X00 - X15
                        stoveLidState[0] = (STOVE_LID_STATE)(recvBuff[2] & 0x3);            // 1#炉盖 X0 - X01
                        stoveLidState[1] = (STOVE_LID_STATE)((recvBuff[2] >> 2) & 0x3);     // 2#炉盖
                        stoveLidState[2] = (STOVE_LID_STATE)((recvBuff[2] >> 4) & 0x3);     // 3#炉盖
                        stoveLidState[3] = (STOVE_LID_STATE)((recvBuff[2] >> 6) & 0x3);     // 4#炉盖

                        stoveLidState[4] = (STOVE_LID_STATE)(recvBuff[3] & 0x3);            // 5#炉盖
                        stoveLidState[5] = (STOVE_LID_STATE)((recvBuff[3] >> 2) & 0x3);     // 6#炉盖
                        stoveLidState[6] = (STOVE_LID_STATE)((recvBuff[3] >> 4) & 0x3);     // 7#炉盖
                        stoveLidState[7] = (STOVE_LID_STATE)(recvBuff[4] & 0x3); //后门
                        stoveLidState[8] = (STOVE_LID_STATE)((recvBuff[4] >> 2) & 0x3); //前门

                        // X30 自动 X31手动
                        //if ((recvBuff[5] & 0x01) == 0)
                        //{
                        //automode_change_cntr_manual = 0;
                        /*
                        automode_change_cntr_auto++;
                        if (automode_change_cntr_auto < 10)
                        {
                            GenericOp.AutoMode = true;// 自动模式
                        }
                        else
                        {
                            automode_change_cntr_auto = 10;
                        }*/
                        // }
                        //   else
                        // {
                        //automode_change_cntr_auto = 0;
                        //    automode_change_cntr_manual++;
                        //   if (automode_change_cntr_manual < 10)
                        //   {
                        //        GenericOp.AutoMode = false;// 手动模式
                        //    }
                        //    else
                        //     {
                        //       automode_change_cntr_manual = 10;
                        //    }
                        //   }

                        // 手动控制柜用于控制龙门的前进、后退、上下、抓放，对应PLC的X41 - X46
                        GenericOp.manual_gtry_action = GenericOp.GTRY_ACTION_TYPE.NONE;// 初始为不做任何动作
                        //if ((recvBuff[6] & 0x02) !=0) GenericOp.manual_gtry_action = GenericOp.GTRY_ACTION_TYPE.FORWARD;      // X41
                        //else if ((recvBuff[6] & 0x04) !=0) GenericOp.manual_gtry_action = GenericOp.GTRY_ACTION_TYPE.BAKWARD; // X42
                        //else if ((recvBuff[6] & 0x08) != 0) GenericOp.manual_gtry_action = GenericOp.GTRY_ACTION_TYPE.UP;        // X43
                        //else if ((recvBuff[6] & 0x10) != 0) GenericOp.manual_gtry_action = GenericOp.GTRY_ACTION_TYPE.DOWN;// X44
                        //else if ((recvBuff[6] & 0x20) != 0) GenericOp.manual_gtry_action = GenericOp.GTRY_ACTION_TYPE.CLAMP;// X45
                        //else if ((recvBuff[6] & 0x40) != 0) GenericOp.manual_gtry_action = GenericOp.GTRY_ACTION_TYPE.CLAMP_RELAX;// X46

                        //GenericOp.EStop_Manual = ((recvBuff[6] >> 7) == 0);// X47  急停信号
                    }
                    else
                    {
                        recvStatus = RECV_STATUS.STATE_IDLE;// 错误，返回初始状态
                    }
                    break;
            }
        }

        /***********************************************************************************************
         * Thread Send
         */

        private void ThreadSend()
        {
            int commCntr = 0;
            int commErrCntr = 0;
            int commErr2Cntr = 0;
            while (true)
            {
                try
                {
                    //
                    if (this.IsOpen)
                    {
                        if (commState == COMM_STATE.IDLE)
                        {
                            commState = COMM_STATE.SEND;// 转入发送状态

                            commErrCntr = 0;
                            commErr2Cntr = 0;

                            Thread.Sleep(200);
                            // 在一次成功发送以后，需要对发送数据进行清零。
                            //SendCmdReset();// 对命令进行清除，避免重复发送同一命令
                        }
                        else if (commState == COMM_STATE.SEND)
                        {
                            commState = COMM_STATE.SENDING;

                            SendDataEncode();// 数据编码
                            this.Write(sendBuff, 0, SendDataNum);// 发送数据

                            commCntr = 0;
                            commState = COMM_STATE.RECV;// 转入接收状态
                        }
                        else
                        {
                            Thread.Sleep(10);// 休眠 10ms

                            // 处于通信接收状态 COMM_STATE.RECV;
                            if (commState == COMM_STATE.RECVING)
                            {
                                commCntr++;
                                if (commCntr > 10)
                                {// 这种情况一般是接收到了数据，但是接收的数据由错误。此时应该立刻重新发送数据
                                    // 如果 5 * 10ms  = 50ms 没有接收到数据
                                    commState = COMM_STATE.SEND;// 重新发送
                                    commErrCntr++;
                                }
                            }
                            if (commState == COMM_STATE.RECV)
                            {
                                commErr2Cntr++;
                                if (commErr2Cntr % 100 == 0)// 固定1秒发送一次数据
                                {// 这种情况一般是发送了数据，但是过了1秒还没有接收到数据，那么重发该数据。
                                    commState = COMM_STATE.SEND;// 重新发送
                                }
                                else if (commErr2Cntr > 1000)// 1000*10ms = 10秒钟
                                {// 这种情况一般是重发了N次以后，依然还是收不到数据，说明通信彻底断了，重置数据。
                                    commErr2Cntr = 0;
                                    //SendCmdReset();// 通信线断了，那么这时候清除所有命令，避免因为长时间通信断后，恢复，机器突然动作。
                                }
                            }
                        }
                    }
                }
                catch (Exception e1)
                {
                    Console.WriteLine(e1.ToString());
                }
                finally
                {
                }
            }
        }

        /***********************************************************************************************
         * RS232 编码
         */
        public Byte[] sendBuff = new Byte[40];

        private void SendDataEncode()
        {
            // 帧头
            sendBuff[0] = 0xAA;
            sendBuff[1] = 0x55;

            sendBuff[2] = (byte)stoveCmd;
            sendBuff[3] = (byte)(stoveCmd >> 8);
            sendBuff[4] = (byte)(stoveCmd >> 16);//M018开始
            sendBuff[5] = (byte)(stoveCmd >> 24);

            // 帧尾巴
            sendBuff[6] = 0xA5;
            sendBuff[7] = 0x5A;
        }

        // 炉盖打开与关闭操作
        private static int stoveCmd = 0;

        public static void OpenStoveLid(int index)
        {
            // 打开炉盖
            int pos = index * 2;
            stoveCmd &= ~(3 << pos);
            stoveCmd |= 1 << pos;// 打开
        }

        public static void CloseStoveLid(int index)
        {
            // 关闭炉盖
            int pos = index * 2;
            stoveCmd &= ~(3 << pos);
            stoveCmd |= 2 << pos;// 关闭
        }

        public static void StopStoveLidCmd(int index)
        {
            // 停止炉盖（打开关闭）动作命令
            int pos = index * 2;
            stoveCmd &= ~(3 << pos);
        }

        public static void ClearStoveCmd()
        {
            // 清除炉盖（打开关闭）命令
            stoveCmd = 0;
        }
    }
}