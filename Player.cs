using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CustomsQueueBot
{
    public class Player
    {
        private string nickname;
        private string ign;
        private int gamesPlayed;
        private int elo;
        private ulong discordID;
        private bool isActive;

        public Player()
        {
            nickname = "";
            ign = "";
            gamesPlayed = 0;
            elo = 0;
            discordID = 0;
            isActive = true;
        }
        public ulong DiscordID { get; set; }
        public string Nickname { get; set; }
        public string IGN { get; set; }
        public int GamesPlayed { get; set; }
        public int Elo { get; set; }
        public bool IsActive { get; set; }

    }

    // Global Player class List 
    public class PlayerList
    {
        public static List<Player> Playerlist { get; set; } = new List<Player>();
    }
}
