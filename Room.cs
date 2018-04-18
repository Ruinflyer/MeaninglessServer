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
            timer = new Timer();
            timer.Elapsed += new ElapsedEventHandler(HandleTimer);
            timer.AutoReset = false;
            timer.Enabled = true;
        }
        public Status status = Status.Preparing;

        public int maxPlayer = 10;
        public Dictionary<string, Player> playerDict = new Dictionary<string, Player>();
        public Dictionary<string, bool> playerReadyDict = new Dictionary<string, bool>();

        public BytesProtocol MapProtocol = null;
        public BytesProtocol ItemsProtocol = null;

        #region 物品变量
        private float[] ProbabilityValue;
        private int[] ItemsID;
        private float totalProbabilityValue = 0;
        private float getRandom = 0;
        #endregion

        #region 毒圈变量
        public CirclefieldInfo circlefieldInfo;
        public int circlefieldIndex = 0;
        private Point Center;
        private const float R = 250f;
        public long LastCirclefieldTime;
        public bool beginTimer = false;
        private bool Moving = false;
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

        /// <summary>
        /// 获取房间信息协议
        /// </summary>
        /// <returns></returns>
        public BytesProtocol GetRoomInfo()
        {
            //(int)playerNum PlayerInfo...
            BytesProtocol protocol = new BytesProtocol();
            protocol.SpliceString("GetRoomInfo");

            protocol.SpliceInt(playerDict.Count);
            foreach (Player player in playerDict.Values)
            {
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

        /// <summary>
        /// 获取地图块协议
        /// </summary>
        /// <returns></returns>
        public BytesProtocol GetMaptitleDataProtocol()
        {
            BytesProtocol protocol = new BytesProtocol();
            protocol.SpliceString("GetMapData");
            List<string> mapDataList = new List<string>();
            mapDataList = SpawnMapAerapoint(RoomManager.instance.GetMaptileInfo());
            //地图块数量
            protocol.SpliceInt(mapDataList.Count);
            foreach (string str in mapDataList)
            {
                protocol.SpliceString(str);
            }

            return protocol;
        }

        /// <summary>
        /// 获取道具协议
        /// </summary>
        /// <returns></returns>
        public BytesProtocol GetItemsProtocol()
        {
            BytesProtocol protocol = new BytesProtocol();
            protocol.SpliceString("GetMapItemData");
            List<int> itemsList = new List<int>();
            itemsList = SpawnItem();
            //物品数量
            protocol.SpliceInt(itemsList.Count);
            foreach (int num in itemsList)
            {
                protocol.SpliceInt(num);
            }

            return protocol;
        }

        /// <summary>
        /// 用极低效率生成地图坐标
        /// </summary>
        /// <param name="maptile"></param>
        /// <returns></returns>
        private List<string> SpawnMapAerapoint(MaptileInfo maptile)
        {
            int MoutainRow;
            int MoutainCol;
            int LakeRow;
            int LakeCol;
            int[] WildHouseRow;
            int[] WildHouseCol;
            const int WildHouseamount = 4;
            int[] TownareaRow;
            int[] TownareaCol;
            const int Townamount = 3;
            //嵌套数组方便计算
            string[][] MapData = new string[8][];

            for (int k = 0; k < 8; k++)
            {
                MapData[k] = new string[8];
            }

            Random random = new Random();



            MoutainCol = random.Next(0, 7);
            MoutainRow = random.Next(0, 7);
            //Debug.Log("Moutain:(" + MoutainCol + "," + MoutainRow + ")");

            /* 在范围内即生成山，不在则不生成
             * 山被森林围住-占3x3格地块
             */
            if (MoutainCol > 0 && MoutainCol < 7)
            {
                if (MoutainRow > 0 && MoutainRow < 7)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            MapData[MoutainCol - 1 + i][MoutainRow - 1 + j] = maptile.Forest_Resname[random.Next(0, maptile.Forest_Resname.Length)];
                        }
                    }
                    MapData[MoutainCol][MoutainRow] = maptile.Moutain_Resname[random.Next(0, maptile.Moutain_Resname.Length)];
                }
            }

            //生成湖
            do
            {
                LakeCol = random.Next(1, 6);
                LakeRow = random.Next(1, 6);
                //Debug.Log("Lake : (" + LakeCol + "," + LakeRow + ")");
            }
            while (Math.Abs((LakeCol - MoutainCol)) < 2 || Math.Abs((LakeRow - MoutainRow)) < 2);
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    MapData[LakeCol - 1 + i][LakeRow - 1 + j] = maptile.Forest_Resname[random.Next(0, maptile.Forest_Resname.Length)];
                }
            }
            MapData[LakeCol][LakeRow] = maptile.Lake_Resname[random.Next(0, maptile.Lake_Resname.Length)];

            //生成城镇
            TownareaCol = new int[Townamount];
            TownareaRow = new int[Townamount];
            for (int i = 0; i < Townamount; i++)
            {
                TownareaCol[i] = random.Next(1, 5);
                TownareaRow[i] = random.Next(1, 5);
                //Debug.Log("Townarea" + i + " : (" + TownareaCol[i] + "," + TownareaRow[i] + ")");
            }


            for (int i = 0; i < Townamount; i++)
            {
                for (int j = 0; j < Townamount; j++)
                {
                    MapData[TownareaCol[i]][TownareaRow[i] + j] = maptile.Town_Resname[random.Next(0, maptile.Town_Resname.Length)];

                }
            }

            //生成野外房屋
            WildHouseCol = new int[WildHouseamount];
            WildHouseRow = new int[WildHouseamount];
            for (int i = 0; i < WildHouseamount; i++)
            {
                WildHouseCol[i] = random.Next(0, 7);
                WildHouseRow[i] = random.Next(0, 7);
                //Debug.Log("WildHouse" + i + " : (" + WildHouseCol[i] + "," + WildHouseRow[i] + ")");
            }
            for (int i = 0; i < WildHouseamount; i++)
            {

                MapData[WildHouseCol[i]][WildHouseRow[i]] = maptile.Wildhouse_Resname[random.Next(0, maptile.Wildhouse_Resname.Length)];
            }


            //生成野外草地
            int m = 0;
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (MapData[i][j] == null)
                    {

                        m = random.Next(0, maptile.Wild_Resname.Length);
                        MapData[i][j] = maptile.Wild_Resname[m];

                    }
                }
            }

            //嵌套转一维，方便传输
            List<string> list = new List<string>();

            for (int z = 0; z < 8; z++)
            {
                for (int x = 0; x < 8; x++)
                {
                    list.Add(MapData[z][x]);
                }
            }

            return list;
        }
        /// <summary>
        /// 生成128个随机道具ID
        /// </summary>
        /// <returns></returns>
        private List<int> SpawnItem()
        {
            List<int> ItemList = new List<int>();

            if (ProbabilityValue == null)
            {
                //所有物品的获得概率数组
                ProbabilityValue = RoomManager.instance.GetTotalOccurrenceProbability();
            }
            if (ItemsID == null)
            {
                //所有物品的ID数组
                ItemsID = RoomManager.instance.GetAllItemsID();
            }

            for (int i = 0; i < 128; i++)
            {
                ItemList.Add(ItemsID[CalcIndex(ProbabilityValue)]);
            }
            return ItemList;
        }
        /// <summary>
        /// 根据出现概率计算道具下标
        /// </summary>
        /// <param name="probabilityValue"></param>
        /// <returns></returns>
        private int CalcIndex(float[] probabilityValue)
        {
            if (totalProbabilityValue == 0)
            {
                for (int i = 0; i < probabilityValue.Length; i++)
                {
                    totalProbabilityValue += probabilityValue[i];
                }
            }

            Random random = new Random();
            int temp_tPV = Convert.ToInt32(totalProbabilityValue * 100f);
            getRandom = random.Next(0, temp_tPV);
            for (int i = 0; i < probabilityValue.Length; i++)
            {
                if (getRandom < probabilityValue[i] * 100f)
                {
                    return i;
                }
                else
                {
                    getRandom -= probabilityValue[i];
                }
            }
            return probabilityValue.Length - 1;

        }

        public bool CanStart()
        {
            if (status != Status.Preparing)
            {
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


        public BytesProtocol CirclefieldProtocol()
        {
            BytesProtocol p = new BytesProtocol();
            p.SpliceString("Circlefield");
            Point point = CalcCircleCenter(0, 0, R, circlefieldInfo.Circlefields[circlefieldIndex].ShrinkPercent);
            p.SpliceFloat(point.X);
            p.SpliceFloat(point.Y);
            p.SpliceFloat(circlefieldInfo.Circlefields[circlefieldIndex].ShrinkPercent);
            p.SpliceInt(circlefieldInfo.Circlefields[circlefieldIndex].Movetime);

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
                    if (circlefieldIndex < circlefieldInfo.Circlefields.Count)
                    {
                        circlefieldIndex++;
                        LastCirclefieldTime = Utility.GetTimeStamp();
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
