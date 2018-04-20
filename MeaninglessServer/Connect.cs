using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;

namespace MeaninglessServer
{
    public class Connect
    {
        //缓冲区长度
        public const int BUFFER_SIZE = 1024;
        public Socket socket;
        //连接是否可用
        public bool isUse = false;
        public byte[] buff = new byte[BUFFER_SIZE];
        public int buffCount = 0;

        //包头-32位无符号整数保存消息长度
        public byte[] lengthBytes = new byte[sizeof(UInt32)];
        public Int32 msgLength = 0;

        public long lastTick = long.MinValue;

        public Player player;

        public Connect()
        {
            buff = new byte[BUFFER_SIZE];
        }

        /// <summary>
        /// 初始化连接
        /// </summary>
        /// <param name="socket"></param>
        public void Init(Socket socket)
        {
            this.socket = socket;
            isUse = true;
            buffCount = 0;
            lastTick = Utility.GetTimeStamp();
        }

        public int GetRemainBuff()
        {
            return BUFFER_SIZE - buffCount;
        }

        public string GetAdress()
        {
            if (!isUse)
            {
                return "连接不可用，无法获取地址";
            }
            return socket.RemoteEndPoint.ToString();
        }

        public void Send(BaseProtocol Protocol)
        {
            Server.instance.Send(this, Protocol);
        }

        public void Close()
        {
            if (!isUse)
            {
                return;
            }
            //玩家退出
            if(player!=null)
            {
                player.Disconnect();
                return;
            }
            Console.WriteLine("[断开连接]："+GetAdress());
            socket.Shutdown(SocketShutdown.Both);
            Thread.Sleep(10);
            socket.Close();
            isUse = false;
        }
    }
}
