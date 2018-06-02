namespace HY_PIP
{
    internal class GenericOp
    {
        public static bool EStop = false;       // 紧急停止
        public static bool AutoMode = false;     // 自动模式，手动模式切换
        public static bool EStop_Manual = false;// 来自手动操作柜，显示屏柜体的急停按钮信号。
        public static bool EStop_Soft = false;// 来自显示屏。
        public static bool RequestZRN = false;// 请求原点回归。

        public static int xPos;             //
        public static int zPos;             //
        public static int xPos_servo;       // 伺服的位置X
        public static int zPos_servo;       // 伺服的位置Z
        public static int weight;
        public static int temperature1;            //1号水槽温度
        public static int temperature1_1 = 0;         //1好水槽设定温度
        public static int temperature2;            //2号水槽温度
        public static int temperature2_1 = 0;          //2号水槽设定温度
        public static int temperature3;            //3号水槽温度
        public static int temperature3_1 = 0;          //3号水槽设定温度

        public static int temperature41;              //4号炉子上温区温度
        public static int temperature42;               //4号炉子下温区温度
        public static int temperature4 = 300;              //4号炉子设置温度

        public static int temperature51;           //5号炉子上温区温度
        public static int temperature52;           //5号炉子下温区温度
        public static int temperature5 = 570;            //5号设置温度

        public static int temperature61;           //6号炉子上温区温度
        public static int temperature62;           //6号炉子下温区温度
        public static int temperature6 = 570;            //6号炉设置温度

        public static int temperature71;           //7号炉子上温区温度
        public static int temperature72;           //7号炉子下温区温度
        public static int temperature7 = 400;            //7号炉设置温度

        public static int temperature111;          //11号炉子上温区温度
        public static int temperature112;          //11号炉子下温区温度
        public static int temperature11 = 160;           //11号炉设置温度
        public static int temperature121;          //12号炉子上温区温度
        public static int temperature122;          //12号炉子下温区温度
        public static int temperature12 = 90;           //12号炉设置温度
        public static int temperature131;          //13号炉子上温区温度
        public static int temperature132;          //13号炉子下温区温度
        public static int temperature13 = 0;           //13号炉子设置温度

        public static int tmpTimeOffset = 0;
        public static int GREASEnum = 0;

        public enum GTRY_ACTION_TYPE { NONE, FORWARD, BAKWARD, UP, DOWN, CLAMP, CLAMP_RELAX }

        public static GTRY_ACTION_TYPE manual_gtry_action;// 手动控制柜对应的龙门动作指令。

        public static int errCode;// 错误码
    }
}