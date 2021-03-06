﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeaninglessServer
{
    public class RoomManager
    {
        public static RoomManager instance;
        private CirclefieldInfo circlefieldInfo;
        public RoomManager()
        {
            instance = this;
        }

        public List<Room> RoomList = new List<Room>();

        public void CreateRoom(Player player)
        {
            Room room = new Room();
            lock (RoomList)
            {
                RoomList.Add(room);
                room.AddPlayer(player);
                room.circlefieldInfo = GetCirclefieldInfo();

                room.DroppointNum = 10;
                for (int i=0;i<10;i++)
                {
                    //初始化10个下落点
                    room.playerDroppoints.Add(i);
                }
                
                
            }

        }

        public void LeaveRoom(Player player)
        {
            PlayerStatus playerStatus = player.playerStatus;
            if (playerStatus.status == PlayerStatus.Status.Null)
            {
                return;
            }


            lock (RoomList)
            {
                playerStatus.room.DelPlayer(player.name);

                //当房间为空，即移除房间
                if (playerStatus.room.playerDict.Count == 0)
                {
                    RoomList.Remove(playerStatus.room);
                }
            }
        }

        public BytesProtocol GetRoomList()
        {
            BytesProtocol protocol = new BytesProtocol();
            protocol.SpliceString("GetRoomList");
            protocol.SpliceInt(RoomList.Count);
            for (int i = 0; i < RoomList.Count; i++)
            {
                
                //房间人数
                protocol.SpliceInt(RoomList[i].playerDict.Count);
                //房间状态
                protocol.SpliceInt((int)RoomList[i].status);
            }
            return protocol;
        }

        //读取毒圈配置
        public CirclefieldInfo GetCirclefieldInfo()
        {
            if (circlefieldInfo == null)
            {
                circlefieldInfo = new CirclefieldInfo();
                circlefieldInfo = Utility.LoadJsonFromFile<CirclefieldInfo>("/Configure/Circlefield.json");
            }
            return circlefieldInfo;
        }

    }
}
