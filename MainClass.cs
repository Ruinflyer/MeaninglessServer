using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace MeaninglessServer
{
    class MainClass
    {

        public static void Main(string[] args)
        {
            RoomManager roomManager = new RoomManager();
            ServerConf serverConf = Utility.LoadServerConf();
            Server server = new Server();
            server.Start(serverConf.ServerHost, serverConf.ServerPort, serverConf.Connects,serverConf.HeartBeatTime);
            Console.ReadLine();
        }
    }


}
