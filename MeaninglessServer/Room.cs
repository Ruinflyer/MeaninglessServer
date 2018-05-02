using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
namespace MeaninglessServer
{
    public class Room
    {

        public enum Status
        {
            Preparing = 0,
            Ongame
        }

        public struct Point
        {
            public float X;
            public float Y;
        }
        Timer timer;
        public Room()
        {
           
        }
        public Status status = Status.Preparing;

        public int maxPlayer = 10;
        public Dictionary<string, Player> playerDict = new Dictionary<string, Player>();
        public Dictionary<string, bool> playerReadyDict = new Dictionary<string, bool>();

        public BytesProtocol MapProtocol = null;
        public BytesProtocol ItemsProtocol = null;


        #region 毒圈变量
        public CirclefieldInfo circlefieldInfo;
        public int circlefieldIndex = 0;

        private const float R = 250f;
        public long LastCirclefieldTime;
        public bool beginTimer = false;
        private bool Moving = false;
        private bool FirstRound = true;
        #endregion
        /*********************************/

        /// <summary>
        /// 加入玩家
        /// </summary>
        public bool AddPlayer(Player player)
        {
            lock (playerDict)
            {
                //超过人数 退出
                if (playerDict.Count >= maxPlayer)
                {
                    return false;
                }
                PlayerStatus playerStatus = player.playerStatus;
                playerStatus.room = this;
                playerStatus.status = PlayerStatus.Status.InRoom;

                //房间没人时为房主
                if (playerDict.Count == 0)
                {
                    playerStatus.isMaster = true;
                }

                playerDict.Add(player.name, player);

            }
            return true;
        }
        /// <summary>
        /// 删除玩家
        /// </summary>
        public void DelPlayer(string playerName)
        {
            lock (playerDict)
            {
                if (!playerDict.ContainsKey(playerName))
                {
                    return;
                }

                playerDict[playerName].playerStatus.status = PlayerStatus.Status.Null;
                bool master = playerDict[playerName].playerStatus.isMaster;
                playerDict.Remove(playerName);
                if (master)
                {
                    NewMaster();
                }
            }
        }
        /// <summary>
        /// 按照基本法去产生房主
        /// </summary>
        public void NewMaster()
        {
            lock (playerDict)
            {
                if (playerDict.Count <= 0)
                {
                    return;
                }
                //将所有玩家设为非房主
                foreach (Player player in playerDict.Values)
                {
                    player.playerStatus.isMaster = false;
                }
                //字典第一个元素为房主
                playerDict.Values.First().playerStatus.isMaster = true;
            }
        }
        /// <summary>
        /// 广播
        /// </summary>
        public void Broadcast(BaseProtocol protocol)
        {
            foreach (Player player in playerDict.Values)
            {
                player.Send(protocol);
            }
        }

        public void NewTimer()
        {
            timer = new Timer();
            timer.Elapsed += new ElapsedEventHandler(HandleTimer);
            timer.AutoReset = false;
            timer.Enabled = true;
        }

        /// <summary>
        /// 获取房间信息协议
        /// </summary>
        /// <returns></returns>
        public BytesProtocol GetRoomInfo()
        {
            //(int)playerNum PlayerInfo...
            BytesProtocol protocol = new BytesProtocol();
            protocol.SpliceString("GetRoomInfo");
            //玩家数量
            protocol.SpliceInt(playerDict.Count);
            foreach (Player player in playerDict.Values)
            {
                //玩家名
                protocol.SpliceString(player.name);
                //是房主则返回1 不是则返回0
                if (player.playerStatus.isMaster)
                {
                    protocol.SpliceInt(1);
                }
                else
                {
                    protocol.SpliceInt(0);
                }
            }
            return protocol;
        }


