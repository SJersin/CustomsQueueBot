using System.Collections.Generic;

namespace CustomsQueueBot
{
    // Global Player class List 
    public class PlayerList
    {
        public static List<Player> Playerlist { get; set; } = new List<Player>();
        public static List<Player> PlayerlistDB { get; set; } = new List<Player>();
        public static List<Player> Bannedlist { get; set; } = new List<Player>();
        public static List<Player> RecentList { get; set; } = new List<Player>();
    }
}
