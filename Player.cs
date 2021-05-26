using System;
using Discord;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace CustomsQueueBot
{
    public class Player
    {

        private int gamesPlayed;
        private int elo;
        private bool isActive;
        private int playerLevel;
        private bool playsTank;
        private bool playsHealer;
        private bool playsFlank;
        private bool playsDamage;
        private bool isBanned;
        private string bannedReason;
        private DateTime entryTime;
        private SocketGuildUser guildUser;

        public Player()
        {
            GamesPlayed = 0;
            Elo = 0;
            PlayerLevel = 0;
            IsActive = true;
            PlaysTank = false;
            PlaysHealer = false;
            PlaysFlank = false;
            PlaysDamage = false;
            IsBanned = false;
            BannedReason = "";

        }

        public int GamesPlayed { get => gamesPlayed; set => gamesPlayed = value; }
        public int Elo { get => elo; set => elo = value; }
        public bool IsActive { get => isActive; set => isActive = value; }
        public int PlayerLevel { get => playerLevel; set => playerLevel = value; }
        public bool PlaysTank { get => playsTank; set => playsTank = value; }
        public bool PlaysHealer { get => playsHealer; set => playsHealer = value; }
        public bool PlaysFlank { get => playsFlank; set => playsFlank = value; }
        public bool PlaysDamage { get => playsDamage; set => playsDamage = value; }
        public bool IsBanned { get => isBanned; set => isBanned = value; }
        public string BannedReason { get => bannedReason; set => bannedReason = value; }
        public DateTime EntryTime { get => entryTime; set => entryTime = value; }
        public SocketGuildUser GuildUser { get => guildUser; set => guildUser = value; }
    }

    public class UpdateMethods
    {
        public static UpdateMethods Update = new UpdateMethods();
        public async Task PlayerList()
        {
            var Message = Caches.Messages.PlayerListEmbed;
            var Channel = Caches.Messages.ReactionMessageChannel;
           // SocketGuildUser user;

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
                    field.WithName($"{player.GuildUser.Nickname} ({player.GuildUser.Username})")
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
