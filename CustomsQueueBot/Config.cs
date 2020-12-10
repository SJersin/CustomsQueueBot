/*
    Configuration of the bot creating or reading the json file
    that will contain the the bots Authentication Token and
    the command prefix to call on the bot's functions.
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
    public struct BotConfig
    {
        public string token { get; set; }
        public string prefix { get; set; }
        public string reaction { get; set; }
        public string webhook { get; set; }

    }
}
