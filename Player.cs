using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeaninglessServer
{
    public class Player
    {
        public string name;
        public Connect connect;
        public PlayerStatus playerStatus;

        public Player(string name, Connect connect)
        {
            this.name = name;
            this.connect = connect;
            playerStatus = new PlayerStatus();
        }

        public void Send(BaseProtocol Protocol)
        {
            if (connect == null)
            {
                return;
            }
            Server.instance.Send(connect, Protocol);
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <returns></returns>
        public bool Disconnect()
        {
            Server.instance.handlePlayerEvent.OnDisconnect(this);

            connect.player = null;
            connect.Close();
            return true;
        }

        public static bool NameIsUsed(string name)
        {
            Connect[] connects = Server.instance.connects;
            for (int i = 0; i < connects.Length; i++)
            {
                if (connects[i]==null || connects[i].isUse==false || connects[i].player == null)
                {
                    continue;
                }
                if(connects[i].player.name==name)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