        public bool CanStart()
        {
            if (status != Status.Preparing)
            {
                return false;
            }
            if(playerDict.Count<2)
            {
                Console.WriteLine("");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 开始战局
        /// </summary>
        public void StartGame()
        {
            BytesProtocol p = new BytesProtocol();
            p.SpliceString("StartGame");
            status = Status.Ongame;
            lock (playerDict)
            {
                //玩家数
                p.SpliceInt(playerDict.Count);
                foreach (Player player in playerDict.Values)
                {
                    player.playerStatus.HP = 100f;
                    p.SpliceString(player.name);
                    p.SpliceFloat(player.playerStatus.HP);

                }
                Broadcast(p);
            }

        }

        /// <summary>
        /// 毒圈协议
        /// </summary>
        /// <returns></returns>
        public BytesProtocol CirclefieldProtocol()
        {
            BytesProtocol p = new BytesProtocol();
            p.SpliceString("Circlefield");
            Point point = CalcCircleCenter(0, 0, R, circlefieldInfo.Circlefields[circlefieldIndex].ShrinkPercent);
            p.SpliceFloat(point.X);
            p.SpliceFloat(point.Y);
            p.SpliceFloat(circlefieldInfo.Circlefields[circlefieldIndex].ShrinkPercent);
            p.SpliceInt(circlefieldInfo.Circlefields[circlefieldIndex].Movetime);
            Console.WriteLine("pointX:{0} pointY:{1} per:{2} mt{3}",point.X,point.Y, circlefieldInfo.Circlefields[circlefieldIndex].ShrinkPercent, circlefieldInfo.Circlefields[circlefieldIndex].Movetime);
            return p;
        }


        public BytesProtocol CirclefieldTimeProtocol()
        {
            BytesProtocol p = new BytesProtocol();
            p.SpliceString("CirclefieldTime");
            p.SpliceInt(circlefieldInfo.Circlefields[circlefieldIndex].Holdtime);
            return p;
        }
        /// <summary>
        /// 计算新毒圈坐标
        /// </summary>
        private Point CalcCircleCenter(float X, float Y, float Radius, float Shrinkpercent)
        {

            float distance = Math.Abs(Radius - (Radius * Shrinkpercent));
            Random random = new Random();
            double randomDecision = random.NextDouble();
            float randomX = 0f, randomY = 0f;
            if (randomDecision <= 0.25d && randomDecision >= 0d)
            {
                randomX = (float)Utility.NextDouble(new Random(), X, X + distance);
                randomY = (float)Utility.NextDouble(new Random(), Y, Y + distance);
            }
            if (randomDecision <= 0.5d && randomDecision > 0.25d)
            {
                randomX = (float)Utility.NextDouble(new Random(), X, X - distance);
                randomY = (float)Utility.NextDouble(new Random(), Y, Y + distance);
            }
            if (randomDecision <= 0.75d && randomDecision > 0.5d)
            {
                randomX = (float)Utility.NextDouble(new Random(), X, X - distance);
                randomY = (float)Utility.NextDouble(new Random(), Y, Y - distance);
            }
            if (randomDecision <= 1d && randomDecision > 0.75d)
            {
                randomX = (float)Utility.NextDouble(new Random(), X, X + distance);
                randomY = (float)Utility.NextDouble(new Random(), Y, Y - distance);
            }
            Point newCenter = new Point
            {
                X = randomX,
                Y = randomY
            };

            return newCenter;
        }

        private void HandleTimer(object sender, ElapsedEventArgs e)
        {
            
            if (beginTimer)
            {
                CirclefieldTick();
                timer.Start();
            }

        }
        /// <summary>
        /// 毒圈计时处理
        /// </summary>
        private void CirclefieldTick()
        {
            long timeNow = Utility.GetTimeStamp();
            long holdTime = circlefieldInfo.Circlefields[circlefieldIndex].Holdtime;
            long moveTime = circlefieldInfo.Circlefields[circlefieldIndex].Movetime;
            if(circlefieldIndex == 0 && FirstRound==true)
            {
                //开始时发送毒圈保持时间
                Broadcast(CirclefieldTimeProtocol());
                FirstRound = false;
            }
            if (Moving == false)
            {
                if (LastCirclefieldTime < timeNow - holdTime)
                {
                    Broadcast(CirclefieldProtocol());
                    LastCirclefieldTime = Utility.GetTimeStamp();
                    Moving = true;
                }
            }
            else
            {
                if (LastCirclefieldTime < timeNow - moveTime)
                {
                    if (circlefieldIndex < circlefieldInfo.Circlefields.Count-1)
                    {
                        circlefieldIndex++;
                        LastCirclefieldTime = Utility.GetTimeStamp();
                        Broadcast(CirclefieldTimeProtocol());//移动完，发送下一圈的保持时间
                        Moving = false;
                    }
                    else
                    {
                        beginTimer = false;
                    }

                }

            }

        }


    }
}
