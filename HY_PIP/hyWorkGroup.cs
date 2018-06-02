using HY_PIP.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace HY_PIP
{
    /*
     * 工作组 WorkGroup
     *
     * 每一天的所有工作流，我们都归为一个工作组
     *
     *              一个工作组，包含了N多工作流
     *
     * 每次开机，都会检查文件系统，提示用户是否创建新的工作组（也就是询问是不是新的一天的开始）
     *
     * 如果新的一天开始，那么会创建一个新的工作组
     * 如果还是当天，那么会读取当前文件系统信息，恢复当前工作组的所有工作流的显示。
     *
     * 当然，用户可以选择回溯到历史工作组，查看历史工作情况。
     *
     * */

    public class hyWorkGroup
    {
        private XmlDocument xmlDoc;
        private XmlNode xroot;
        private string xmlName;
        public List<hyWorkFlow> workFlowList = new List<hyWorkFlow>();// 创建工作流列表
        private hyWorkFlow currWorkFlow;
        private hyWorkFlow lastWorkFlow;

        public List<hyCarrier> carrierList = new List<hyCarrier>();

        public DateTime createdTime = DateTime.Now;// 工作组 创建时间

        public int[] max_station_endingTime = new int[hyProcess.stationNum];// 工作流结束
        private int max_workflow_id_assigned = 0;

        public hyWorkGroup()
        {
        }

        /**
         * 新增一个工作组
         * */

        public void NewWorkGroup(string dfname)
        {
            xmlName = dfname;
            // 工作组由一系列工作流组成,
            // 新建一个工作流列表
            xmlDoc = new XmlDocument();
            try
            {
                FileStream fs = new FileStream(xmlName, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs);
                //开始写入
                sw.Write(HY_PIP.Properties.Resources.hyData);// 将 hyData.xml 模板文件的内容提取出来。
                //清空缓冲区
                sw.Flush();
                //关闭流
                sw.Close();
                fs.Close();
            }
            catch
            {
            }

            xmlDoc.Load(xmlName); //加载xml文件
            xroot = xmlDoc.SelectSingleNode("workgroup");

            XmlNodeList xnl1 = xroot.ChildNodes;

            XmlElement xre = (XmlElement)xroot;
            xre.SetAttribute("created", DateTime.Now.ToString("yyyy/MM/dd/HH/mm"));
            string strTime = xre.GetAttribute("created");
            string[] strTime2 = strTime.Split('/');
            createdTime = new DateTime(Convert.ToInt32(strTime2[0]), Convert.ToInt32(strTime2[1]), Convert.ToInt32(strTime2[2]), Convert.ToInt32(strTime2[3]), Convert.ToInt32(strTime2[4]), 0);//Convert.ToInt32();// 表格创建时间
            xmlDoc.Save(xmlName);
            //
            workFlowList.Clear();//  清空工作列表
            // 之后，再根据现场实际操作，实时添加工作流。
        }

        /**
         *
         *
         * 加载 workGroup 数据文件
         * 读取数据文件
         *
         * */

        public void LoadWorkGroup(string dfname)
        {
            LoadXml(dfname);
        }

        private List<int> changeTimeList = new List<int>();

        private void UpdateChangeOverTime()
        {
            changeTimeList.Clear();

            foreach (hyWorkFlow wf in workFlowList)
            {
                foreach (hyStationPara stationPara in wf.process.stationParaList)
                {
                    if (stationPara.enabled)
                    {
                        changeTimeList.Add(stationPara.startingTimeWithHead);
                        changeTimeList.Add(stationPara.endingTime);
                    }
                }
            }
        }

        /**
         * 新增一个工作流
         * */

        public void NewWorkFlow(int process_id, int carrier_name, int person_id, int loading_station_id)
        {
            lastWorkFlow = currWorkFlow;
            currWorkFlow = new hyWorkFlow(this);
            max_workflow_id_assigned++;
            currWorkFlow.process_id = process_id;// 工艺ID
            currWorkFlow.carrier.name = carrier_name;// 夹具名称
            currWorkFlow.person_id = person_id;// 创建人员ID

            currWorkFlow.workflow_id = max_workflow_id_assigned;// 工作流ID，自动生成
            currWorkFlow.carrier_id = max_workflow_id_assigned;// 夹具ID，自动生成
            currWorkFlow.loading_station_id = loading_station_id;// 取料工位ID，备用

            hyProcess rawProcess = null;
            foreach (hyProcess process in MainForm.processGroup.processList)
            {
                rawProcess = process;
                if (process.process_id == process_id)
                {
                    currWorkFlow.process.process_name = process.process_name;
                    int ii = 0;
                    foreach (hyStationPara stationPara in process.stationParaList)
                    {
                        currWorkFlow.process.stationParaList[ii].station_id = stationPara.station_id;
                        currWorkFlow.process.stationParaList[ii].workingTemp = stationPara.workingTemp;// 工作温度
                        currWorkFlow.process.stationParaList[ii].workingTime = stationPara.workingTime;// 工作时间
                        if (currWorkFlow.process.stationParaList[ii].workingTime > 0)
                        {
                            switch (ii)
                            {
                                case 0:
                                    GenericOp.temperature1_1 = (currWorkFlow.process.stationParaList[ii].workingTemp);
                                    break;

                                case 2:
                                    GenericOp.temperature2_1 = (currWorkFlow.process.stationParaList[ii].workingTemp);
                                    break;

                                case 3:
                                    GenericOp.temperature4 = (currWorkFlow.process.stationParaList[ii].workingTemp);
                                    break;

                                case 4:
                                    GenericOp.temperature5 = (currWorkFlow.process.stationParaList[ii].workingTemp);
                                    break;

                                case 5:
                                    GenericOp.temperature6 = (currWorkFlow.process.stationParaList[ii].workingTemp);
                                    break;

                                case 6:
                                    GenericOp.temperature7 = (currWorkFlow.process.stationParaList[ii].workingTemp);
                                    break;

                                case 9:
                                    GenericOp.temperature3_1 = (currWorkFlow.process.stationParaList[ii].workingTemp);
                                    break;

                                case 10:
                                    GenericOp.temperature11 = (currWorkFlow.process.stationParaList[ii].workingTemp);
                                    break;

                                case 11:
                                    GenericOp.temperature12 = (currWorkFlow.process.stationParaList[ii].workingTemp);
                                    break;

                                default:
                                    break;
                            }
                        }

                        ii++;
                    }
                    SerialTemp.commState = SerialTemp.COMM_STATE.IDLE;
                    Thread.Sleep(100);
                    SerialTemp.commState = SerialTemp.COMM_STATE.IDLE;
                    Thread.Sleep(100);
                    SerialTemp.commState = SerialTemp.COMM_STATE.IDLE;
                    Thread.Sleep(100);
                    SerialTemp.commState = SerialTemp.COMM_STATE.IDLE;
                    break;
                }
            }
            if (rawProcess == null)
            {
                MessageBox.Show("新增工艺出错，没有找到匹配的工艺号！");
                return;
            }
            // 根据 工艺ID (process id)，读取工艺参数文件
            int stationIndex = 0;
            //bool isHead = true;
            //int iii = 0;
            currWorkFlow.max_workflow_endingtime = 0;
            // -------------------------------------------------------------------------
            // 生成一个工作流序列，原始工作序列。
            foreach (hyStationPara stationPara in currWorkFlow.process.stationParaList)
            {
                stationPara.station_id = stationIndex;
                // -------------------------------------------------------------------------
                // 生成一个工作流序列，原始工作序列。
                if (stationPara.enabled)
                {
                    currWorkFlow.max_workflow_endingtime = Math.Max(currWorkFlow.max_workflow_endingtime, MainForm.SystemMinutes);// 这里每一行的最大允许时间永远是在最新时间之后的。
                    stationPara.startingTimeWithHead = currWorkFlow.max_workflow_endingtime;// 开始时间 hyProcess.interval 5 分钟间隔时间
                    stationPara.endingTime = stationPara.startingTimeWithHead + stationPara.workingTimeWithHead;// 结束时间
                    currWorkFlow.max_workflow_endingtime = stationPara.endingTime;// 更新总结束时间
                }
                stationIndex++;
            }

            // -------------------------------------------------------------------------
            // 将序列添加到 workGroup 中去。但是要做以下检查：1）紧跟上一个工作流后边，2）避免和以前任何工作流的切换时间发生冲突
            // 也就是寻找当前工作流的位置
            if (lastWorkFlow != null)
            {
                int max = 0;
                for (int i = 0; i < hyProcess.stationNum; i++)
                {
                    hyStationPara curr = currWorkFlow.process.stationParaList[i];
                    hyStationPara last = lastWorkFlow.process.stationParaList[i];
                    if (curr.enabled)
                    {
                        this.max_station_endingTime[i] = Math.Max(this.max_station_endingTime[i], MainForm.SystemMinutes);// 这里每一行的最大允许时间永远是在最新时间之后的。
                        int a = (this.max_station_endingTime[i] - curr.startingTimeWithHead);
                        max = Math.Max(max, a);
                    }
                }
                for (int i = 0; i < hyProcess.stationNum; i++)
                {
                    hyStationPara stationPara = currWorkFlow.process.stationParaList[i];
                    if (stationPara.enabled)
                    {
                        stationPara.startingTimeWithHead += max;
                        stationPara.endingTime = stationPara.startingTimeWithHead + stationPara.workingTimeWithHead;// 结束时间
                        currWorkFlow.max_workflow_endingtime = stationPara.endingTime;// 更新总结束时间
                    }
                }
            }

            // -------------------------------------------------------------------------
            // 开始时间是否与切换时间冲突，检查
            bool checkPass = false;
            while (!checkPass)
            {
                for (stationIndex = 0; stationIndex < hyProcess.stationNum; stationIndex++)
                {
                    hyStationPara stationPara = currWorkFlow.process.stationParaList[stationIndex];
                    if (stationPara.enabled)
                    {
                        foreach (int changTime in changeTimeList)
                        {
                            int interval = stationPara.startingTimeWithHead - changTime;
                            if (Math.Abs(interval) < hyProcess.interval_m)
                            {
                                int delay = hyProcess.interval_m - interval;
                                for (int i = 0; i < hyProcess.stationNum; i++)
                                {
                                    stationPara = currWorkFlow.process.stationParaList[i];
                                    stationPara.startingTimeWithHead += delay;// 加一个大的量。加小了有问题。
                                    stationPara.endingTime = stationPara.startingTime + stationPara.workingTime;// 结束时间
                                    currWorkFlow.max_workflow_endingtime = stationPara.endingTime;// 更新总结束时间
                                }
                                stationIndex = -1;// 有冲突，重新来过
                                break;// 有冲突，重新来过
                            }
                        }
                    }
                }
                checkPass = true;
            }

            int j = 0;
            foreach (hyStationPara stationPara in currWorkFlow.process.stationParaList)
            {
                if (stationPara.enabled)
                {
                    this.max_station_endingTime[j] = Math.Max(this.max_station_endingTime[j], stationPara.endingTime);// 更新每一行的总结束时间
                }
                j++;
            }
            currWorkFlow.carrier.UpdateCarrierInfo(hyWorkFlow.POS_LOAD);// 更新夹具信息
            workFlowList.Add(currWorkFlow);// 添加工作流

            UpdateChangeOverTime();     // 更新 换型过度时间
            // 插入 数据 XML
            InsertXmlNode();
        }

        /**
         * 修改数据文件
         * */

        private void UpdateDataFile()
        {
        }

        /*
         *
         * 从 xml 数据中  读取一个已经存在的工作组（N个工作流的组合）
         *
         * */

        private void LoadXml(string dfname)
        {
            xmlName = dfname;
            xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlName); //加载xml文件
            xroot = xmlDoc.SelectSingleNode("workgroup");

            XmlNodeList xnl1 = xroot.ChildNodes;

            XmlElement xre = (XmlElement)xroot;
            string strTime = xre.GetAttribute("created");
            string[] strTime2 = strTime.Split('/');
            createdTime = new DateTime(Convert.ToInt32(strTime2[0]), Convert.ToInt32(strTime2[1]), Convert.ToInt32(strTime2[2]), Convert.ToInt32(strTime2[3]), Convert.ToInt32(strTime2[4]), 0);//Convert.ToInt32();// 表格创建时间

            foreach (XmlNode xn1 in xnl1)
            {// workflow
                // 每一个工作流，都包含关联一个工艺
                // 每个工艺中，都包含了13个station(炉子，水槽）的参数.station para
                hyWorkFlow workFlow = currWorkFlow = new hyWorkFlow(this);

                XmlElement xe1 = (XmlElement)xn1;
                workFlow.workflow_id = Convert.ToInt32(xe1.GetAttribute("workflow_id"));// 工作流ID
                workFlow.process_id = Convert.ToInt32(xe1.GetAttribute("process_id"));// 工艺ID
                workFlow.person_id = Convert.ToInt32(xe1.GetAttribute("person_id"));// 创建人员ID

                workFlow.carrier_id = Convert.ToInt32(xe1.GetAttribute("carrier_id"));// 夹具ID
                workFlow.carrier.name = Convert.ToInt32(xe1.GetAttribute("carrier_name"));// 夹具名称
                workFlow.carrier.status = (hyCarrier.STATUS)(Convert.ToInt32(xe1.GetAttribute("carrier_status")));// 夹具状态
                workFlow.carrier.pos = Convert.ToInt32(xe1.GetAttribute("carrier_pos"));// 夹具pos

                max_workflow_id_assigned = Math.Max(max_workflow_id_assigned, workFlow.workflow_id);// 已经分配的最大ID

                XmlNodeList xnl2 = xe1.ChildNodes;
                int index = 0;
                foreach (XmlNode xn2 in xnl2)
                {//staion
                    XmlElement xe2 = (XmlElement)xn2;
                    index = Convert.ToInt32(xe2.GetAttribute("station_id"));
                    workFlow.process.stationParaList[index].workingTime = Convert.ToInt32(Encript.Decode(xe2.ChildNodes.Item(0).InnerText));// 工作时间  加密
                    workFlow.process.stationParaList[index].workingTemp = Convert.ToInt32(Encript.Decode(xe2.ChildNodes.Item(1).InnerText));// 工作温度  加密
                    workFlow.process.stationParaList[index].startingTime = Convert.ToInt32(Encript.Decode(xe2.ChildNodes.Item(2).InnerText));// 起始时间  加密
                    workFlow.process.stationParaList[index].endingTime = Convert.ToInt32(Encript.Decode(xe2.ChildNodes.Item(3).InnerText));//结束时间  加密
                                                                                                                                           //
                    if (workFlow.process.stationParaList[index].workingTemp > 0)
                    {
                        switch (index)
                        {
                            case 0:
                                GenericOp.temperature1_1 = (workFlow.process.stationParaList[index].workingTemp);
                                break;

                            case 2:
                                GenericOp.temperature2_1 = (workFlow.process.stationParaList[index].workingTemp);
                                break;

                            case 3:
                                GenericOp.temperature4 = (workFlow.process.stationParaList[index].workingTemp);
                                break;

                            case 4:
                                GenericOp.temperature5 = (workFlow.process.stationParaList[index].workingTemp);
                                break;

                            case 5:
                                GenericOp.temperature6 = (workFlow.process.stationParaList[index].workingTemp);
                                break;

                            case 6:
                                GenericOp.temperature7 = (workFlow.process.stationParaList[index].workingTemp);
                                break;

                            case 9:
                                GenericOp.temperature3_1 = (workFlow.process.stationParaList[index].workingTemp);
                                break;

                            case 10:
                                GenericOp.temperature11 = (workFlow.process.stationParaList[index].workingTemp);
                                break;

                            case 11:
                                GenericOp.temperature12 = (workFlow.process.stationParaList[index].workingTemp);
                                break;

                            default:
                                break;
                        }
                    }
                    if (workFlow.process.stationParaList[index].workingTime != 0)
                    {
                        this.max_station_endingTime[index] = workFlow.process.stationParaList[index].endingTime;// 更新每一行的总结束时间
                        workFlow.max_workflow_endingtime = workFlow.process.stationParaList[index].endingTime;// 更新总结束时间
                    }
                }
                SerialTemp.commState = SerialTemp.COMM_STATE.IDLE;
                Thread.Sleep(100);
                SerialTemp.commState = SerialTemp.COMM_STATE.IDLE;
                Thread.Sleep(100);
                SerialTemp.commState = SerialTemp.COMM_STATE.IDLE;
                Thread.Sleep(100);
                SerialTemp.commState = SerialTemp.COMM_STATE.IDLE;
                if ((workFlow.carrier.pos >= hyWorkFlow.POS_FIRST_STATION) && (workFlow.carrier_pos <= hyWorkFlow.POS_LAST_STATION))
                {//  从 POS_LOAD 到 POS_LAST_STATION 都是有endingTime的。
                    workFlow.carrier.endingTime = workFlow.process.stationParaList[workFlow.carrier.pos].endingTime;// 更新carrier结束时间
                }
                if (workFlow.carrier.pos == hyWorkFlow.POS_LOAD)
                {
                    workFlow.carrier.endingTime = workFlow.process.stationParaList[hyWorkFlow.POS_FIRST_STATION].startingTime;
                }
                workFlowList.Add(workFlow);
            }

            UpdateChangeOverTime();     // 更新 换型过度时间
        }

        /*
         *
         * 在 xml 数据中  追加（插入）数据。
         *
         * */

        private void InsertXmlNode()
        {
            /**
             * 插入一个工作流
             * <workflow>
             * */
            XmlElement xe1 = xmlDoc.CreateElement("workflow");//创建一个﹤workflow﹥节点
            xe1.SetAttribute("workflow_id", currWorkFlow.workflow_id.ToString());// 工作流ID
            xe1.SetAttribute("process_id", currWorkFlow.process_id.ToString());// 工艺ID
            xe1.SetAttribute("person_id", currWorkFlow.person_id.ToString());// 创建人员ID
            xe1.SetAttribute("carrier_id", currWorkFlow.carrier_id.ToString());// 夹具ID
            xe1.SetAttribute("carrier_name", currWorkFlow.carrier.name.ToString());// 夹具名称
            xe1.SetAttribute("carrier_status", ((int)currWorkFlow.carrier.status).ToString());// 夹具状态
            xe1.SetAttribute("carrier_pos", currWorkFlow.carrier.pos.ToString());//夹具pos

            /**
             * 循环插入station
             * <station>
             * */
            foreach (hyStationPara stationPara in currWorkFlow.process.stationParaList)
            {
                XmlElement xe2 = xmlDoc.CreateElement("station");
                xe2.SetAttribute("station_id", stationPara.station_id.ToString());// 工位ID

                /**
                 * <working_time>,<workint_temp>,<starting_time>,<ending_time>
                 * */
                XmlElement xe3_1 = xmlDoc.CreateElement("working_time");
                xe3_1.InnerText = Encript.Encode(stationPara.workingTime.ToString());// 工作时间   加密
                xe2.AppendChild(xe3_1);
                XmlElement xe3_2 = xmlDoc.CreateElement("workint_temp");
                xe3_2.InnerText = Encript.Encode(stationPara.workingTemp.ToString());//工作温度  加密
                xe2.AppendChild(xe3_2);
                XmlElement xe3_3 = xmlDoc.CreateElement("starting_time");
                xe3_3.InnerText = Encript.Encode(stationPara.startingTime.ToString());// 开始时间 加密
                xe2.AppendChild(xe3_3);
                XmlElement xe3_4 = xmlDoc.CreateElement("ending_time");
                xe3_4.InnerText = Encript.Encode(stationPara.endingTime.ToString());// 结束时间 加密
                xe2.AppendChild(xe3_4);//添加到﹤staion﹥节点中

                xe1.AppendChild(xe2);//添加到﹤workflow﹥节点中
            }

            xroot.AppendChild(xe1);//添加到﹤workgroup﹥节点中
            xmlDoc.Save(xmlName); //保存其更改
        }

        ///<summary>
        /// 修改节点
        ///</summary>
        public void UpdateXmlNodeCarrierStatus(int carrier_id, hyCarrier.STATUS carrier_status, int carrier_pos)
        {
            XmlNodeList xnl1 = xroot.ChildNodes;

            // 遍历所有子节点
            foreach (XmlNode xn1 in xnl1)
            {// workflow
                // 每一个工作流，都包含关联一个工艺
                // 每个工艺中，都包含了13个station(炉子，水槽）的参数.station para
                hyWorkFlow workFlow = currWorkFlow = new hyWorkFlow(this);

                XmlElement xe1 = (XmlElement)xn1;
                workFlow.carrier_id = Convert.ToInt32(xe1.GetAttribute("carrier_id"));// 工作流ID
                if (workFlow.carrier_id == carrier_id)
                {
                    xe1.SetAttribute("carrier_status", ((int)carrier_status).ToString());// 夹具状态
                    xe1.SetAttribute("carrier_pos", carrier_pos.ToString());//夹具endingTime
                    break;// 找到，就跳出。
                }
            }

            xmlDoc.Save(xmlName);//保存。
        }

        ///<summary>
        /// 删除节点
        ///</summary>
        private void DeleteXmlNode()
        {
            xmlDoc = new XmlDocument();
            xmlDoc.Load("bookshop.xml"); //加载xml文件
            XmlNodeList xnl = xmlDoc.SelectSingleNode("bookshop").ChildNodes;

            foreach (XmlNode xn in xnl)
            {
                XmlElement xe = (XmlElement)xn;

                if (xe.GetAttribute("genre") == "fantasy")
                {
                    xe.RemoveAttribute("genre");//删除genre属性
                }
                else if (xe.GetAttribute("genre") == "update Sky_Kwolf")
                {
                    xe.RemoveAll();//删除该节点的全部内容
                }
            }
            xmlDoc.Save("bookshop.xml");
        }
    }
}