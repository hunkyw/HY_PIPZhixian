using System;
using System.IO.Ports;
using System.Threading;

namespace HY_PIP
{
    public class SerialWireless : SerialPort
    {
        private Thread threadRecv;
        private Thread threadSend;

        private static int cmd_cntr;

        public static GTYRY_CMD gtryCmd
        {
            set
            {
                if (gtry_cmd_ == GTYRY_CMD.STOP)
                {
                    cmd_cntr = 0;
                    if (value != GTYRY_CMD.STOP_RELEASE)
                    {
                        return;// 如果急停状态，如果没有松开急停，那么不接受其他的命令。
                    }
                }
                else if (gtry_cmd_ == GTYRY_CMD.STOP_RELEASE)
                {
                    if ((value >= GTYRY_CMD.X_RELATIVE) && (value <= GTYRY_CMD.CLAMP_RELAX))
                    {
                        cmd_cntr++;
                        if (cmd_cntr < 100)
                        {
                            return;
                        }
                    }
                }

                if (gtry_cmd_ == GTYRY_CMD.PAUSE)
                {
                    if (value != GTYRY_CMD.PAUSE_RELEASE)
                    {
                        return;// 如果暂停状态，如果没有松开暂停，那么不接受其他的命令。
                    }
                }
                gtry_cmd_ = value;
            }
            get { return gtry_cmd_; }
        }

        private static GTYRY_CMD gtry_cmd_ = GTYRY_CMD.NONE;

        private GTYRY_CMD gtryCmdDoing = GTYRY_CMD.NONE;// 正在执行的龙门指令

        public enum GTYRY_CMD
        {
            NONE = 0, X_RELATIVE = 1, X_ABSOLUTE = 2, X_ZRN = 3, Z_RELATIVE = 4, Z_ABSOLUTE = 5, Z_ZRN = 6,
            CLAMP = 10, CLAMP_RELAX = 11,
            GREASE = 20,
            XPOS_SET_ZREO = 101, ZPOS_SET_ZERO = 102,
            PAUSE_RELEASE = 126, STOP_RELEASE = 127,
            CMD_CANCEL = -3, PAUSE = -2, STOP = -1
        };

        public static int gtryPos = 0;
        public static int gtrySpeed = 0;

        private enum COMM_STATE
        { IDLE, SEND, SENDING, RECV, RECVING };

        private COMM_STATE commState = COMM_STATE.SEND;// 默认处于发送状态

        //public static int xPos;
        //public static int zPos;
        public const int pulse_per_mm = 200;

        private static UInt32 gtryState;
        public const UInt32 GTRY_STATE_CLAMP = 0x00000001;// 龙门夹爪闭合
        public const UInt32 GTRY_STATE_CLAMP_RELAX = 0x00000002;// 龙门夹爪松开
        public const UInt32 GTRY_STATE_ESTOP_STATE = 0x80000000;// 龙门急停状态
        public const UInt32 GTRY_STATE_REQUEST_ZRN = 0x40000000;// 龙门请求圆点回归

        public SerialWireless(string portName, int baudRate)
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
        private const int SendDataNum = 20;// 总数据长度。
        private const int RecvDataNum = 20;// 总数据长度。
        private const int PayloadDataNum = RecvDataNum - 4;// 除去 AA 55 ... A5 5A 头尾4字节后的有效数据长度。
        private int gtry_estop_state_cntr = 0;
        private int gtry_request_zrn_cntr = 0;

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
                        GenericOp.xPos = 5 * BitConverter.ToInt16(recvBuff, 4);// 转化为 mm 单位
                        GenericOp.zPos = 5 * BitConverter.ToInt16(recvBuff, 6);// 转化为 mm 单位
                        GenericOp.xPos_servo = -5 * BitConverter.ToInt16(recvBuff, 8);// 转化为 mm 单位
                        GenericOp.zPos_servo = -5 * BitConverter.ToInt16(recvBuff, 10);// 转化为 mm 单位
                        gtryState = BitConverter.ToUInt32(recvBuff, 12);// 龙门机构状态
                        GenericOp.weight = (int)BitConverter.ToInt16(recvBuff, 16);// 载重量

                        // 龙门机构的急停状态的获取
                        if (SerialWireless.GetGtryState(SerialWireless.GTRY_STATE_ESTOP_STATE))
                        {
                            gtry_estop_state_cntr++;
                            if (gtry_estop_state_cntr % 20 == 0)// 每20包数据，检查一下PLC的急停状态
                            {
                                GenericOp.EStop_Soft = true;// 根据 龙门急停状态 给  软急停标识  赋值，只赋值10次就不重复赋值。为了避免反复被刷新。
                            }
                        }
                        else
                        {
                            //gtry_estop_state_cntr = 0;
                        }

