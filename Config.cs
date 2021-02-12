/*
 *
 *  Configuration of the bot creating or reading the json file
 *  that will contain the bot's:
 *  
 *    -Authentication Token 
 *    -Command prefix to call on the bot's functions
 *    -Unicode reaction emoji or emote string name
 *    -Group size integer for default number of players
 *
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq; //Using Lists
using System.IO;
using Newtonsoft.Json;  //Using Json files

namespace CustomsQueueBot
{
    class Config
    {
        private const string configFolder = "Resources";
        private const string configFile = "config.json";
        public static BotConfig bot;

        static Config()
        {
            if (!Directory.Exists(configFolder))    //Create directory
                Directory.CreateDirectory(configFolder);

            if (!File.Exists(configFolder + "/" + configFile)) //Create file
            {
                bot = new BotConfig();
                bot.groupsize = 8;
                bot.messagesize = 5;
                string json = JsonConvert.SerializeObject(bot, Formatting.Indented); //Json file creation
                File.WriteAllText(configFolder + "/" + configFile, json);
            }
            else
            {
                string json = File.ReadAllText(configFolder + "/" + configFile);  //Json file found and read
                bot = JsonConvert.DeserializeObject<BotConfig>(json);
            }
        }
    }
    public class BotConfig
    {
        private const string configFolder = "Resources";
        private const string configFile = "config.json";

        public string token { get; set; }
        public string prefix { get; set; }
        public string reaction { get; set; }
        public string role { get; set; }
        public int groupsize { get; set; }
        public int messagesize { get; set; }

        public override string ToString()
        {
            string toReturn = "";

            toReturn += "Token: " + token + "\nPrefix:\t" + prefix + "\nReaction:\t" + reaction + "\nRole:\t" + role + "\nGroup size:\t" + groupsize 
                + "\nMessage cache size:\t" + messagesize;


            return toReturn;
        }

        public bool ReloadConfigFile()
        {
            bool check;

            try
            {
                string json = File.ReadAllText(configFolder + "/" + configFile);  //Json file found and read
                Config.bot = JsonConvert.DeserializeObject<BotConfig>(json);
                check = true;
            }
            catch
            {
                check = false;
            }
            return check;
        }
    }
}
