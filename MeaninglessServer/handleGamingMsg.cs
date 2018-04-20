using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeaninglessServer
{
    public partial class handlePlayerMsg
    {
        public void MsgRequestStartGame(Player player, BaseProtocol baseProtocol)
        {
            BytesProtocol protocol = new BytesProtocol();
            protocol.SpliceString("RequestStartGame");
            /*
            if (!player.playerStatus.room.CanStart())
            {
                protocol.SpliceInt(-1);
                player.Send(protocol);
                return;
            }
            */
            protocol.SpliceInt(0);
            player.Send(protocol);
            player.playerStatus.room.StartGame();
        }

        /// <summary>
        /// 获取地图块数据-废弃
        /// </summary>
        public void MsgGetMapData(Player player, BaseProtocol baseProtocol)
        {
            player.Send(player.playerStatus.room.MapProtocol);
        }

        /// <summary>
        /// 获取地图物品数据
        /// </summary>
        public void MsgGetMapItemData(Player player, BaseProtocol baseProtocol)
        {
            player.Send(player.playerStatus.room.ItemsProtocol);
        }

        /// <summary>
        /// 玩家加载完毕,定时器开始计时
        /// </summary>
        public void MsgPlayerReady(Player player, BaseProtocol baseProtocol)
        {
            
            int startIndex = 0;
            BytesProtocol p = (BytesProtocol)baseProtocol;
            p.GetString(startIndex, ref startIndex);
            string playerName = p.GetString(startIndex, ref startIndex);
            //int playerstatus = p.GetInt(startIndex, ref startIndex);
            Room room = player.playerStatus.room;
            if (room.playerReadyDict.Count < room.playerDict.Count)
            {
                lock (room.playerReadyDict)
                {
                    room.playerReadyDict.Add(playerName, true);
                }

            }
            else
            {
                //广播-所有玩家加载完毕信息
                BytesProtocol protocol = new BytesProtocol();
                protocol.SpliceString("AllPlayerLoaded");
                room.Broadcast(protocol);
                room.beginTimer = true;
                room.LastCirclefieldTime = Utility.GetTimeStamp();

            }

        }

        /// <summary>
        /// 更新玩家信息
        /// </summary>
        public void MsgUpdatePlayerInfo(Player player, BaseProtocol baseProtocol)
        {
            if (player.playerStatus.status != PlayerStatus.Status.Gaming)
            {
                return;
            }
            BytesProtocol p = (BytesProtocol)baseProtocol;
            int startIndex = 0;
            p.GetString(startIndex, ref startIndex);
            float HP = p.GetFloat(startIndex, ref startIndex);
            float posX = p.GetFloat(startIndex, ref startIndex);
            float posY = p.GetFloat(startIndex, ref startIndex);
            float posZ = p.GetFloat(startIndex, ref startIndex);
            float rotX = p.GetFloat(startIndex, ref startIndex);
            float rotY = p.GetFloat(startIndex, ref startIndex);
            float rotZ = p.GetFloat(startIndex, ref startIndex);
            int HeadItem = p.GetInt(startIndex, ref startIndex);
            int BodyItem = p.GetInt(startIndex, ref startIndex);
            int WeaponID = p.GetInt(startIndex, ref startIndex);
            string CurrentAction = p.GetString(startIndex, ref startIndex);

            player.playerStatus.HP = HP;
            player.playerStatus.posX = posX;
            player.playerStatus.posY = posY;
            player.playerStatus.posZ = posZ;
            player.playerStatus.HeadItemID = HeadItem;
            player.playerStatus.BodyItemID = BodyItem;
            player.playerStatus.WeaponID = WeaponID;
            player.playerStatus.CurrentAction = CurrentAction;
            player.playerStatus.LastUpdateTime = Utility.GetTimeStamp();

            BytesProtocol protocolReturn = new BytesProtocol();
            protocolReturn.SpliceString("UpdatePlayerInfo");
            protocolReturn.SpliceString(player.name);
            protocolReturn.SpliceFloat(posX);
            protocolReturn.SpliceFloat(posY);
            protocolReturn.SpliceFloat(posZ);
            protocolReturn.SpliceFloat(rotX);
            protocolReturn.SpliceFloat(rotY);
            protocolReturn.SpliceFloat(rotZ);
            protocolReturn.SpliceInt(HeadItem);
            protocolReturn.SpliceInt(BodyItem);
            protocolReturn.SpliceInt(WeaponID);
            protocolReturn.SpliceString(CurrentAction);
            player.playerStatus.room.Broadcast(protocolReturn);
        }

        /// <summary>
        /// 击中玩家
        /// </summary>
        public void MsgPlayerHitSomeone(Player player, BaseProtocol baseProtocol)
        {
            //击中玩家协议
            //消息结构:(string)PlayerHitSomeone + (string)PlayerName + (float)Damage

            //玩家不在游戏状态，当没事发生
          
            Room room = player.playerStatus.room;



            int startIndex = 0;
            BytesProtocol p = (BytesProtocol)baseProtocol;
            p.GetString(startIndex, ref startIndex);
            string HitplayerName = p.GetString(startIndex, ref startIndex);
            float Damage = p.GetFloat(startIndex, ref startIndex);

            if (room.playerDict.ContainsKey(HitplayerName))
            {
                lock (room.playerDict)
                {
                    //扣血操作
                    room.playerDict[HitplayerName].playerStatus.HP -= Damage;
                    //死亡后自动离开房间
                    if (room.playerDict[HitplayerName].playerStatus.HP <= 0)
                    {
                        //玩家死亡协议
                        //消息结构:(string)PlayerKilled + (string)Killer + (string)KilledPlayer
                        BytesProtocol deadProtool = new BytesProtocol();
                        deadProtool.SpliceString("PlayerKilled");
                        deadProtool.SpliceString(player.name);
                        deadProtool.SpliceString(HitplayerName);
                        room.Broadcast(deadProtool);
                        RoomManager.instance.LeaveRoom(room.playerDict[HitplayerName]);

                    }
                }
            }
            else
            {
                //玩家不在房间中,返回
                return;
            }
        }

        /// <summary>
        /// 玩家被毒圈伤害
        /// </summary>
        public void MsgPlayerPoison(Player player, BaseProtocol baseProtocol)
        {
            //玩家被毒圈伤害协议
            //消息结构:(string)PlayerPoison

            Room room = player.playerStatus.room;
            //玩家不在游戏状态，当没事发生
            if (player.playerStatus.status != PlayerStatus.Status.Gaming)
            {
                return;
            }
            //玩家不在房间中,返回


            int startIndex = 0;
            BytesProtocol p = (BytesProtocol)baseProtocol;
            p.GetString(startIndex, ref startIndex);


            if (player.playerStatus.HP > 0)
            {
                if (room.circlefieldIndex <= room.circlefieldInfo.Circlefields.Count - 1)
                {
                    player.playerStatus.HP -= room.circlefieldInfo.Circlefields[room.circlefieldIndex].DamagePerSec;
                }
            }
            else
            {
                //玩家死亡协议
                //消息结构:(string)PlayerDead + (string)playerName
                BytesProtocol deadProtool = new BytesProtocol();
                deadProtool.SpliceString("PlayerDead");
                deadProtool.SpliceString(player.name);
                room.Broadcast(deadProtool);
                RoomManager.instance.LeaveRoom(player);
            }



        }

        /// <summary>
        /// 玩家一般魔法,起点位置,起点旋转度
        /// </summary>
        /// <param name="player"></param>
        /// <param name="baseProtocol"></param>
        public void MsgPlayerMagic(Player player, BaseProtocol baseProtocol)
        {

            //玩家魔法
            //消息结构: (string)PlayerMagic + (string)playerName + (int)magicItemID + magicItemID + (float)posX + (float)posY +(float)posZ+ (float)rotX + (float)rotY + (float)rotZ
            if (player.playerStatus.status != PlayerStatus.Status.Gaming)
            {
                return;
            }
            Room room = player.playerStatus.room;

            int startIndex = 0;
            BytesProtocol p = (BytesProtocol)baseProtocol;
            p.GetString(startIndex, ref startIndex);
            string playerName = p.GetString(startIndex, ref startIndex);
            int magicItemID = p.GetInt(startIndex, ref startIndex);
            float posX = p.GetFloat(startIndex, ref startIndex);
            float posY = p.GetFloat(startIndex, ref startIndex);
            float posZ = p.GetFloat(startIndex, ref startIndex);
            float rotX = p.GetFloat(startIndex, ref startIndex);
            float rotY = p.GetFloat(startIndex, ref startIndex);
            float rotZ = p.GetFloat(startIndex, ref startIndex);

            //转发魔法消息
            BytesProtocol p_broadcast = new BytesProtocol();
            p_broadcast.SpliceString("PlayerMagic");
            p_broadcast.SpliceString(player.name);
            p_broadcast.SpliceInt(magicItemID);
            p_broadcast.SpliceFloat(posX);
            p_broadcast.SpliceFloat(posY);
            p_broadcast.SpliceFloat(posZ);
            p_broadcast.SpliceFloat(rotX);
            p_broadcast.SpliceFloat(rotY);
            p_broadcast.SpliceFloat(rotZ);
            room.Broadcast(p_broadcast);
        }

        /// <summary>
        /// 玩家特殊魔法,带终点
        /// </summary>
        public void MsgPlayerMagicEndpoint(Player player, BaseProtocol baseProtocol)
        {
            //玩家特殊魔法,终点生成
            //消息结构: (string)PlayerMagic + (string)playerName + (int)magicItemID + (float)endposX+(float)endposY+(float)endposZ
            if (player.playerStatus.status != PlayerStatus.Status.Gaming)
            {
                return;
            }
            Room room = player.playerStatus.room;

            int startIndex = 0;
            BytesProtocol p = (BytesProtocol)baseProtocol;
            p.GetString(startIndex, ref startIndex);
            string playerName = p.GetString(startIndex, ref startIndex);
            int magicItemID = p.GetInt(startIndex, ref startIndex);
            float posX = p.GetFloat(startIndex, ref startIndex);
            float posY = p.GetFloat(startIndex, ref startIndex);
            float posZ = p.GetFloat(startIndex, ref startIndex);

            //转发魔法消息
            BytesProtocol p_broadcast = new BytesProtocol();
            p_broadcast.SpliceString("PlayerMagicEndpoint");
            p_broadcast.SpliceString(player.name);
            p_broadcast.SpliceInt(magicItemID);
            p_broadcast.SpliceFloat(posX);
            p_broadcast.SpliceFloat(posY);
            p_broadcast.SpliceFloat(posZ);
            room.Broadcast(p_broadcast);
        }

        /// <summary>
        /// 玩家胜利
        /// </summary>
        public void MsgPlayerSuccess(Player player, BaseProtocol baseProtocol)
        {
            //玩家胜利
            //消息结构: (string)PlayerSuccess
            if (player.playerStatus.status != PlayerStatus.Status.Gaming)
            {
                return;
            }
            Room room = player.playerStatus.room;

            int startIndex = 0;
            BytesProtocol p = (BytesProtocol)baseProtocol;
            p.GetString(startIndex, ref startIndex);


            RoomManager.instance.LeaveRoom(player);

        }

       /// <summary>
       /// 房间门打开转发
       /// </summary>
       /// <param name="player"></param>
       /// <param name="baseProtocol"></param>
        public void MsgDoorOpen(Player player, BaseProtocol baseProtocol)
        {
            //玩家胜利
            //消息结构: (string)PlayerSuccess
            if (player.playerStatus.status != PlayerStatus.Status.Gaming)
            {
                return;
            }
            Room room = player.playerStatus.room;
            int startIndex = 0;
            BytesProtocol p = (BytesProtocol)baseProtocol;
            p.GetString(startIndex, ref startIndex);
            int DoorID = p.GetInt(startIndex, ref startIndex);

            BytesProtocol doorProtocol = new BytesProtocol();
            doorProtocol.SpliceString("DoorOpen");
            doorProtocol.SpliceInt(DoorID);
            room.Broadcast(doorProtocol);
        }

       
    }
}
