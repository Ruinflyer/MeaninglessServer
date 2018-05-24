using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeaninglessServer
{
    /// <summary>
    /// 玩家信息处理类
    /// </summary>
    public partial class handlePlayerMsg
    {
        
        /// <summary>
        /// 心跳消息
        /// </summary>
        /// <param name="connect"></param>
        /// <param name="baseProtocol"></param>
        public void MsgHeartBeat(Player player, BaseProtocol baseProtocol)
        {
            player.connect.lastTick = Utility.GetTimeStamp();
            Console.WriteLine("[更新心跳时间]" + player.connect.GetAddress());
        }

        
       

    }
}
