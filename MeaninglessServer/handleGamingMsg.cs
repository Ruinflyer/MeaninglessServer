﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeaninglessServer
{
    public partial class handlePlayerMsg
    {

        private int randomItemCode = 0;

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
        /// 获取房间玩家信息
        /// </summary>
        public void MsgGetPlayersInfo(Player player, BaseProtocol baseProtocol)
        {
            //消息结构: (string)GetPlayersInfo + (int)PlayerNum +(string)PlayerName1 + ... +(string)PlayerName#
            int startIndex = 0;
            BytesProtocol get = baseProtocol as BytesProtocol;
            get.GetString(startIndex, ref startIndex);
            Room room = player.playerStatus.room;

            BytesProtocol p = new BytesProtocol();
            p.SpliceString("GetPlayersInfo");
            p.SpliceInt(room.playerDict.Count);
            foreach (Player pr in room.playerDict.Values)
            {
                p.SpliceString(pr.name);
            }
            player.Send(p);
        }
        /// <summary>
        /// 获取地图物品数据
        /// </summary>
        public void MsgGetMapItemData(Player player, BaseProtocol baseProtocol)
        {
            int startIndex = 0;
            BytesProtocol protocol = baseProtocol as BytesProtocol;
            protocol.GetString(startIndex, ref startIndex);

            Random ran = new Random((int)Utility.GetTimeStamp());

            if (randomItemCode == 0)
            {
                randomItemCode = ran.Next(1, 999);
            }
            //Console.WriteLine("RandomItemCode Set: " + randomItemCode.ToString());
            BytesProtocol p = new BytesProtocol();
            p.SpliceString("GetMapItemData");
            p.SpliceInt(randomItemCode);
            player.playerStatus.room.Broadcast(p);

            //player.Send(player.playerStatus.room.ItemsProtocol);
        }
        /// <summary>
        /// 玩家加载完毕,定时器开始计时
        /// </summary>
        public void MsgPlayerReady(Player player, BaseProtocol baseProtocol)
        {

            int startIndex = 0;
            BytesProtocol p = baseProtocol as BytesProtocol;
            p.GetString(startIndex, ref startIndex);
            string playerName = p.GetString(startIndex, ref startIndex);
            //int playerstatus = p.GetInt(startIndex, ref startIndex);
            Room room = player.playerStatus.room;

            ////测试单人使用
            //if (room.playerDict.Count == 1)
            //{
            //    BytesProtocol protocol = new BytesProtocol();
            //    protocol.SpliceString("AllPlayerLoaded");
            //    room.Broadcast(protocol);
            //    room.NewTimer();
            //    room.beginTimer = true;
            //    room.LastCirclefieldTime = Utility.GetTimeStamp();
            //    return;
            //}
            if (room.playerReadyDict.Count < room.playerDict.Count)
            {
                lock (room.playerReadyDict)
                {
                    room.playerReadyDict.Add(playerName, true);
                }
                Console.WriteLine("PlayerReadyCount:{0} ", room.playerReadyDict.Count);
            }

            if (room.playerReadyDict.Count == room.playerDict.Count)
            {
                //广播-所有玩家加载完毕信息
                BytesProtocol protocol = new BytesProtocol();
                protocol.SpliceString("AllPlayerLoaded");
                room.Broadcast(protocol);
                room.NewTimer();
                room.beginTimer = true;
                room.LastCirclefieldTime = Utility.GetTimeStamp();

            }

        }
        /// <summary>
        /// 获取下落点
        /// </summary>
        public void MsgDroppoint(Player player, BaseProtocol baseProtocol)
        {
            int startIndex = 0;
            BytesProtocol get = baseProtocol as BytesProtocol;
            get.GetString(startIndex, ref startIndex);
            Room room = player.playerStatus.room;
            BytesProtocol p = new BytesProtocol();
            p.SpliceString("Droppoint");
            Random rand = new Random();
            lock (room.playerDroppoints)
            {
                int index = rand.Next(0, room.playerDroppoints.Count - 1);
                room.playerDroppoints.RemoveAt(index);
                p.SpliceInt(room.playerDroppoints[index]);
            }
            player.Send(p);
        }

        /// <summary>
        /// 更新玩家信息
        /// </summary>
        public void MsgUpdatePlayerInfo(Player player, BaseProtocol baseProtocol)
        {
            //if (player.playerStatus.status != PlayerStatus.Status.Gaming)
            //{
            //    return;
            //}
            BytesProtocol p = baseProtocol as BytesProtocol;
            int startIndex = 0;
            p.GetString(startIndex, ref startIndex);
            float posX = p.GetFloat(startIndex, ref startIndex);
            float posY = p.GetFloat(startIndex, ref startIndex);
            float posZ = p.GetFloat(startIndex, ref startIndex);
            float rotX = p.GetFloat(startIndex, ref startIndex);
            float rotY = p.GetFloat(startIndex, ref startIndex);
            float rotZ = p.GetFloat(startIndex, ref startIndex);
            string CurrentAction = p.GetString(startIndex, ref startIndex);

            player.playerStatus.posX = posX;
            player.playerStatus.posY = posY;
            player.playerStatus.posZ = posZ;
            player.playerStatus.CurrentAction = CurrentAction;
            player.playerStatus.LastUpdateTime = Utility.GetTimeStamp();

            BytesProtocol protocolReturn = new BytesProtocol();
            protocolReturn.SpliceString("UpdatePlayerInfo");
            protocolReturn.SpliceString(player.name);
            protocolReturn.SpliceFloat(player.playerStatus.HP);
            protocolReturn.SpliceFloat(posX);
            protocolReturn.SpliceFloat(posY);
            protocolReturn.SpliceFloat(posZ);
            protocolReturn.SpliceFloat(rotX);
            protocolReturn.SpliceFloat(rotY);
            protocolReturn.SpliceFloat(rotZ);
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
            BytesProtocol p = baseProtocol as BytesProtocol;
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

            room.PlayerSuccess();
        }

        /// <summary>
        /// 玩家被毒圈伤害
        /// </summary>
        public void MsgPlayerPoison(Player player, BaseProtocol baseProtocol)
        {
            //玩家被毒圈伤害协议
            //消息结构:(string)PlayerPoison
            Room room = player.playerStatus.room;
            int startIndex = 0;
            BytesProtocol p = baseProtocol as BytesProtocol;
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
            room.PlayerSuccess();
        }

        /// <summary>
        /// 玩家死亡，自杀时客户端上报
        /// </summary>
        public void MsgPlayerDead(Player player, BaseProtocol baseProtocol)
        {
            int startIndex = 0;
            BytesProtocol p = baseProtocol as BytesProtocol;
            Room room = player.playerStatus.room;
            p.GetString(startIndex, ref startIndex);
            BytesProtocol ret = new BytesProtocol();

            if (room.playerDict.ContainsKey(player.name))
            {
                ret.SpliceString("PlayerDead");
                ret.SpliceString(player.name);
                room.Broadcast(ret);
                RoomManager.instance.LeaveRoom(room.playerDict[player.name]);
            }
            room.PlayerSuccess();
        }

        /// <summary>
        /// 玩家一般魔法,起点位置,起点旋转度
        /// </summary>
        /// <param name="player"></param>
        /// <param name="baseProtocol"></param>
        public void MsgPlayerMagic(Player player, BaseProtocol baseProtocol)
        {

            //玩家魔法
            //消息结构: (string)PlayerMagic  + (string)magicName + (float)posX + (float)posY +(float)posZ+ (float)rotX + (float)rotY + (float)rotZ
            //if (player.playerStatus.status != PlayerStatus.Status.Gaming)
            //{
            //    return;
            //}
            Room room = player.playerStatus.room;

            int startIndex = 0;
            BytesProtocol p = baseProtocol as BytesProtocol;
            p.GetString(startIndex, ref startIndex);
            string magicName = p.GetString(startIndex, ref startIndex);
            float posX = p.GetFloat(startIndex, ref startIndex);
            float posY = p.GetFloat(startIndex, ref startIndex);
            float posZ = p.GetFloat(startIndex, ref startIndex);
            float rotX = p.GetFloat(startIndex, ref startIndex);
            float rotY = p.GetFloat(startIndex, ref startIndex);
            float rotZ = p.GetFloat(startIndex, ref startIndex);

            //转发魔法消息
            BytesProtocol p_broadcast = new BytesProtocol();
            p_broadcast.SpliceString("PlayerMagic");
            p_broadcast.SpliceString(magicName);
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
            BytesProtocol p = baseProtocol as BytesProtocol;
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
        /// 玩家获得有害状态，转发
        /// </summary>
        public void MsgPlayerGetBuff(Player player, BaseProtocol baseProtocol)
        {
            //消息结构: (string)PlayerName + (int)BuffType +(float)buffTime 
            int startIndex = 0;
            BytesProtocol get = baseProtocol as BytesProtocol;
            Room room = player.playerStatus.room;
            get.GetString(startIndex, ref startIndex);
            string PlayerName = get.GetString(startIndex, ref startIndex);
            int bufftype = get.GetInt(startIndex, ref startIndex);
            float bufftime = get.GetFloat(startIndex, ref startIndex);
            if (room.playerDict.ContainsKey(PlayerName))
            {
                room.playerDict[PlayerName].playerStatus.buffType = bufftype;
                room.playerDict[PlayerName].playerStatus.buffTime = bufftime;
            }



            BytesProtocol p = new BytesProtocol();
            p.SpliceString("PlayerGetBuff");
            p.SpliceString(PlayerName);
            p.SpliceInt(bufftype);
            p.SpliceFloat(bufftime);
            foreach (Player pr in room.playerDict.Values)
            {
                p.SpliceString(pr.name);
            }
            player.Send(p);
        }
  

        /// <summary>
        /// 房间门打开转发
        /// </summary>
        /// <param name="player"></param>
        /// <param name="baseProtocol"></param>
        public void MsgDoorOpen(Player player, BaseProtocol baseProtocol)
        {
            //开门
            //消息结构: (string)DoorOpen + (int)DoorID

            Room room = player.playerStatus.room;
            int startIndex = 0;
            BytesProtocol p = baseProtocol as BytesProtocol;
            p.GetString(startIndex, ref startIndex);
            int DoorID = p.GetInt(startIndex, ref startIndex);

            BytesProtocol doorProtocol = new BytesProtocol();
            doorProtocol.SpliceString("DoorOpen");
            doorProtocol.SpliceInt(DoorID);
            room.Broadcast(doorProtocol);
        }

        /// <summary>
        /// 拾取物品
        /// </summary>
        public void MsgPickItem(Player player, BaseProtocol baseProtocol)
        {
            //拾取物品
            //消息结构: (string)PickItem + (int)GroundItemID
            int startIndex = 0;
            BytesProtocol get = baseProtocol as BytesProtocol;
            get.GetString(startIndex, ref startIndex);
            int GroundItemID = get.GetInt(startIndex, ref startIndex);

            BytesProtocol p = new BytesProtocol();
            p.SpliceString("PickItem");
            p.SpliceInt(GroundItemID);
            player.playerStatus.room.Broadcast(p);
        }
        /// <summary>
        /// 玩家扔物品
        /// </summary>
        public void MsgDropItem(Player player, BaseProtocol baseProtocol)
        {
            //拾取物品
            //消息结构: (string)PickItem + (int)GroundItemID
            int startIndex = 0;
            BytesProtocol p = baseProtocol as BytesProtocol;
            p.GetString(startIndex, ref startIndex);
            int GroundItemID = p.GetInt(startIndex, ref startIndex);
            float posX = p.GetFloat(startIndex, ref startIndex);
            float posY = p.GetFloat(startIndex, ref startIndex);
            float posZ = p.GetFloat(startIndex, ref startIndex);

            //转发魔法消息
            BytesProtocol p_broadcast = new BytesProtocol();
            p_broadcast.SpliceString("DropItem");
            p_broadcast.SpliceInt(GroundItemID);
            p_broadcast.SpliceFloat(posX);
            p_broadcast.SpliceFloat(posY);
            p_broadcast.SpliceFloat(posZ);
            player.playerStatus.room.Broadcast(p);
        }




<<<<<<< HEAD
            BytesProtocol p = new BytesProtocol();
            p.SpliceString("PlayerGetBuff");
            p.SpliceString(PlayerName);
            p.SpliceInt(bufftype);
            p.SpliceFloat(bufftime);
            room.Broadcast(p);
        }

=======
>>>>>>> 3976aff33d9b12c11c80ab31dc0ab20e1a6d1a96
        /// <summary>
        /// 玩家戴头盔
        /// </summary>
        public void MsgPlayerEquipHelmet(Player player, BaseProtocol baseProtocol)
        {
            int startIndex = 0;
            BytesProtocol get = baseProtocol as BytesProtocol;
            Room room = player.playerStatus.room;
            get.GetString(startIndex, ref startIndex);
            int ItemID = get.GetInt(startIndex, ref startIndex);
            player.playerStatus.HeadItemID = ItemID;

            BytesProtocol p = new BytesProtocol();
            p.SpliceString("PlayerEquipHelmet");
            p.SpliceString(player.name);
            p.SpliceInt(player.playerStatus.HeadItemID);
            room.Broadcast(p);
        }
        /// <summary>
        /// 玩家拿衣服
        /// </summary>
        public void MsgPlayerEquipClothe(Player player, BaseProtocol baseProtocol)
        {
            int startIndex = 0;
            BytesProtocol get = baseProtocol as BytesProtocol;
            Room room = player.playerStatus.room;
            get.GetString(startIndex, ref startIndex);
            int ItemID = get.GetInt(startIndex, ref startIndex);
            player.playerStatus.BodyItemID = ItemID;

            BytesProtocol p = new BytesProtocol();
            p.SpliceString("PlayerEquipClothe");
            p.SpliceString(player.name);
            p.SpliceInt(player.playerStatus.BodyItemID);
            room.Broadcast(p);
        }
        /// <summary>
        /// 玩家装武器
        /// </summary>
        public void MsgPlayerEquipWeapon(Player player, BaseProtocol baseProtocol)
        {
            int startIndex = 0;
            BytesProtocol get = baseProtocol as BytesProtocol;
            Room room = player.playerStatus.room;
            get.GetString(startIndex, ref startIndex);
            int ItemID = get.GetInt(startIndex, ref startIndex);
            player.playerStatus.WeaponID = ItemID;

            BytesProtocol p = new BytesProtocol();
            p.SpliceString("PlayerEquipWeapon");
            p.SpliceString(player.name);
            p.SpliceInt(player.playerStatus.WeaponID);
            room.Broadcast(p);
        }




    }
}
