using System;
using System.Timers;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System.Linq;
using Discord.WebSocket;
using System.Collections;
using Microsoft.Extensions.Logging;

/*
 * 0.6c Changes to file:
 * 
 * -Last Call disabled to rework logic.
 * -Next, Random and Last Call commands now auto-delete previous messages to avoid clutter. Will update config file later.
 * -Next, Random and Last Call now DM Lobby information as well as @mention pinging them in the game channel.
 * -Fixed issue where users can be added to the queue list twice after being banned or removed.
 * 
 * 
 * 
 * Things to work on:
 * - Last Call cycle logic
 * - Why does list/mlist/qlist stop working at times??
 * - Why does next not work sometimes??
 * 
 */

namespace CustomsQueueBot.Core.Commands
{
    public class QueueCommands : ModuleBase<SocketCommandContext>
    {
        [Command("create")]
        [Alias("cq", "+q", "createqueue")]
        [Summary(": Create a new queue for users to join.\nYou must pass a user role @mention.\nex: `create @customs`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task NewQueue(IRole role)
        {
            await Context.Channel.TriggerTypingAsync();
            Timer timer = new Timer(120000); // Timer for auto-posting list embed

            var leader = Context.Guild.GetUser(Context.User.Id);
            var embed = new EmbedBuilder();
            var field = new EmbedFieldBuilder();

            // Create message with reaction for queue
            embed.WithColor(Color.DarkGreen)
                .WithTitle($"Hey { role.Name }! The customs games queue is now open!")
                .WithTimestamp(DateTime.Now);

            field.WithName("Click or Tap on the reaction to join queue.")
                .WithValue("Removing your reaction will set you as inactive.\nThis will skip over your turn when " +
                "lobbies are called,\nbut you will retain your position in line.\nReact to this message " +
                "again to become active and stop being skipped." +
                "\nReact to this message only if you are not already in either\na custom lobby or a custom match.");

            // Put timer and code for posting list after timer expires here
            var embed2 = new EmbedBuilder()
                .WithTitle($"Current Player q-υωυ-e Listings ({PlayerList.Playerlist.Count()})")
                .WithDescription("-------------------------------------------------------")
                .WithFooter($"{DateTime.Now}");

            Caches.Messages.PlayerListEmbed = await Context.Channel.SendMessageAsync(embed: embed2.Build());

            embed.AddField(field);
            Caches.Messages.ReactionMessage = await Context.Channel.SendMessageAsync(embed: embed.Build());    // Sends the embed for people to react to.
            var emote = new Emoji(Config.bot.reaction);  //"👍"
            //var emote = Emote.Parse(Config.bot.reaction);
             
            await Caches.Messages.ReactionMessage.AddReactionAsync(emote);

            Caches.Messages.ReactionMessageChannel = Caches.Messages.ReactionMessage.Channel;
            Caches.IsOpen.isOpen = true;

        }

        [Command("close")]
        [Alias("removequeue", "-q", "closequeue")]
        [Summary(": Close off the queue and clear the playerlist")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task CloseQueue()
        {
            await Context.Channel.TriggerTypingAsync();
            PlayerList.Playerlist.Clear();
            PlayerList.PlayerlistDB.Clear();
            PlayerList.Bannedlist.Clear();

            foreach (var lobbyMessage in Caches.Messages.LobbyMessages)
            {
                await lobbyMessage.DeleteAsync();
            }

            var embed = new EmbedBuilder().WithTitle("The customs queue has been closed!")
                .WithColor(Color.DarkRed)
                .WithDescription("The queue has been closed and the list has been emptied.\nThank you everyone " +
                "who joined in today's session!!");

            await Caches.Messages.ReactionMessage.DeleteAsync();      //Context.Channel.DeleteMessageAsync(Caches.Messages.ReactionMessage);  // Delete the message with reacts
            await Caches.Messages.PlayerListEmbed.DeleteAsync();      //Context.Channel.DeleteMessageAsync(Caches.Messages.PlayerListEmbed);
            await ReplyAsync(embed: embed.Build());
            Caches.IsOpen.isOpen = false;
        }

/*        [Command("mlist")]
        [Summary(": Shows all players in the playerlist, their current active status and total number of games played.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task ShowModPlayerList()
        {
            if (!Caches.IsOpen.isOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                return;
            }
            await Context.Channel.TriggerTypingAsync();

            string names = "";
            List<EmbedFieldBuilder> PlayerListField = new List<EmbedFieldBuilder>();
            var embed = new EmbedBuilder()
                .WithTitle($"There are {PlayerList.Playerlist.Count} players in the list")
                .WithCurrentTimestamp();

            if (PlayerList.Playerlist.Count == 0)
            {
                embed.WithTitle("The list is Empty.")
                    .WithDescription("Get people in the list!");
            }
            else
            {
                foreach (Player player in PlayerList.Playerlist)
                {
                    names = player.Nickname;

                    EmbedFieldBuilder field = new EmbedFieldBuilder();

                    PlayerListField.Add(field.WithName(names)
                    .WithValue($"Active: {(player.IsActive ? "Yes" : "No")}\nGames Played: {player.GamesPlayed}")
                    .WithIsInline(true));

                }
                foreach (EmbedFieldBuilder field in PlayerListField)
                    embed.AddField(field);
            }
            await ReplyAsync(embed: embed.Build());
        }

*/

        [Command("list")]
        [Summary(": Provides the list of everyone in the queue database.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task ShowQueueList()
        {
            if (!Caches.IsOpen.isOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                return;
            }
            await Context.Channel.TriggerTypingAsync();

            List<EmbedFieldBuilder> PlayerListField = new List<EmbedFieldBuilder>();
            var embed = new EmbedBuilder()
                .WithTitle($"There are {PlayerList.Playerlist.Count} players in the list")
                .WithCurrentTimestamp();

            if (PlayerList.PlayerlistDB.Count != 0)
            {
                foreach (Player player in PlayerList.PlayerlistDB)
                {
                    
                    EmbedFieldBuilder field = new EmbedFieldBuilder();

                    PlayerListField.Add(field.WithName(player.Nickname)
                    .WithValue($"Active: {(player.IsActive ? "Yes" : "`No`")}\nGames Played: {player.GamesPlayed}\nBanned: {(player.IsBanned ? "`Yes`" : "No")}")
                    .WithIsInline(true));
                }
                foreach (EmbedFieldBuilder field in PlayerListField)
                    embed.AddField(field);

                await ReplyAsync(embed: embed.Build());
            }
            else
            {                
                embed.WithTitle("The database is Empty.")
                    .WithDescription("Get people in the list!");

                await ReplyAsync(embed: embed.Build());
            }
        }

        [Command("next")]
        [Alias("nextgame")]
        [Summary(": Gets and displays [x] number of players for the next lobby" +
            "\nIf no number is provided, default will be 8.\nA third argument " +
            "can be passed for the password.\nex: `next [password]` or `next [x] [password]`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task NextGroupSize(string arg = "", [Remainder] string password = "")
        {
            // Start checks


            if (!Caches.IsOpen.isOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                return;
            }

            int count = 0;
            int groupSize = Config.bot.groupsize;
            // Check for first argument: Is it a different group size number or just a password?

            try
            {
                groupSize = int.Parse(arg);
            }
            catch
            {
                password = arg;
            }

            // Check for number of active players
            foreach (Player active in PlayerList.Playerlist)
            {
                if (active.IsActive)
                    count++;
            }

            Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - Active player count: {count}");

            if (groupSize >= count) // If groupSize is larger than or equal to the count of active people--
            {
                groupSize = count; // Set groupSize to number of active players.
                //foreach (Player player in PlayerList.PlayerlistDB) // Repopulate player list by active and not banned players.
                //{
                //    if (player.IsActive && !player.IsBanned)
                //    { 
                //        PlayerList.Playerlist.Add(player);
                //        Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - Active player: {player.Nickname} added.");
                //    }
                //}

            }
            // End of Checks

            // Pull the next group from the queue list and displays their names.

            await Context.Channel.TriggerTypingAsync();
            var leader = Context.Guild.GetUser(Context.User.Id);

            // Create and display embed for users selected for next game.
            Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - Create Embed Builder");
            var embed = new EmbedBuilder().WithTitle($"{leader.Username}'s Next Lobby Group:")
                .WithDescription($"Here are the next {groupSize} players for {leader.Username}'s lobby.\nThe password is: ` {password} `\n*Only join this lobby if your name is in this list!*")
                .WithColor(Color.DarkGreen);

            List<EmbedFieldBuilder> PlayerListField = new List<EmbedFieldBuilder>();

            int index = 0;
            int ListPos = 0;
            string mentions = "";
            List<Player> players = new List<Player>();

            while (index < groupSize)
            {
                if (PlayerList.Playerlist[ListPos].IsActive && !PlayerList.Playerlist[ListPos].IsBanned)
                {
                    players.Add(PlayerList.Playerlist[ListPos]);
                    index++;
                    ListPos++;
                }

                else  //Skip inactive players
                {
                    ListPos++;
                }

            }

            foreach (Player player in players)
            {
                Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - Foreach player in players.");

                var user = Context.Guild.GetUser(player.DiscordID);
                Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - Building fields");
                EmbedFieldBuilder field = new EmbedFieldBuilder();
                field.WithName(player.Nickname)
                    .WithValue($"*--------------------------*")
                    .WithIsInline(true);
                Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - field built for {player.Nickname}");

                PlayerListField.Add(field);
                PlayerList.Playerlist.Remove(player);
                PlayerList.PlayerlistDB[PlayerList.PlayerlistDB.IndexOf(player)].GamesPlayed += 1;
                PlayerList.Playerlist.Add(player);

                mentions += $"{user.Mention} "; // @mentions the players
                Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - Adding Mentions");
                await user.SendMessageAsync($"You are in {leader.Username}'s lobby. The password is: ` {password} ` .");
                Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - Direct Message sent.");

            }
            foreach (EmbedFieldBuilder field in PlayerListField)
            {
                embed.AddField(field);
                Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - field embedded");
            }

            var Messagae = await Context.Channel.SendMessageAsync(mentions, embed: embed.Build());
            Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - Mentions and Embeds messaged to channel.");
            await UpdateList();
            Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - Player list update method.");
            Caches.Messages.LobbyMessages.Add(Messagae);

            if (Caches.Messages.LobbyMessages.Count() > 3) // Only have 3 embed messages showing at a time. Will softode this to the config file later. I hope.
            {
                await Caches.Messages.LobbyMessages[0].DeleteAsync();
                Caches.Messages.LobbyMessages.RemoveAt(0);
            }
        }

/*        [Command("next?")]
        [Alias("nextgame?")]
        [Summary(": Gets and displays [x] number of players for the next lobby" +
            "\nIf no number is provided, default will be 8.\nThis will not actively " +
            "pull from the queue but merely display whom would be pulled next " +
            "based on current player activity modifiers")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task NextGroupList()
        {
            // Start checks


            if (!Caches.IsOpen.isOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                return;
            }

            int count = 0;
            int groupSize = Config.bot.groupsize;

            // Check for number of active players
            foreach (Player active in PlayerList.Playerlist)
            {
                if (active.IsActive)
                    count++;
            }

            Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - Active player count: {count}");

            if (groupSize >= count) // If groupSize is larger than or equal to the count of active people--
            {
                groupSize = count; // Set groupSize to number of active players.
            }
            // End of Checks

            // Pull the next group from the queue list and displays their names.
            // Maybe have them get DMed the information(?)
            await Context.Channel.TriggerTypingAsync();

            // Create and display embed for users selected for next game.
            var embed = new EmbedBuilder().WithTitle($"Expected Next Lobby Group:")
                .WithDescription($"Here is the next group of players that should be called upon for the next lobby.\n*This is only an approximation.*")
                .WithColor(Color.DarkGreen);

            List<EmbedFieldBuilder> PlayerListField = new List<EmbedFieldBuilder>();

            int index = 0;
            int ListPos = 0;
            string mentions = "";
            List<Player> players = new List<Player>();

            while (index < groupSize)
            {
                if (PlayerList.Playerlist[ListPos].IsActive && !PlayerList.Playerlist[ListPos].IsBanned)
                {
                    players.Add(PlayerList.Playerlist[ListPos]);
                    index++;
                    ListPos++;
                }

                else  //Skip inactive players
                {
                    ListPos++;
                }

            }

            foreach (Player player in players)
            {
                EmbedFieldBuilder field = new EmbedFieldBuilder();

                field.WithName($"{player.Nickname}")
                    .WithValue($"-------------------")
                    .WithIsInline(true);
                PlayerListField.Add(field);

            }
            foreach (EmbedFieldBuilder field in PlayerListField)
            {
                embed.AddField(field);
            }

            Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - Sending Mentions and Embed");
            var Messagae = await Context.Channel.SendMessageAsync(mentions, embed: embed.Build());
            Caches.Messages.LobbyMessages.Add(Messagae.Id);
        }
        */

        [Command("random")]
        [Summary(": Gets and displays [x] number of random players for the next lobby" +
            "\nIf no number is provided, default will be 8.\nA third argument can be " +
            "passed for the password.\nex: `random [password]` or `random [x] [password]`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task RandomAsync(string arg = "", [Remainder] string password = "")
        {
            if (!Caches.IsOpen.isOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                return;
            }
            int groupSize = Config.bot.groupsize;
            int count = 0;

            try
            {
                groupSize = int.Parse(arg);
            }
            catch
            {
                password = arg;
            }

            // Check for number of active players
            foreach (Player active in PlayerList.Playerlist)
            {
                if (active.IsActive && !active.IsBanned)
                    count++;
            }

            if (groupSize >= count)
            {
                Console.WriteLine($"{DateTime.Now} => [DEBUGGING] Random: Group size larger than active pool. Repopulating list.");
                groupSize = count;
                //foreach (Player player in PlayerList.PlayerlistDB)
                //{
                //    if (player.IsActive && !player.IsBanned)
                //        PlayerList.Playerlist.Add(player);
                //}

            }

            await Context.Channel.TriggerTypingAsync();
            var leader = Context.Guild.GetUser(Context.User.Id);

            List<EmbedFieldBuilder> PlayerListField = new List<EmbedFieldBuilder>();
            List<Player> players = new List<Player>();
            string mentions = "";

            // Create and display embed for users selected for next game.
            var embed = new EmbedBuilder().WithTitle($"{leader.Username}'s Next Lobby Group:")
                .WithDescription($"Here are the next RANDOM {groupSize} players for {leader.Username}'s lobby.\nThe password is: ` {password} `\n*Only join this lobby if your name is in this list!*")
                .WithColor(Color.DarkGreen);
            var random = new Random();

            HashSet<int> numbers = new HashSet<int>();
            while (numbers.Count < groupSize)
            {
                int index = random.Next(0, PlayerList.Playerlist.Count());

                if (PlayerList.Playerlist[index].IsActive && !PlayerList.Playerlist[index].IsBanned)
                    numbers.Add(index);
            }

            foreach (int number in numbers)
            {
                var field = new EmbedFieldBuilder();
                players.Add(PlayerList.Playerlist[number]);
                PlayerListField.Add(field
                    .WithName(PlayerList.Playerlist[number].Nickname)
                    .WithValue("*--------------------------*")
                    .WithIsInline(true)
                    );
            }

            foreach (Player player in players)
            {
                var user = Context.Guild.GetUser(player.DiscordID);
                PlayerList.PlayerlistDB[PlayerList.PlayerlistDB.IndexOf(player)].GamesPlayed += 1;
                PlayerList.Playerlist.Remove(player);
                PlayerList.Playerlist.Add(player);

                mentions += $"{user.Mention} "; // @mentions the players
                await user.SendMessageAsync($"You are in {leader.Username}'s lobby. The password is: ` {password} ` .");
            }

            foreach (EmbedFieldBuilder field in PlayerListField)
                embed.AddField(field);

            var Messagae = await Context.Channel.SendMessageAsync(mentions, embed: embed.Build());
            await UpdateList();
            Caches.Messages.LobbyMessages.Add(Messagae);


            if (Caches.Messages.LobbyMessages.Count() > 3) // Only have 3 embed messages showing at a time. Will softcode this to the config file later. I hope.
            {
                await Context.Channel.DeleteMessageAsync(Caches.Messages.LobbyMessages[0]);
                Caches.Messages.LobbyMessages.RemoveAt(0);
            }
        }

        /*         [Command("lastcall")]
                [Summary("Gets and displays [x] number of players who have the lowest\n " +
                    "number of games played for the next, usually last, lobby.\nIf no number is provided, " +
                    "default will be 8.\nA third argument can be passed for the password." +
                    "\nex: `lastcall [password]` or `lastcall [x] [password]`")]
                [RequireUserPermission(GuildPermission.ManageChannels)]
               public async Task LastCall(string arg = "", [Remainder] string password = "")
                {
                    // Start checks
        if (!Caches.IsOpen.isOpen)
                    {
                        await Context.Channel.SendMessageAsync("There is no open queue.");
                        return;
                    }            int count = 0, size = 0;
                    int groupSize = Config.bot.groupsize;
                    // Check for first argument: Is it a different group size number or just a password?
                    try
                    {
                        groupSize = int.Parse(arg);
                    }
                    catch
                    {
                        password = arg;
                    }

                    // Check for number of active players
                    foreach (Player active in PlayerList.Playerlist)
                    {
                        if (active.IsActive)
                            count++;
                    }

                    Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : lastcall - Active player count: {count}");

                    if (groupSize >= count)
                    {
                        size = count;
                        foreach (Player player in PlayerList.PlayerlistDB)
                        {
                            if (player.IsActive && !PlayerList.Playerlist.Contains(player))
                                PlayerList.Playerlist.Add(player);
                        }

                    }
                    // End of Checks

                    List<Player> players = new List<Player>();
                    List<Player> playersToAdd = new List<Player>();
                    int x = 0;
                    while (x < groupSize)
                    {
                        players.Add(PlayerList.Playerlist[x]);
                        x++;
                    }

                    x = 0;
                    while (x < groupSize)
                    {
                        foreach (Player player in players)
                        {
                            foreach (Player player1 in PlayerList.Playerlist)
                            {
                                if (player.GamesPlayed > player1.GamesPlayed)
                                {
                                    playersToAdd.Insert(0, player1);
                                }
                            }
                        }

                        x++;
                    }

                    await Context.Channel.TriggerTypingAsync();
                    var leader = Context.Guild.GetUser(Context.User.Id);

                    // Create and display embed for users selected for next game.
                    var embed = new EmbedBuilder().WithTitle($"{leader.Username}'s Next Lobby Group:")
                        .WithDescription($"Here are the next {groupSize} players for {leader.Username}'s lobby.\nThe password is: ` {password} `\n*Only join this lobby if your name is in this list!*")
                        .WithColor(Color.DarkGreen);

                    List<EmbedFieldBuilder> PlayerListField = new List<EmbedFieldBuilder>();
                    string mentions = "";

                    foreach (Player player in playersToAdd)
                    {
                        var user = Context.Guild.GetUser(player.DiscordID); // get the userID to mention

                        EmbedFieldBuilder field = new EmbedFieldBuilder();
                        PlayerListField.Add(field.WithName(player.Nickname)
                            .WithValue($"*___________*")
                            .WithIsInline(true));
                        PlayerList.Playerlist.Remove(player);
                        PlayerList.PlayerlistDB[PlayerList.PlayerlistDB.IndexOf(player)].GamesPlayed += 1;

                        if (!PlayerList.Playerlist.Contains(player))
                            PlayerList.Playerlist.Add(player);

                        mentions += $"{user.Mention} "; // @mentions the players
                        await user.SendMessageAsync($"You are in {leader.Nickname}'s lobby. The password is: ` {password} ` .");
                    }
                    foreach (EmbedFieldBuilder field in PlayerListField)
                    {
                        embed.AddField(field);
                        Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : LastCall: Adding fields to embed.");
                    }

                    var Messagae = await Context.Channel.SendMessageAsync(mentions, embed: embed.Build());
                    Caches.Messages.LobbyMessages.Add(Messagae.Id); // Use this for storing all called games.

                    if (Caches.Messages.LobbyMessages.Count() > 3) // Only have 3 embed messages showing at a time. Will softode this to the config file later. I hope.
                    {
                        await Context.Channel.DeleteMessageAsync(Caches.Messages.LobbyMessages[0]);
                        Caches.Messages.LobbyMessages.RemoveAt(0);
                    }
                }*/


        private async Task UpdateList()
        {
            var Message = Caches.Messages.PlayerListEmbed;
            var Channel = Caches.Messages.ReactionMessageChannel;

            var embed = new EmbedBuilder()
                                .WithTitle($"Current Player q-υωυ-e Listings ({PlayerList.Playerlist.Count()})")
                .WithDescription("-----------------------------------------------------------------------")
                .WithFooter($"{DateTime.Now}");
            List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();

            int counter = 1;

            foreach (Player player in PlayerList.Playerlist)
            {
                var field = new EmbedFieldBuilder();
                field.WithName($"{player.Nickname}")
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



