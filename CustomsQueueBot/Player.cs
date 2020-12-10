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
        private int playerLevel;

        public Player()
        {
            nickname = "";
            ign = "";
            gamesPlayed = 0;
            elo = 0;
            discordID = 0;
            isActive = true;
            playerLevel = 0;
        }
        public ulong DiscordID { get => discordID; set => discordID = value; }
        public string Nickname { get => nickname; set => nickname = value; }
        public string IGN { get => ign; set => ign = value; }
        public int GamesPlayed { get => gamesPlayed; set => gamesPlayed = value; }
        public int Elo { get => elo; set => elo = value; }
        public bool IsActive { get => isActive; set => isActive = value; }
        public int PlayerLevel { get => playerLevel; set => playerLevel = value; }
    }

    // Global Player class List 
    public class PlayerList
    {
        public static List<Player> Playerlist { get; set; } = new List<Player>();
        public static List<Player> PlayerlistDB { get; set; } = new List<Player>();
    }
}
