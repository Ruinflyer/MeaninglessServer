using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Timers;

namespace MeaninglessServer
{
    public class Server
    {
        //监听文件描述符(Listen file descriptor)
        public Socket Listenfd;
        //连接数组
        public Connect[] connects;
        //最大连接数
        public int maxConnectCount = 50;
        //协议
        public BaseProtocol protocol;

        //消息处理类实例
        public handleConnectMsg handleConnectMsg = new handleConnectMsg();
        public handlePlayerEvent handlePlayerEvent = new handlePlayerEvent();
        public handlePlayerMsg handlePlayerMsg = new handlePlayerMsg();

        //心跳时间
        public long heartBeatTime = 30;
        //计时器
        Timer timer = new Timer(1000);

        public static Server instance;
        public Server()
        {
            instance = this;
        }

        /// <summary>
        /// 获取新连接的连接池索引值，返回负数表示获取失败
        /// </summary>
        /// <returns></returns>
        public int GetNewConnectIndex()
        {
            if (connects == null)
            {
                return -1;
            }

            for (int i = 0; i < connects.Length; i++)
            {
                //为空则即时创建一个，非空即返回下标
                if (connects[i] == null)
                {
                    connects[i] = new Connect();
                    return i;
                }
                else if (connects[i].isUse == false)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 开启服务器
        /// </summary>
        /// <param name="host">主机</param>
        /// <param name="port">端口号</param>
        public void Start(string host, int port, int MaxConnectCount,long HeartBeatTime)
        {
            maxConnectCount = MaxConnectCount;
            heartBeatTime = HeartBeatTime;
            //初始化协议
            protocol = new BytesProtocol();
            //初始化计时器
            timer.Elapsed += new ElapsedEventHandler(HandleMainTimer);
            timer.AutoReset = false;
            timer.Enabled = true;
            //初始化连接池
            connects = new Connect[maxConnectCount];
            for (int i = 0; i < maxConnectCount; i++)
            {
                connects[i] = new Connect();
            }

            //新建Socket
            Listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //Bind
            IPAddress iPAddress = IPAddress.Parse(host);
            IPEndPoint iPEndPoint = new IPEndPoint(iPAddress, port);
            Listenfd.Bind(iPEndPoint);

            //Listen(最大连接数)
            Listenfd.Listen(maxConnectCount);

            //异步Accept
            Listenfd.BeginAccept(AcceptCallBack, null);

            Console.WriteLine("[服务器]：启动成功 连接类型TCP");
        }

        private void AcceptCallBack(IAsyncResult ar)
        {
            try
            {
                Socket socket = Listenfd.EndAccept(ar);
                int index = GetNewConnectIndex();

                if (index < 0)
                {
                    socket.Close();
                    Console.WriteLine("[连接被拒绝]：已达到最大连接数");
                }
                else
                {
                    Connect connect = connects[index];
                    connect.Init(socket);
                    string adress = connect.GetAdress();
                    Console.WriteLine("[客户端 " + adress + " ]：连接，ConnectID：" + index);
                    //开始异步接收客户端数据
                    connect.socket.BeginReceive(connect.buff, connect.buffCount, connect.GetRemainBuff(), SocketFlags.None, ReceiveCallBack, connect);
                    //消息循环
                    Listenfd.BeginAccept(AcceptCallBack, null);

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[AcceptCallBack 失败]：" + e.Message);
            }
        }

        private void ReceiveCallBack(IAsyncResult ar)
        {
            Connect connect = (Connect)ar.AsyncState;
            lock (connect)
            {
               
                try
                {

               
                    //结束异步接收
                    int count = connect.socket.EndReceive(ar);
                    if (count <= 0)
                    {
                        Console.WriteLine("[客户端 " + connect.GetAdress() + " ]：断开连接");
                        connect.Close();
                        return;
                    }
                    connect.buffCount += count;
                    PacketProcess(connect);
                    connect.socket.BeginReceive(connect.buff, connect.buffCount, connect.GetRemainBuff(), SocketFlags.None, ReceiveCallBack, connect);
                }
                catch
                {
                    Console.WriteLine("[客户端 " + connect.GetAdress() + " ]：断开连接");
                    connect.Close();
                }


            }

        }
        private void PacketProcess(Connect connect)
        {
            //消息长度小于一个消息头的长度，消息出错
            if (connect.buffCount < sizeof(Int32))
            {
                return;
            }

            Array.Copy(connect.buff, connect.lengthBytes, sizeof(Int32));
            connect.msgLength = BitConverter.ToInt32(connect.lengthBytes, 0);
            if (connect.buffCount < connect.msgLength + sizeof(Int32))
            {
                return;
            }
            //处理 消息 
            BaseProtocol proto = this.protocol.Decode(connect.buff, sizeof(Int32), connect.msgLength);
            HandleMsg(connect, proto);
            //消息已处理 下标更新
            int count = connect.buffCount - connect.msgLength - sizeof(Int32);
            Array.Copy(connect.buff, sizeof(Int32) + connect.msgLength, connect.buff, 0, count);
            connect.buffCount = count;
            if (connect.buffCount > 0)
            {
                PacketProcess(connect);
            }
        }

        private void HandleMsg(Connect connect, BaseProtocol baseProtocol)
        {
            string protocolName = baseProtocol.GetProtocolName();
            string methodName = "Msg" + protocolName;
            //当连接并未有玩家使用 或 接收方法为心跳消息(连接协议)
            if (connect.player == null || methodName == "HeartBeat" || methodName == "Disconnect")
            {
                MethodInfo methodInfo = handleConnectMsg.GetType().GetMethod(methodName);
                if (methodInfo == null)
                {
                    Console.WriteLine("[警告](连接消息)没有此方法：" + methodName + "已忽略");
                    return;
                }
                object[] param = new object[] { connect, baseProtocol };
                Console.WriteLine("[客户端 " + connect.GetAdress() + " ](连接消息)：" + methodName + " 处理");
                methodInfo.Invoke(handleConnectMsg, param);
            }
            else
            {
                //玩家协议：
                MethodInfo methodInfo = handlePlayerMsg.GetType().GetMethod(methodName);
                if (methodInfo == null)
                {
                    Console.WriteLine("[警告](玩家消息)没有此方法：" + methodName + "已忽略");
                    return;
                }
                object[] param = new object[] { connect.player, baseProtocol };
                Console.WriteLine("[客户端 " + connect.player.name + " ](玩家消息)：" + methodName + " 处理");
                methodInfo.Invoke(handlePlayerMsg, param);
            }

        }
        private void HandleMainTimer(object sender, ElapsedEventArgs e)
        {
            //处理心跳
            HandleHeartBeat();
            timer.Start();
        }
        private void HandleHeartBeat()
        {

            long timeNow = Utility.GetTimeStamp();

            for (int i = 0; i < connects.Length; i++)
            {
                Connect connect = connects[i];

                if (connect == null)
                {
                    continue;
                }
                if (!connect.isUse)
                {
                    continue;
                }

                if (connect.lastTick < timeNow - heartBeatTime)
                {
                    Console.WriteLine("[客户端 " + connect.GetAdress() + " ]：过久无心跳断开连接 ");
                    lock (connect)
                    {
                        connect.Close();
                    }
                    
                }
            }
        }
        /// <summary>
        /// 往一个连接发送消息
        /// </summary>
        /// <param name="connect"></param>
        /// <param name="protocol"></param>
        public void Send(Connect connect, BaseProtocol protocol)
        {
            byte[] bytes = protocol.Encode();
            byte[] length = BitConverter.GetBytes(bytes.Length);
            byte[] sendbuff = length.Concat(bytes).ToArray();
            try
            {
                connect.socket.BeginSend(sendbuff, 0, sendbuff.Length, SocketFlags.None, null, null);
            }
            catch (Exception e)
            {
                Console.WriteLine("[发送消息]" + connect.GetAdress() + " : " + e.Message);
            }
        }
        /// <summary>
        /// 广播消息
        /// </summary>
        /// <param name="protocol"></param>
        public void Broadcast(BaseProtocol protocol)
        {
            for (int i = 0; i < connects.Length; i++)
            {
                if (connects[i].isUse == false)
                {
                    continue;
                }
                if (connects[i].player == null)
                {
                    continue;
                }
                Send(connects[i], protocol);
            }
        }


        public void Close()
        {
            for (int i = 0; i < connects.Length; i++)
            {
                Connect connect = connects[i];
                if (connect == null)
                {
                    continue;
                }

                if (connect.isUse == false)
                {
                    continue;
                }
                lock (connect)
                {
                    connect.Close();
                }
            }
        }
    }
}
