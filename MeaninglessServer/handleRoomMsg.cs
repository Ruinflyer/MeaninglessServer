using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeaninglessServer
{
    public partial class handlePlayerMsg
    {
        public void MsgGetRoomList(Player player, BaseProtocol protocol)
        {
            player.Send(RoomManager.instance.GetRoomList());
        }
        public void MsgCreateRoom(Player player, BaseProtocol baseProtocol)
        {
            BytesProtocol bytesProtocol = new BytesProtocol();
            bytesProtocol.SpliceString("CreateRoom");

            //玩家的状态不是在房间中或战局中时，创建失败，返回-1
            if (player.playerStatus.status != PlayerStatus.Status.Null)
            {
                bytesProtocol.SpliceInt(-1);
                player.Send(bytesProtocol);
                Console.WriteLine("MsgCreateRoom 失败,创建提出者：" + player.name);
                return;
            }
            RoomManager.instance.CreateRoom(player);
            //创建成功 返回0
            bytesProtocol.SpliceInt(0);
            player.Send(bytesProtocol);
            Console.WriteLine("MsgCreateRoom 创建房间完成, 房主是: " + player.name);
        }
        public void MsgJoinRoom(Player player, BaseProtocol baseProtocol)
        {
            int startIndex = 0;
            BytesProtocol Protocol = baseProtocol as BytesProtocol;
            string methodName = Protocol.GetString(startIndex, ref startIndex);
            int RoomIndex = Protocol.GetInt(startIndex, ref startIndex);
            Console.WriteLine("[客户端 " + player.name + " ]" + "请求加入房间(MsgJoinRoom)：index：" + RoomIndex);
            Protocol = new BytesProtocol();
            Protocol.SpliceString("JoinRoom");
            if (RoomIndex < 0 || RoomIndex >= RoomManager.instance.RoomList.Count)
            {
                Console.WriteLine("[客户端 " + player.name + " ]" + "请求加入房间(MsgJoinRoom)：index：" + RoomIndex + " 超出列表范围");
                Protocol.SpliceInt(-1);
                player.Send(Protocol);
                return;
            }
            Room room = RoomManager.instance.RoomList[RoomIndex];
            if (room.status != Room.Status.Preparing)
            {
                Console.WriteLine("[客户端 " + player.name + " ]" + "请求加入房间(MsgJoinRoom)：index：" + RoomIndex + " 房间正在游玩");
                Protocol.SpliceInt(-1);
                player.Send(Protocol);
                return;
            }
            if (room.AddPlayer(player))
            {
                room.Broadcast(room.GetRoomInfo());
                Protocol.SpliceInt(0);
                player.Send(Protocol);
            }
            else
            {
                Console.WriteLine("[客户端 " + player.name + " ]" + "请求加入房间(MsgJoinRoom)：index：" + RoomIndex + " 房间人数已满");
                Protocol.SpliceInt(-1);
                player.Send(Protocol);
            }

        }
        /// <summary>
        /// 获取房间消息
        /// </summary>
        /// <param name="player"></param>
        /// <param name="baseProtocol"></param>
        public void MsgGetRoomInfo(Player player, BaseProtocol baseProtocol)
        {
            if (player.playerStatus.status != PlayerStatus.Status.InRoom)
            {
                Console.WriteLine("[客户端 " + player.name + " ]" + "请求获取房间消息(MsgGetRoomInfo)：玩家不在房间中，不需获取");
                return;
            }
            //返回所在房间信息
            player.Send(player.playerStatus.room.GetRoomInfo());
        }
        public void MsgLeaveRoom(Player player, BaseProtocol baseProtocol)
        {
            BytesProtocol protocol = new BytesProtocol();
            protocol.SpliceString("LeaveRoom");
            if (player.playerStatus.status != PlayerStatus.Status.InRoom)
            {
                Console.WriteLine("[客户端 " + player.name + " ]" + "请求离开房间(MsgLeaveRoom)：玩家不在房间中，离开失败");
                protocol.SpliceInt(-1);
                player.Send(protocol);
                return;
            }
            protocol.SpliceInt(0);
            player.Send(protocol);
            Room room = player.playerStatus.room;
            RoomManager.instance.LeaveRoom(player);
            //离开房间时，房间仍有人，则广播新的房间玩家信息
            if (room != null)
            {
                room.Broadcast(room.GetRoomInfo());
            }

        }
    }
}
