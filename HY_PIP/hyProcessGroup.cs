using HY_PIP.utils;
using System;
using System.Collections.Generic;
using System.Xml;

namespace HY_PIP
{
    public class hyProcessGroup
    {
        private XmlDocument xmlDoc;
        private XmlNode xroot;

        public List<hyProcess> processList;
        private int process_id_max_assigned;

        public hyProcessGroup()
        {
        }

        /**
         * 新增一个工艺
         * */

        public void NewProcess(hyProcess process)
        {
            processList.Add(process);// 添加工艺
            InsertXmlNode(process);// 追加数据文件
        }

        /**
         * 读取工艺文件列表
         * */

        public void LoadProcessList()
        {
            LoadXml();
        }

        /**
         * 修改数据文件
         * */

        public void UpdateProcess(int index, hyProcess process)
        {
            UpdateXmlNode(process);// XML文件
            processList[index] = process;
        }

        /**
         * 删除数据文件
         * */

        public void DeleteProcess(int index, int process_id)
        {
            DeleteXmlNode(process_id);//第一步：删除 XML 文件内容
            processList.RemoveAt(index);//第二步：删除 processGroup 中的工艺列表 processList
        }

        /*
         *
         * 从 xml 数据中  读取一个已经存在的工作组（N个工作流的组合）
         *
         * */

        private void LoadXml()
        {
            processList = new List<hyProcess>();// 创建工作流列表

            xmlDoc = new XmlDocument();
            xmlDoc.Load("hyProcess.xml"); //加载xml文件
            xroot = xmlDoc.SelectSingleNode("processgroup");

            XmlNodeList xnl1 = xroot.ChildNodes;

            foreach (XmlNode xn1 in xnl1)
            {// process
                // 每一个工作流，都包含关联一个工艺
                // 每个工艺中，都包含了13个station(炉子，水槽）的参数.station para
                hyProcess process = new hyProcess();

                XmlElement xe1 = (XmlElement)xn1;
                process.process_id = Convert.ToInt32(xe1.GetAttribute("process_id"));// 工艺ID
                process.process_name = xe1.GetAttribute("process_name");// 工艺名称
                process_id_max_assigned = Math.Max(process_id_max_assigned, process.process_id);// 已经分配的最大process_id

                XmlNodeList xnl2 = xe1.ChildNodes;
                int index = 0;
                foreach (XmlNode xn2 in xnl2)
                {//staion
                    XmlElement xe2 = (XmlElement)xn2;
                    index = Convert.ToInt32(xe2.GetAttribute("station_id"));


                    process.stationParaList[index].workingTemp = Convert.ToInt32(Encript.Decode(xe2.ChildNodes.Item(0).InnerText));// 工作温度// 加密
                    process.stationParaList[index].workingTime = Convert.ToInt32(Encript.Decode(xe2.ChildNodes.Item(1).InnerText));// 工作时间// 加密
                    process.stationParaList[index].formula = Encript.Decode(xe2.ChildNodes.Item(2).InnerText);// 配方// 加密
                }
                processList.Add(process);
            }
        }

        /*
         *
         * 在 xml 数据中  追加（插入）数据。
         *
         * */

        private void InsertXmlNode(hyProcess process)
        {
            /**
             * 插入一个工作流
             * <workflow>
             * */
            XmlElement xe1 = xmlDoc.CreateElement("process");//创建一个﹤workflow﹥节点
            process_id_max_assigned++;// 工艺ID
            process.process_id = process_id_max_assigned;// 工艺ID
            xe1.SetAttribute("process_id", process.process_id.ToString());// 工艺ID
            xe1.SetAttribute("process_name", process.process_name.ToString());// 工艺名称

            /**
             * 循环插入station
             * <station>
             * */
            foreach (hyStationPara stationPara in process.stationParaList)
            {
                XmlElement xe2 = xmlDoc.CreateElement("station");
                xe2.SetAttribute("station_id", stationPara.station_id.ToString());// 工位ID

                /**
                 * <working_time>,<working_temp>,<starting_time>,<ending_time>
                 * */
                XmlElement xe3_1 = xmlDoc.CreateElement("working_temp");// 工作温度
                xe3_1.InnerText = Encript.Encode(stationPara.workingTemp.ToString());// 加密
                xe2.AppendChild(xe3_1);
                XmlElement xe3_2 = xmlDoc.CreateElement("working_time");// 工作时间
                xe3_2.InnerText = Encript.Encode(stationPara.workingTime.ToString());// 加密
                xe2.AppendChild(xe3_2);
                XmlElement xe3_3 = xmlDoc.CreateElement("formula");// 配方
                xe3_3.InnerText = Encript.Encode(stationPara.formula.ToString());// 加密
                xe2.AppendChild(xe3_3);//添加到﹤staion﹥节点中

                xe1.AppendChild(xe2);//添加到﹤process﹥节点中
            }

            xroot.AppendChild(xe1);//添加到﹤processgroup﹥节点中
            xmlDoc.Save("hyProcess.xml"); //保存其更改
        }

        ///<summary>
        /// 修改节点
        ///</summary>
        private void UpdateXmlNode(hyProcess process)
        {
            //xmlDoc = new XmlDocument();
            //xmlDoc.Load("hyProcess.xml"); // 加载xml文件
            // 获取processgroup节点的所有子节点
            XmlNodeList xnl2 = xmlDoc.SelectSingleNode("processgroup").ChildNodes;

            // 遍历所有子节点
            foreach (XmlNode xn2 in xnl2)
            {
                XmlElement xe2 = (XmlElement)xn2; // 将子节点类型转换为XmlElement类型

                if (Convert.ToInt32(xe2.GetAttribute("process_id")) == process.process_id)// 工艺ID 是否能够匹配上，工艺ID自动生成。
                {
                    xe2.SetAttribute("process_name", process.process_name.ToString());// 工艺名称

                    // 循环修改station
                    XmlNodeList xnl3 = xe2.ChildNodes;
                    int index = 0;
                    foreach (XmlNode xn3 in xnl3)
                    {//staion
                        XmlElement xe3 = (XmlElement)xn3;
                        index = Convert.ToInt32(xe3.GetAttribute("station_id"));
                        xe3.ChildNodes.Item(0).InnerText = Encript.Encode(process.stationParaList[index].workingTemp.ToString());// 工作时间// 加密
                        xe3.ChildNodes.Item(1).InnerText = Encript.Encode(process.stationParaList[index].workingTime.ToString());// 工作温度// 加密
                        xe3.ChildNodes.Item(2).InnerText = Encript.Encode(process.stationParaList[index].formula);// 配方// 加密
                    }
                    break;
                }
            }

            xmlDoc.Save("hyProcess.xml");//保存。
        }

        ///<summary>
        /// 删除节点
        ///</summary>
        private void DeleteXmlNode(int process_id)
        {
            //xmlDoc = new XmlDocument();
            //xmlDoc.Load("hyProcess.xml"); //加载xml文件
            XmlNode xn1 = xmlDoc.SelectSingleNode("processgroup");
            XmlNodeList xnl2 = xn1.ChildNodes;

            foreach (XmlNode xn2 in xnl2)
            {
                XmlElement xe2 = (XmlElement)xn2;

                if (Convert.ToInt32(xe2.GetAttribute("process_id")) == process_id)// 工艺ID 是否能够匹配上，工艺ID自动生成。
                {
                    //xe2.RemoveAll();//删除该节点的全部内容
                    xn1.RemoveChild(xe2);
                    break;
                }
            }
            xmlDoc.Save("hyProcess.xml");
        }
    }
}