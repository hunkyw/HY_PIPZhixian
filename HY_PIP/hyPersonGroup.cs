using HY_PIP.utils;
using System;
using System.Collections.Generic;
using System.Xml;

namespace HY_PIP
{
    public class hyPersonGroup
    {
        private XmlDocument xmlDoc;
        private XmlNode xroot;

        public List<hyPerson> personList;
        public int person_id_max_assigned = 0;

        public hyPersonGroup()
        {
        }

        /**
         * 新增一个人员
         * */

        public void NewPerson(hyPerson Person)
        {
            personList.Add(Person);// 添加工艺
            InsertXmlNode(Person);// 追加数据文件
        }

        /**
         * 读取人员列表
         * */

        public void LoadPersonList()
        {
            LoadXml();
        }

        /**
         * 修改数据文件
         * */

        public void UpdatePerson(int index, hyPerson Person)
        {
            UpdateXmlNode(Person); // XML文件
            personList[index] = Person;
        }

        /**
         * 删除数据文件
         * */

        public void DeletePerson(int index, int person_id)
        {
            DeleteXmlNode(person_id);//第一步：删除 XML 文件内容
            personList.RemoveAt(index);//第二步：删除 PersonGroup 中的工艺列表 PersonList
        }

        /*
         *
         * 从 xml 数据中  读取一个已经存在的工作组（N个工作流的组合）
         *
         * */

        private void LoadXml()
        {
            personList = new List<hyPerson>();// 创建工作流列表

            xmlDoc = new XmlDocument();
            xmlDoc.Load("hyPerson.xml"); //加载xml文件
            xroot = xmlDoc.SelectSingleNode("persongroup");

            XmlNodeList xnl1 = xroot.ChildNodes;

            foreach (XmlNode xn1 in xnl1)
            {// Person
                // 每一个工作流，都包含关联一个工艺
                // 每个工艺中，都包含了13个station(炉子，水槽）的参数.station para
                hyPerson Person = new hyPerson();

                XmlElement xe1 = (XmlElement)xn1;

                Person.id = Convert.ToInt32(xe1.ChildNodes.Item(0).InnerText);// id
                Person.name = xe1.ChildNodes.Item(1).InnerText;// 姓名
                Person.job_number = xe1.ChildNodes.Item(2).InnerText;// 工号
                Person.position = xe1.ChildNodes.Item(3).InnerText;// 岗位
                Person.password = Encript.Decode(xe1.ChildNodes.Item(4).InnerText);// 密码//加密

                person_id_max_assigned = Math.Max(Person.id, person_id_max_assigned);

                personList.Add(Person);
            }
        }

        /*
         *
         * 在 xml 数据中  追加（插入）数据。
         *
         * */

        private void InsertXmlNode(hyPerson Person)
        {
            /**
             * 插入一个person
             * <person>
             * */
            XmlElement xe1 = xmlDoc.CreateElement("person");//创建一个﹤person﹥节点
            person_id_max_assigned++;// ID
            Person.id = person_id_max_assigned;// ID

            XmlElement xe2_1 = xmlDoc.CreateElement("id");// id
            xe2_1.InnerText = Person.id.ToString();
            xe1.AppendChild(xe2_1);
            XmlElement xe2_2 = xmlDoc.CreateElement("name");// id
            xe2_2.InnerText = Person.name.ToString();
            xe1.AppendChild(xe2_2);
            XmlElement xe2_3 = xmlDoc.CreateElement("job_number");// id
            xe2_3.InnerText = Person.job_number.ToString();
            xe1.AppendChild(xe2_3);
            XmlElement xe2_4 = xmlDoc.CreateElement("position");// id
            xe2_4.InnerText = Person.position.ToString();
            xe1.AppendChild(xe2_4);
            XmlElement xe2_5 = xmlDoc.CreateElement("password");// id
            xe2_5.InnerText = Encript.Encode(Person.password.ToString());// 加密
            xe1.AppendChild(xe2_5);

            xroot.AppendChild(xe1);//添加到﹤Persongroup﹥节点中
            xmlDoc.Save("hyPerson.xml"); //保存其更改
        }

        ///<summary>
        /// 修改节点
        ///</summary>
        private void UpdateXmlNode(hyPerson Person)
        {
            //xmlDoc = new XmlDocument();
            //xmlDoc.Load("hyPerson.xml"); // 加载xml文件
            // 获取Persongroup节点的所有子节点
            XmlNodeList xnl2 = xmlDoc.SelectSingleNode("persongroup").ChildNodes;

            // 遍历所有子节点
            foreach (XmlNode xn2 in xnl2)
            {
                XmlElement xe2 = (XmlElement)xn2; // 将子节点类型转换为XmlElement类型

                if (Convert.ToInt32(xe2.ChildNodes.Item(0).InnerText) == Person.id)
                {
                    xe2.ChildNodes.Item(1).InnerText = Person.name;//
                    xe2.ChildNodes.Item(2).InnerText = Person.job_number;//
                    xe2.ChildNodes.Item(3).InnerText = Person.position;//
                    xe2.ChildNodes.Item(4).InnerText = Encript.Encode(Person.password);// 加密

                    break;
                }
            }

            xmlDoc.Save("hyPerson.xml");//保存。
        }

        ///<summary>
        /// 删除节点
        ///</summary>
        private void DeleteXmlNode(int Person_id)
        {
            //xmlDoc = new XmlDocument();
            //xmlDoc.Load("hyPerson.xml"); //加载xml文件
            XmlNode xn1 = xmlDoc.SelectSingleNode("persongroup");
            XmlNodeList xnl2 = xn1.ChildNodes;

            foreach (XmlNode xn2 in xnl2)
            {
                XmlElement xe2 = (XmlElement)xn2;

                if (Convert.ToInt32(xe2.ChildNodes.Item(0).InnerText) == Person_id)// ID 是否能够匹配上，ID自动生成。
                {
                    // xe2.RemoveAll();// 删除该节点的全部内容
                    xn1.RemoveChild(xe2);
                    break;
                }
            }
            xmlDoc.Save("hyPerson.xml");
        }
    }
}