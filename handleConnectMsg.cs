using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeaninglessServer
{
    /// <summary>
    /// 连接消息处理类
    /// </summary>
    public class handleConnectMsg
    {
        /// <summary>
        /// 心跳消息
        /// </summary>
        /// <param name="connect"></param>
        /// <param name="baseProtocol"></param>
        public void MsgHeartBeat(Connect connect,BaseProtocol baseProtocol)
        {
            connect.lastTick = Utility.GetTimeStamp();
            Console.WriteLine("[更新心跳时间]" + connect.GetAdress());
        }

        public void MsgConnect(Connect connect, BaseProtocol Protocol)
        {
            int startIndex = 0;
            BytesProtocol bytesProtocol = (BytesProtocol)Protocol;
            string protocolName = bytesProtocol.GetString(startIndex, ref startIndex);
            string name = bytesProtocol.GetString(startIndex,ref startIndex);
            Console.WriteLine("[客户端 " + connect.GetAdress() + " ](连接消息) 以用户名："+name+" 连接");
            BytesProtocol bytesProtocolReturn = new BytesProtocol();
            bytesProtocolReturn.SpliceString("Connect");
            
            //名字已被使用，无法连接，返回-1，连接失败
            if(Player.NameIsUsed(name))
            {
                bytesProtocolReturn.SpliceInt(-1);
                connect.Send(bytesProtocolReturn);
                return;
            }
            //为连接初始化角色数据
            connect.player = new Player(name,connect);

            //触发连接事件
            Server.instance.handlePlayerEvent.OnConnect(connect.player);
            //返回0，即连接成功
            bytesProtocolReturn.SpliceInt(0);
            connect.Send(bytesProtocolReturn);
            return;
        }

        public void MsgDisconnect(Connect connect, BaseProtocol Protocol)
        {
            BytesProtocol bytesProtocol = new BytesProtocol();
            bytesProtocol.SpliceString("Disconnect");
            bytesProtocol.SpliceInt(0);
            if(connect.player==null)
            {
                connect.Send(bytesProtocol);
                connect.Close();
            }
            else
            {
                connect.Send(bytesProtocol);
                connect.player.Disconnect();
            }
        }
    }

}
