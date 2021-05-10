using System;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CustomsQueueBot
{

    public class Logs
    {
        private const string configFolder = "Resources";
        private const string errorLogFile = "ErrorLog";
        private const string debugLogFile = "DebugLog";
        private const string errorLogPath = configFolder + "/" + errorLogFile;
        private const string debugLogPath = configFolder + "/" + debugLogFile;
        // public static string log { get; set; }

        public static Logs Log = new Logs();
        private string errorMessage;
        private string debugMessage;


        public void WriteErrorLog(string report)
        {
            File.AppendAllText(errorLogPath + "_" + DateTime.Today, report);
        }

        public void WriteDebugLog(string debug)
        {
            File.AppendAllText(debugLogPath + "_" + DateTime.Today, debug);
        }

    }

    public class Checks
    {

        public static IUserMessage MessageToCheck { get; set; }
        public static IUserMessage EmbedToCheck { get; set; }

        public static IResult MessageResult { get; set; }
        public static IResult EmbedResult { get; set; }


        public static Checks Check = new Checks();
        public bool CheckMessage(IResult result)
        {
            bool x = true;

            return x;
        }

    }

    public class Debug
    {
        
    }

    public class Error
    {

    }

}
