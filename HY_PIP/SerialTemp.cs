using System;
using System.IO.Ports;
using System.Threading;

namespace HY_PIP
{
    public class SerialTemp : SerialPort
    {
        private Thread threadRecv;
        private Thread threadSend;

        public enum COMM_STATE { IDLE, SEND, SENDING, RECV, RECVING };

        public static COMM_STATE commState = COMM_STATE.RECV;// 默认处于接受状态

        public SerialTemp(string portName, int baudRate)
        {
            this.PortName = portName;// 端口名
            this.BaudRate = baudRate;// 通信波特率

            threadRecv = new Thread(new ThreadStart(ThreadRecv)); //也可简写为new Thread(ThreadMethod);
            threadRecv.Start(); //启动线程

            threadSend = new Thread(new ThreadStart(ThreadSend)); //也可简写为new Thread(ThreadMethod);
            threadSend.Start(); //启动线程
        }

        // public void CloseSerial()
        // {
        //     this.Close();
        //   if (threadRecv != null)
        //    {
        //       threadRecv.Abort();
        // }
        //   if (threadSend != null)
        //    {
        //       threadSend.Abort();
        //   }
        //  }

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
          * RS485 解码
          *
          * 总共 20 字节
          */
        private Byte[] recvBuff = new Byte[59];
        private int RecvDataIndex = 0;
        private const int SendDataNum = 59;// 总数据长度。
        private const int RecvDataNum = 58;// 总数据长度。
        private const int PayloadDataNum = RecvDataNum - 4;// 除去 AA 55 A5 5A头4字节后的有效数据长度。

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
                        commState = COMM_STATE.RECV;// 成功收到数据，切换为通信接受状态

                        //获取水槽温度
                        GenericOp.temperature1 = BitConverter.ToInt16(recvBuff, 2) / 10;// 1号水槽温度
                        GenericOp.temperature2 = BitConverter.ToInt16(recvBuff, 4) / 10;// 2号水槽温度
                        GenericOp.temperature3 = BitConverter.ToInt16(recvBuff, 6) / 10;// 3号水槽温度

                        GenericOp.temperature41 = BitConverter.ToInt16(recvBuff, 8) / 10; //4号炉子上温区温度
                        GenericOp.temperature42 = BitConverter.ToInt16(recvBuff, 10) / 10; //4号炉子下温区温度

                        GenericOp.temperature51 = BitConverter.ToInt16(recvBuff, 12) / 10; //5号炉子上温区温度
                        GenericOp.temperature52 = BitConverter.ToInt16(recvBuff, 14) / 10; //5号炉子下温区温度

                        GenericOp.temperature61 = BitConverter.ToInt16(recvBuff, 16) / 10; //6号炉子上温区温度
                        GenericOp.temperature62 = BitConverter.ToInt16(recvBuff, 18) / 10; //6号炉子下温区温度

                        GenericOp.temperature71 = BitConverter.ToInt16(recvBuff, 20) / 10; //7号炉子上温区温度
                        GenericOp.temperature72 = BitConverter.ToInt16(recvBuff, 22) / 10; //7号炉子上温区温度

                        GenericOp.temperature111 = BitConverter.ToInt16(recvBuff, 24) / 10; //11号炉子上温区温度
                        GenericOp.temperature112 = BitConverter.ToInt16(recvBuff, 26) / 10; //11号炉子下温区温度
                        GenericOp.temperature121 = BitConverter.ToInt16(recvBuff, 28) / 10; //12号炉子上温区温度
                        GenericOp.temperature122 = BitConverter.ToInt16(recvBuff, 30) / 10; //12号炉子下温区温度
                        GenericOp.temperature131 = BitConverter.ToInt16(recvBuff, 32) / 10; //13号炉子温度
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

        public void ThreadSend()
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
        public static Byte[] sendBuff = new Byte[60];
        private int num = 100;

