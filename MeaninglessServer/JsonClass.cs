using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeaninglessServer
{
    [Serializable]
    public class ServerConf
    {
        //监听地址
        public string ServerHost;
        //监听端口
        public int ServerPort;
        //连接数
        public int Connects;
        //心跳时间
        public long HeartBeatTime;

    }

    [Serializable]
    public class MaptileInfo
    {
        //与客户端一样
        public string[] Town_Resname;
        public string[] Lake_Resname;
        public string[] Wild_Resname;
        public string[] Forest_Resname;
        public string[] Wildhouse_Resname;
        public string[] Moutain_Resname;
    }

    [Serializable]
    public class SingleItemInfo
    {
        public int ItemID;
        //OccurrenceProbability 出现概率
        public float OP;
    }

    [Serializable]
    public class ItemsInfo
    {
       public List<SingleItemInfo> ItemInfoList;
    }

    [System.Serializable]
    public class CirclefieldInfo
    {
        public List<SingleCirclefield> Circlefields;
    }
    //单个圈参数
    [System.Serializable]
    public class SingleCirclefield
    {
        //圈持续时间
        public int Holdtime;
        //圈移动时间
        public int Movetime;
        //收缩为原圈半径的比例
        public float ShrinkPercent;
        //处在此阶段的圈时对玩家造成每秒伤害
        public int DamagePerSec;
    }
}
