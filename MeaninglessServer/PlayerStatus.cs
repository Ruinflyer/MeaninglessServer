using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeaninglessServer
{
    public class PlayerStatus
    {

        public enum Status
        {
            Null = 0,
            InRoom,
            Gaming
        }
        //所属房间
        public Room room;
        //当前状态
        public Status status;
        //是否为房主
        public bool isMaster = false;

        public float HP;
        public float posX;
        public float posY;
        public float posZ;
        public int HeadItemID;
        public int BodyItemID;
        public int WeaponID;
        public string CurrentAction;

        public long LastUpdateTime;
        public PlayerStatus()
        {

        }

    }
}
