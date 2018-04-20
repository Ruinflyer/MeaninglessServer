using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeaninglessServer
{
    /// <summary>
    /// 玩家事件处理类
    /// </summary>
    public class handlePlayerEvent
    {
       public void OnConnect(Player player)
        {

        }

        public void OnDisconnect(Player player)
        {
            //玩家断线时离开房间
            if(player.playerStatus.status==PlayerStatus.Status.InRoom)
            {
                Room room = player.playerStatus.room;
                RoomManager.instance.LeaveRoom(player);
                if(room!=null)
                {
                    room.Broadcast(room.GetRoomInfo());
                }
            }
        }
    }
}
