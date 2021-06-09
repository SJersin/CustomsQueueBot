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
using System.Text;
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
                bot = new BotConfig
                {
                    Prefix = "+",
                    GroupSize = 8,
                    MaxSize = 10,
                    MessageSize = 2,
                    MinGamesPlayed = 2,
                    NumberOfVotes = 3
                };

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

        public string Token { get; set; }
        public string Prefix { get; set; }
        public string Reaction { get; set; }
        public string Role { get; set; }
        public int GroupSize { get; set; }
        public int MaxSize { get; set; }
        public int MessageSize { get; set; }
        public int MinGamesPlayed { get; set; }
        public int NumberOfVotes { get; set; }

        public override string ToString()
        {
            string toReturn = "";

            toReturn += "Prefix:\t" + Prefix + "\nReaction:\t" + Reaction + "\nRole:\t" + Role + "\nGroup size:\t" + GroupSize +
                "\nMax group size:\t" + MaxSize + "\nMessage cache size:\t" + MessageSize + "\nMin # of games:\t" + MinGamesPlayed +
                "\nVote Count:\t" + NumberOfVotes;


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