        private static void SendDataEncode()
        {
            // 帧头
            sendBuff[0] = 0x55;
            sendBuff[1] = 0xAA;
            byte sendbuff2, sendbuff3;
            string temp1;

            temp1 = Convert.ToString(GenericOp.temperature1_1, 16);
            if (temp1.Length > 2)
            {
                sendbuff2 = Convert.ToByte(temp1.Substring(0, temp1.Length - 2), 16);
            }
            else
            {
                sendbuff2 = 0x00;
            }
            if (GenericOp.temperature1_1 > 10)
            {
                sendbuff3 = Convert.ToByte(temp1.Substring(temp1.Length - 2), 16);
            }
            else
            {
                sendbuff3 = 0x00;
            }
            sendBuff[2] = (byte)(sendbuff2);//一号水槽
            sendBuff[3] = (byte)(sendbuff3);
            byte sendbuff4, sendbuff5;
            string temp2;

            temp2 = Convert.ToString(GenericOp.temperature2_1, 16);
            if (temp2.Length > 2)
            {
                sendbuff4 = Convert.ToByte(temp2.Substring(0, temp2.Length - 2), 16);
            }
            else
            {
                sendbuff4 = 0x00;
            }
            if (GenericOp.temperature2_1 > 10)
            {
                sendbuff5 = Convert.ToByte(temp2.Substring(temp2.Length - 2), 16);
            }
            else
            {
                sendbuff5 = 0x00;
            }
            sendBuff[4] = (byte)(sendbuff4);//2号水槽
            sendBuff[5] = (byte)(sendbuff5);
            byte sendbuff6, sendbuff7;
            string temp3;

            temp3 = Convert.ToString(GenericOp.temperature3_1, 16);
            if (temp3.Length > 2)
            {
                sendbuff6 = Convert.ToByte(temp3.Substring(0, temp3.Length - 2), 16);
            }
            else
            {
                sendbuff6 = 0x00;
            }
            if (GenericOp.temperature3_1 > 10)
            {
                sendbuff7 = Convert.ToByte(temp3.Substring(temp3.Length - 2), 16);
            }
            else
            {
                sendbuff7 = 0x00;
            }
            sendBuff[6] = (byte)(sendbuff6);//3号水槽
            sendBuff[7] = (byte)(sendbuff7);
            byte sendbuff8, sendbuff9;
            string temp4;

            temp4 = Convert.ToString(GenericOp.temperature4, 16);
            if (temp4.Length > 2)
            {
                sendbuff8 = Convert.ToByte(temp4.Substring(0, temp4.Length - 2), 16);
            }
            else
            {
                sendbuff8 = 0x00;
            }
            if (GenericOp.temperature4 > 10)
            {
                sendbuff9 = Convert.ToByte(temp4.Substring(temp4.Length - 2), 16);
            }
            else
            {
                sendbuff9 = 0x00;
            }
            sendBuff[8] = (byte)(sendbuff8);//预热炉上温区
            sendBuff[9] = (byte)(sendbuff9);
            sendBuff[10] = (byte)(sendbuff8);//预热炉下温区
            sendBuff[11] = (byte)(sendbuff9);
            byte sendbuff12, sendbuff13;
            string temp5;

            temp5 = Convert.ToString(GenericOp.temperature5, 16);
            if (temp5.Length > 2)
            {
                sendbuff12 = Convert.ToByte(temp5.Substring(0, temp5.Length - 2), 16);
            }
            else
            {
                sendbuff12 = 0x00;
            }
            if (GenericOp.temperature5 > 10)
            {
                sendbuff13 = Convert.ToByte(temp5.Substring(temp5.Length - 2), 16);
            }
            else
            {
                sendbuff13 = 0x00;
            }
            sendBuff[12] = (byte)(sendbuff12);//氮化炉1上温区
            sendBuff[13] = (byte)(sendbuff13);
            sendBuff[14] = (byte)(sendbuff12);//氮化炉1下温区
            sendBuff[15] = (byte)(sendbuff13);
            byte sendbuff16, sendbuff17;
            string temp6;

            temp6 = Convert.ToString(GenericOp.temperature6, 16);
            if (temp6.Length > 2)
            {
                sendbuff16 = Convert.ToByte(temp6.Substring(0, temp6.Length - 2), 16);
            }
            else
            {
                sendbuff16 = 0x00;
            }
            if (GenericOp.temperature6 > 10)
            {
                sendbuff17 = Convert.ToByte(temp6.Substring(temp6.Length - 2), 16);
            }
            else
            {
                sendbuff17 = 0x00;
            }
            sendBuff[16] = (byte)(sendbuff16);//氮化炉2上温区
            sendBuff[17] = (byte)(sendbuff17);
            sendBuff[18] = (byte)(sendbuff16);//氮化炉2下温区
            sendBuff[19] = (byte)(sendbuff17);
            byte sendbuff20, sendbuff21;
            string temp7;

            temp7 = Convert.ToString(GenericOp.temperature7, 16);
            if (temp7.Length > 2)
            {
                sendbuff20 = Convert.ToByte(temp7.Substring(0, temp7.Length - 2), 16);
            }
            else
            {
                sendbuff20 = 0x00;
            }
            if (GenericOp.temperature7 > 10)
            {
                sendbuff21 = Convert.ToByte(temp7.Substring(temp7.Length - 2), 16);
            }
            else
            {
                sendbuff21 = 0x00;
            }
            sendBuff[20] = (byte)(sendbuff20);//氧化炉上温区
            sendBuff[21] = (byte)(sendbuff21);
            sendBuff[22] = (byte)(sendbuff20);//氧化炉下温区
            sendBuff[23] = (byte)(sendbuff21);
            byte sendbuff24, sendbuff25;
            string temp11;

            temp11 = Convert.ToString(GenericOp.temperature11, 16);
            if (temp11.Length > 2)
            {
                sendbuff24 = Convert.ToByte(temp11.Substring(0, temp11.Length - 2), 16);
            }
            else
            {
                sendbuff24 = 0x00;
            }
            if (GenericOp.temperature11 > 10)
            {
                sendbuff25 = Convert.ToByte(temp11.Substring(temp11.Length - 2), 16);
            }
            else
            {
                sendbuff25 = 0x00;
            }
            sendBuff[24] = (byte)(sendbuff24);//离子稳定炉上温区
            sendBuff[25] = (byte)(sendbuff25);
            sendBuff[26] = (byte)(sendbuff24);//离子稳定炉下温区
            sendBuff[27] = (byte)(sendbuff25);
            byte sendbuff28, sendbuff29;
            string temp12;

            temp12 = Convert.ToString(GenericOp.temperature12, 16);
            if (temp12.Length > 2)
            {
                sendbuff28 = Convert.ToByte(temp12.Substring(0, temp12.Length - 2), 16);
            }
            else
            {
                sendbuff28 = 0x00;
            }
            if (GenericOp.temperature12 > 10)
            {
                sendbuff29 = Convert.ToByte(temp12.Substring(temp12.Length - 2), 16);
            }
            else
            {
                sendbuff29 = 0x00;
            }
            sendBuff[28] = (byte)(sendbuff28);//油炉上温区
            sendBuff[29] = (byte)(sendbuff29);
            sendBuff[30] = (byte)(sendbuff28);//油炉下温区
            sendBuff[31] = (byte)(sendbuff29);

            // 帧尾巴
            sendBuff[56] = 0x5A;
            sendBuff[57] = 0xA5;
        }
    }
}