using System;
using Discord;
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
        private bool playsFrontline;
        private bool playsSupport;
        private bool playsFlank;
        private bool playsDamage;
        private bool isBanned;
        private string bannedReason;

        public Player()
        {
            nickname = "";
            ign = "";
            gamesPlayed = 0;
            elo = 0;
            discordID = 0;
            playerLevel = 0;
            isActive = true;
            playsFrontline = false;
            playsSupport = false;
            playsFlank = false;
            playsDamage = false;
            isBanned = false;
            bannedReason = "";

        }
        public ulong DiscordID { get => discordID; set => discordID = value; }
        public string Nickname { get => nickname; set => nickname = value; }
        public string IGN { get => ign; set => ign = value; }
        public int GamesPlayed { get => gamesPlayed; set => gamesPlayed = value; }
        public int Elo { get => elo; set => elo = value; }
        public bool IsActive { get => isActive; set => isActive = value; }
        public int PlayerLevel { get => playerLevel; set => playerLevel = value; }
        public bool PlaysFrontline { get => playsFrontline; set => playsFrontline = value; }
        public bool PlaysSupport { get => playsSupport; set => playsSupport = value; }
        public bool PlaysFlank { get => playsFlank; set => playsFlank = value; }
        public bool PlaysDamage { get => playsDamage; set => playsDamage = value; }
        public bool IsBanned { get => isBanned; set => isBanned = value; }
        public string BannedReason { get => bannedReason; set => bannedReason = value; }
    }

    // Global Player class List 
    public class PlayerList
    {
        public static List<Player> Playerlist { get; set; } = new List<Player>();
        public static List<Player> PlayerlistDB { get; set; } = new List<Player>();
        public static List<Player> Bannedlist { get; set; } = new List<Player>();
    }

    public class UpdateMethodTest
    {
        
    }
}
