using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Logger
    {

        private static List<string> msgList = new List<string>();

        public static void AddLog(string msg)
        {
            msgList.Add(msg);
        }

        public static string GetLog()
        {
            if(msgList.Count > 0)
            {
                string s = msgList[0];
                msgList.RemoveAt(0);
                return s;
            }
            return string.Empty;
        }

    }
}
