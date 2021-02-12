using System;
using Discord;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace CustomsQueueBot
{
    public class Player
    {
        private string nickname;
        private string discordName;
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
            discordName = "";
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
        public string DiscordName { get => discordName; set => discordName = value; }
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
        public static List<Player> RecentList { get; set; } = new List<Player>();
    }

    public class UpdateMethods
    {
        public static UpdateMethods Update = new UpdateMethods();
        public async Task PlayerList()
        {
            var Message = Caches.Messages.PlayerListEmbed;
            var Channel = Caches.Messages.ReactionMessageChannel;
            SocketGuildUser user;

            var embed = new EmbedBuilder()
                .WithTitle("Current Top 24 q-υωυ-e Listings:")
               .WithDescription("-----------------------------------------------------------------------").
               WithFooter($"{DateTime.Now}");
            List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();

            int counter = 1;
            int maxNumber = CustomsQueueBot.PlayerList.Playerlist.Count;
            if (CustomsQueueBot.PlayerList.Playerlist.Count > 24)
                maxNumber = 24;
            else if (CustomsQueueBot.PlayerList.Playerlist.Count == 0)
                return;


            for (int x = 0; x < maxNumber; x++)
            {
                    Player player = CustomsQueueBot.PlayerList.Playerlist[x];
                    var field = new EmbedFieldBuilder();
                    field.WithName($"{player.DiscordName} ({player.Nickname})")
                        .WithValue($"Status: {(player.IsActive ? "Active" : "`Inactive`")}\nPosition: {counter}\n-----------------------")
                        .WithIsInline(true);
                    counter++;
                    fields.Add(field);
            }

            foreach (var field in fields)
            {
                embed.AddField(field);
            }

            await Message.ModifyAsync(x => x.Embed = embed.Build());
        }


    }
}
