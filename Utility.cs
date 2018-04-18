using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.IO;


namespace MeaninglessServer
{
    public class Utility
    {

        /// <summary>
        /// 获取1970年1月1日零点到现在的时间戳
        /// </summary>
        /// <returns></returns>
        public static long GetTimeStamp()
        {
            TimeSpan timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);

            return Convert.ToInt64(timeSpan.TotalSeconds);

        }

        /// <summary>
        /// 从文件加载Json反序列化
        /// </summary>
        /// <typeparam name="T">序列化后的类</typeparam>
        /// <param name="JsonFilePath">Json文件路径</param>
        /// <returns></returns>
        public static T LoadJsonFromFile<T>(string JsonFilePath)
        {
            JsonFilePath = Directory.GetCurrentDirectory() + JsonFilePath;
            if (!File.Exists(JsonFilePath))
            {
                Console.WriteLine("加载Json文件 " + JsonFilePath + " 失败,文件不存在");
                return default(T);
            }

            FileStream jsonFileStream = new FileStream(JsonFilePath, FileMode.Open);
            StreamReader jsonFileStreamR = new StreamReader(jsonFileStream, System.Text.Encoding.UTF8);
            string json = jsonFileStreamR.ReadToEnd();
            jsonFileStreamR.Close();
            jsonFileStream.Close();

            if (json.Length > 0)
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            else
            {
                Console.WriteLine("加载Json文件 " + JsonFilePath + " 失败,文件为空");
                return default(T);
            }
        }

        public static ServerConf LoadServerConf()
        {
            return LoadJsonFromFile<ServerConf>("/Configure/ServerConf.json");
        }

        public static double NextDouble(Random random, double miniDouble, double maxiDouble)
        {
            if (random != null)
            {
                return random.NextDouble() * (maxiDouble - miniDouble) + miniDouble;
            }
            else
            {
                return 0.0d;
            }
        }
    }
}