                        // 龙门原点回归请求
                        if (SerialWireless.GetGtryState(SerialWireless.GTRY_STATE_REQUEST_ZRN))
                        {
                            gtry_request_zrn_cntr++;
                            if (gtry_request_zrn_cntr > 5)
                            {
                                gtry_request_zrn_cntr = 5;
                                GenericOp.RequestZRN = true;// 请求原点回归标识  置位
                            }
                        }
                        else
                        {
                            gtry_request_zrn_cntr = 0;
                            GenericOp.RequestZRN = false;
                        }
                    }
                    else
                    {
                        recvStatus = RECV_STATUS.STATE_IDLE;// 错误，返回初始状态
                    }
                    break;
            }
        }

        public static bool GetGtryState(UInt32 state_to_get)
        {
            return (gtryState & state_to_get) != 0;
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

                            // 在一次成功发送以后，需要对发送数据进行清零。
                            SendCmdReset();// 对命令进行清除，避免重复发送同一命令

                            //Thread.Sleep(50);
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
                                /*if (commErrCntr == 50)// 只执行一次
                                {
                                    //SendCmdReset();// 通信线断了，那么这时候清除所有命令，避免因为长时间通信断后，恢复，机器突然动作。
                                }*/
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
                                    SendCmdReset();// 通信线断了，那么这时候清除所有命令，避免因为长时间通信断后，恢复，机器突然动作。
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

            if (gtryCmd == GTYRY_CMD.CLAMP_RELAX)
            {
                Console.WriteLine("aa");
            }
            gtryCmdDoing = gtryCmd;// 正在执行的龙门指令

            sendBuff[2] = (byte)((int)gtryCmdDoing & 0xff);
            sendBuff[3] = (byte)(((int)gtryCmdDoing >> 8) & 0xff);

            // 伺服运动命令
            if ((gtryCmdDoing <= GTYRY_CMD.Z_ZRN) && (gtryCmdDoing >= GTYRY_CMD.X_RELATIVE))
            {
                int tmpGtryPos = gtryPos * pulse_per_mm;
                int tmpGtrySpeed = gtrySpeed * pulse_per_mm;

                sendBuff[4] = (byte)(tmpGtryPos & 0xff);
                sendBuff[5] = (byte)((tmpGtryPos >> 8) & 0xff);
                sendBuff[6] = (byte)((tmpGtryPos >> 16) & 0xff);
                sendBuff[7] = (byte)((tmpGtryPos >> 24) & 0xff);

                sendBuff[8] = (byte)(tmpGtrySpeed & 0xff);
                sendBuff[9] = (byte)((tmpGtrySpeed >> 8) & 0xff);
                sendBuff[10] = (byte)((tmpGtrySpeed >> 16) & 0xff);
                sendBuff[11] = (byte)((tmpGtrySpeed >> 24) & 0xff);
            }
            else if ((gtryCmdDoing >= GTYRY_CMD.XPOS_SET_ZREO) && (gtryCmdDoing <= GTYRY_CMD.ZPOS_SET_ZERO))
            {
                int tmpGtryPos = gtryPos;
                int tmpGtrySpeed = gtrySpeed;

                sendBuff[4] = (byte)(tmpGtryPos & 0xff);
                sendBuff[5] = (byte)((tmpGtryPos >> 8) & 0xff);
                sendBuff[6] = (byte)((tmpGtryPos >> 16) & 0xff);
                sendBuff[7] = (byte)((tmpGtryPos >> 24) & 0xff);

                sendBuff[8] = (byte)(tmpGtrySpeed & 0xff);
                sendBuff[9] = (byte)((tmpGtrySpeed >> 8) & 0xff);
                sendBuff[10] = (byte)((tmpGtrySpeed >> 16) & 0xff);
                sendBuff[11] = (byte)((tmpGtrySpeed >> 24) & 0xff);
            }
            else
            {
                sendBuff[0] = sendBuff[0];
            }

            // 帧尾巴
            sendBuff[18] = 0xA5;
            sendBuff[19] = 0x5A;
        }

        // 发送命令的重置
        private void SendCmdReset()
        {
            if (gtryCmdDoing == GTYRY_CMD.STOP) return;
            if (gtryCmdDoing == GTYRY_CMD.PAUSE) return;

            if (gtryCmdDoing != GTYRY_CMD.NONE)
            {
                gtryCmdDoing = GTYRY_CMD.NONE;
                gtryCmd = GTYRY_CMD.NONE;
            }
        }
    }
}