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
using System.IO;
using Newtonsoft.Json;

/*
 * 0.7 Changes made:
 * 
 * -Fixed "Newest" pulling inactive players.
 * -Began implementing SQLite database.
 * 
 * 0.6e Changes made:
 * 
 * -Added "Least" command, which shows players who have played less than 3 games in the queue.
 * -Fixed bug that randomly added people to the list and would pull them for lobbies.
 * -Added Role checks to reaction handler.
 * -Added "Config" command, which allows for changing the config file values from the bot. Restarting the bot is required for changes to take effect.
 * -Added "Recall" command, which pings users from the most recently pulled list again as a last call.
 * -Fixed "Next" not working properly. It works properly now. Proper.
 * -
 * -
 * -
 * 
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
            Caches.Lobby.IsOpen = true;

        }

        [Command("close")]
        [Summary(": Close off the queue and clear the playerlist")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task CloseQueue()
        {
            if (Caches.Lobby.IsOpen)
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
                Caches.Lobby.IsOpen = false;
                await Context.Channel.SendMessageAsync(embed: embed.Build());

                ConfigChecks.Checks.DeleteQueueMessages();
            }
        }

        [Command("config")]
        [Summary(": Change parameters of the bots configuration file.\nSyntax: `config -msg 4`\n\nSwitches:" +
            "\n-prefix\tChange the prefix string the bot responds to." +
            "\n-react\tChange the reaction used by the bot." +
            "\n-group\tChange the size of the group the bot will pull." +
            "\n-role\tChange the role needed by the user for the bot to accept them." +
            "\n-msg\tChange the number of of messages to leave before starting to clean them up.")] //make sure to list parameters in here!!
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task BotConfig(string attrib = "", [Remainder] string value = "")
        {
            string configFolder = "Resources";
            string configFile = "config.json";
            string json = File.ReadAllText(configFolder + "/" + configFile);  //Json file found and read


            string invalid = "The parameter you have entered is invalid.";
            string onSuccess = "Configuration settings have been changed.";
            // t,p,r,g,m (token, prefix, reaction, group size, message limit)

            //Check if user just wants to see current configuration.
            if (attrib == "" && value == "")
            {
                var embed = new EmbedBuilder()
                    .WithTitle("Current Configuration:")
                    .WithDescription("-------------------------------")
                    .WithColor(Color.Blue);

                var field = new EmbedFieldBuilder()
                    .WithName(Config.bot.ToString())
                    .WithValue(DateTime.Now.ToString());
                embed.AddField(field);

                await Context.Channel.SendMessageAsync(embed: embed.Build());
                return;
            }



            //Check what user wants to change and change it.
            if (attrib == "-token")
            {
                try
                {
                    //bot.token = value;
                    await Context.Channel.SendMessageAsync("This command is unavailable.");             //(onSuccess);
                }
                catch
                {
                    await Context.Channel.SendMessageAsync(invalid);
                }

            }
            else if (attrib == "-prefix")
            {
                try
                {
                    Config.bot.prefix = value;
                    await Context.Channel.SendMessageAsync(onSuccess);
                }
                catch
                {
                    await Context.Channel.SendMessageAsync(invalid);
                }
            }
            else if (attrib == "-react")
            {
                try
                {
                    Config.bot.reaction = value;
                    await Context.Channel.SendMessageAsync(onSuccess);
                }
                catch
                {
                    await Context.Channel.SendMessageAsync(invalid);
                }
            }
            else if (attrib == "-group")
            {
                try
                {
                    Config.bot.groupsize = int.Parse(value);
                    await Context.Channel.SendMessageAsync(onSuccess);
                }
                catch
                {
                    await Context.Channel.SendMessageAsync(invalid);
                }
            }
            else if (attrib == "-msg")
            {
                try
                {
                    Config.bot.messagesize = int.Parse(value);
                    await Context.Channel.SendMessageAsync(onSuccess);
                }
                catch
                {
                    await Context.Channel.SendMessageAsync(invalid);
                }
            }
            else if (attrib == "-role")
            {
                try
                {
                    Config.bot.role = value;
                    await Context.Channel.SendMessageAsync(onSuccess);
                }
                catch
                {
                    await Context.Channel.SendMessageAsync(invalid);
                }
            }
            else if (attrib == "-restart")
            {
                try
                {
                    if (value.ToLower() == "true")
                    {
                        if (Config.bot.ReloadConfigFile())
                            await Context.Channel.SendMessageAsync("Config file successfully reloaded.");
                        else
                            await Context.Channel.SendMessageAsync("Authorization was not permitted.");
                    }
                }
                catch
                {
                    await Context.Channel.SendMessageAsync(invalid);
                }
            }



            // ----THE END----
            json = JsonConvert.SerializeObject(Config.bot, Formatting.Indented); //Json file creation
            File.WriteAllText(configFolder + "/" + configFile, json);
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

        [Command("least")]
        [Summary(": Lists player with the least number of games played. Currently only shows a games played range of 0-2. If 3 or more games have been played, logic won't handle it and return empty embeds.")]
        public async Task LatestArrivals()
        {
            if (!Caches.Lobby.IsOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                Console.WriteLine($"{DateTime.Now} at list in QueueCommands: No open queue.");
                return;
            }
            await Context.Channel.TriggerTypingAsync();

            List<Player> playersOrdered = new List<Player>();

            if (PlayerList.Playerlist.Count > 0)
            {
                int x = 0;
                while (x < 4)
                {
                    foreach (Player player in PlayerList.Playerlist)
                    {
                        if (player.GamesPlayed == x)
                            playersOrdered.Add(player);
                    }
                    x++;
                }

                if (playersOrdered.Count > 0)
                {
                    var embed = new EmbedBuilder()
                        .WithTitle("Players with least games played:")
                        .WithDescription($"as of: {DateTime.Now}.");


                    EmbedFieldBuilder field0 = new EmbedFieldBuilder();
                    EmbedFieldBuilder field1 = new EmbedFieldBuilder();
                    EmbedFieldBuilder field2 = new EmbedFieldBuilder();

                    field0.WithName(" 0  Played:");
                    field1.WithName(" 1  Played:");
                    field2.WithName(" 2  Played:");
                    string game0 = "--------------",
                        game1 = "---------------",
                        game2 = "---------------";

                    foreach (Player player in playersOrdered)
                    {
                        if (player.GamesPlayed < 1)
                        {
                            game0 += $"\n{player.DiscordName} ({player.Nickname})";
                        }
                        else if (player.GamesPlayed < 2)
                        {
                            game1 += $"\n{player.DiscordName} ({player.Nickname})";
                        }
                        else if (player.GamesPlayed < 3)
                        {
                            game2 += $"\n{player.DiscordName} ({player.Nickname})";
                        }
                    }


                    embed.AddField(field0.WithValue(game0).WithIsInline(true));
                    embed.AddField(field1.WithValue(game1).WithIsInline(true));
                    embed.AddField(field2.WithValue(game2).WithIsInline(true));

                    if (Caches.Messages.LeastCommandMessage != null)
                        await Caches.Messages.LeastCommandMessage.DeleteAsync();

                    Caches.Messages.LeastCommandMessage = await Context.Channel.SendMessageAsync(embed: embed.Build());
                }
                else
                {
                    //Send message saying the list is empty
                    await Context.Channel.SendMessageAsync("The list is currently empty.");
                }
            }
            else
            {
                //Send message saying the list is empty
                await Context.Channel.SendMessageAsync("The list is currently empty.");
            }


        }

        [Command("list")]
        [Summary(": Provides the list of everyone in the queue database.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task ShowQueueList()
        {
            if (!Caches.Lobby.IsOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                Console.WriteLine($"{DateTime.Now} at list in QueueCommands: No open queue.");
                return;
            }
            await Context.Channel.TriggerTypingAsync();

            List<EmbedFieldBuilder> PlayerListField = new List<EmbedFieldBuilder>();
            var embed = new EmbedBuilder()
                .WithTitle($"There are {PlayerList.Playerlist.Count} players in the list")
                .WithCurrentTimestamp();

            var embed2 = new EmbedBuilder()
                .WithTitle("Player list continued...");

            if (PlayerList.PlayerlistDB.Count > 0 && PlayerList.PlayerlistDB.Count < 25)
            {

                Console.WriteLine($"{DateTime.Now} at list in QueueCommands: Playerlist DB count != 0.");
                foreach (Player player in PlayerList.PlayerlistDB)
                {
                    Console.WriteLine($"{DateTime.Now} at list in QueueCommands: foreach- fieldbuilder.");
                    EmbedFieldBuilder field = new EmbedFieldBuilder();

                    PlayerListField.Add(field.WithName(player.DiscordName)
                    .WithValue($"Active: {(player.IsActive ? "Yes" : "`No`")}\nGames Played: {player.GamesPlayed}\nBanned: {(player.IsBanned ? "`Yes`" : "No")}\nTime joined: {player.EntryTime.ToShortTimeString()}")
                    .WithIsInline(true));
                    Console.WriteLine($"{DateTime.Now} at list in QueueCommands: foreach- listfield added.");
                }
                foreach (EmbedFieldBuilder field in PlayerListField)
                {
                    embed.AddField(field);
                    Console.WriteLine($"{DateTime.Now} at list in QueueCommands: foreach2- field added to embed.");
                }
                Console.WriteLine($"{DateTime.Now} at list in QueueCommands: ReplyAsync(embed.Build())- Building and sending embed.");
                Caches.Messages.ListCommandMessage = await Context.Channel.SendMessageAsync(embed: embed.Build());

            }
            else if (PlayerList.PlayerlistDB.Count >= 25)
            {
                for (int x = 0; x < 25; x++)
                {
                    Console.WriteLine($"{DateTime.Now} at list in QueueCommands: for- fieldbuilder.");
                    EmbedFieldBuilder field = new EmbedFieldBuilder();

                    PlayerListField.Add(field.WithName(PlayerList.PlayerlistDB[x].DiscordName)
                        .WithValue($"Active: {(PlayerList.PlayerlistDB[x].IsActive ? "Yes" : "`No`")}\nGames Played: {PlayerList.PlayerlistDB[x].GamesPlayed}" +
                        $"\nBanned: {(PlayerList.PlayerlistDB[x].IsBanned ? "`Yes`" : "No")}")
                        .WithIsInline(true));
                    Console.WriteLine($"{DateTime.Now} at list in QueueCommands: for- listfield added.");
                }
                foreach (EmbedFieldBuilder field in PlayerListField)
                {
                    embed.AddField(field);
                    Console.WriteLine($"{DateTime.Now} at list in QueueCommands: foreach- field added to embed.");
                }
                Console.WriteLine($"{DateTime.Now} at list in QueueCommands: ReplyAsync(embed.Build())- Building and sending embed.");
                Caches.Messages.ListCommandMessage = await Context.Channel.SendMessageAsync(embed: embed.Build());
                PlayerListField.Clear();

                for (int x = 25; x < PlayerList.PlayerlistDB.Count; x++)
                {
                    Console.WriteLine($"{DateTime.Now} at list in QueueCommands: for- fieldbuilder.");
                    EmbedFieldBuilder field = new EmbedFieldBuilder();

                    PlayerListField.Add(field.WithName(PlayerList.PlayerlistDB[x].DiscordName)
                        .WithValue($"Active: {(PlayerList.PlayerlistDB[x].IsActive ? "Yes" : "`No`")}\nGames Played: {PlayerList.PlayerlistDB[x].GamesPlayed}" +
                        $"\nBanned: {(PlayerList.PlayerlistDB[x].IsBanned ? "`Yes`" : "No")}")
                        .WithIsInline(true));
                    Console.WriteLine($"{DateTime.Now} at list in QueueCommands: for- listfield added.");
                }
                foreach (EmbedFieldBuilder field in PlayerListField)
                {
                    embed2.AddField(field);
                    Console.WriteLine($"{DateTime.Now} at list in QueueCommands: foreach2- field added to embed.");
                }
                Console.WriteLine($"{DateTime.Now} at list in QueueCommands: ReplyAsync(embed.Build())- Building and sending embed.");
                Caches.Messages.ListCommandMessage2 = await Context.Channel.SendMessageAsync(embed: embed2.Build());


            }
            else
            {
                embed.WithTitle("The database is Empty.")
                    .WithDescription("Get people in the list!");

                Caches.Messages.ListCommandMessage = await Context.Channel.SendMessageAsync(embed: embed.Build());
            }

        }

        [Command("next")] // Consider integrating Next and Newest
        [Alias("nextgame")]
        [Summary(": Gets and displays [x] number of players for the next lobby" +
            "\nIf no number is provided, the default will be used.\nA third argument " +
            "can be passed for the password.\nex: `next [password]` or `next [x] [password]`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task NextGroup(string arg = "", [Remainder] string password = "")
        {
            // Start checks
            if (!Caches.Lobby.IsOpen)
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

            if (PlayerList.RecentList.Any())
            {
                PlayerList.RecentList.Clear();
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
                Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - While loop");

            while (index < groupSize)
            {
                if (PlayerList.Playerlist[ListPos].IsActive && !PlayerList.Playerlist[ListPos].IsBanned)
                {
                    Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - SocketUser checkRole");
                    var checkRole = await Context.Channel.GetUserAsync(PlayerList.Playerlist[ListPos].DiscordID) as SocketGuildUser;
                    if (checkRole.Roles.Any(r => r.Name == Config.bot.role))
                    {
                        Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - {PlayerList.Playerlist[ListPos]} has role. Adding to list");
                        players.Add(PlayerList.Playerlist[ListPos]);
                        index++;
                        ListPos++;
                    }
                }

                else  //Skip inactive players
                {
                    ListPos++;
                }

            }
          
            Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - Foreach player in players.");
            foreach (Player player in players)
            {
                var user = Context.Guild.GetUser(player.DiscordID);
                Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - Building fields");
                EmbedFieldBuilder field = new EmbedFieldBuilder();
                field.WithName($"{player.DiscordName} ({player.Nickname})")
                    .WithValue($"*--------------------------*")
                    .WithIsInline(true);
                Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - field built for {player.DiscordName} ({player.Nickname})");

                PlayerListField.Add(field);
                PlayerList.Playerlist.Remove(player);
                PlayerList.PlayerlistDB[PlayerList.PlayerlistDB.IndexOf(player)].GamesPlayed += 1;
                PlayerList.Playerlist.Add(player);
                PlayerList.RecentList.Add(player);

                mentions += $"{user.Mention} "; // @mentions the players
                Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - DMing {player.DiscordName} ({player.Nickname})");
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
            await UpdateMethods.Update.PlayerList();
            Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - Player list update method.");
            Caches.Messages.LobbyMessages.Add(Messagae);

            if (Caches.Messages.LobbyMessages.Count() > Config.bot.messagesize) // Only have [x] embed messages showing at a time.
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
            "\nIf no number is provided, the default will be used.\nA third argument can be " +
            "passed for the password.\nex: `random [password]` or `random [x] [password]`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task RandomAsync(string arg = "", [Remainder] string password = "")
        {
            if (!Caches.Lobby.IsOpen)
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

                var checkRole = await Context.Channel.GetUserAsync(PlayerList.Playerlist[index].DiscordID) as SocketGuildUser;
                if (PlayerList.Playerlist[index].IsActive && !PlayerList.Playerlist[index].IsBanned && checkRole.Roles.Any(r => r.Name == Config.bot.role))
                    numbers.Add(index);
            }

            foreach (int number in numbers)
            {
                var field = new EmbedFieldBuilder();
                players.Add(PlayerList.Playerlist[number]);
                PlayerListField.Add(field
                    .WithName($"{PlayerList.Playerlist[number].DiscordName} ({PlayerList.Playerlist[number].Nickname})")
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
            await UpdateMethods.Update.PlayerList();
            Caches.Messages.LobbyMessages.Add(Messagae);


            if (Caches.Messages.LobbyMessages.Count() > Config.bot.messagesize) // Only have x embed messages showing at a time.
            {
                await Context.Channel.DeleteMessageAsync(Caches.Messages.LobbyMessages[0]);
                Caches.Messages.LobbyMessages.RemoveAt(0);
            }
        }

        [Command("newest")]
        [Summary("Gets and displays [x] number of players who have the lowest\n " +
                    "number of games played for the next, usually last, lobbies.\nIf no number is provided, " +
                    "the default will be used..\nA third argument can be passed for the password." +
                    "\nex: `newest [password]` or `newest [x] [password]`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task LastCall(string arg = "", [Remainder] string password = "")
        {
            // Start checks
            if (!Caches.Lobby.IsOpen)
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
                if (active.IsActive && active.GamesPlayed < 3)
                    count++;
            }

            Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : lastcall - Active player count: {count}");

            if (groupSize >= count)
                groupSize = count;

            // End of Checks

            // Look into using a list of list<player>s
            List<List<Player>> PlayerLists = new List<List<Player>>();

            List<Player> players0 = new List<Player>();
            List<Player> players1 = new List<Player>();
            List<Player> players2 = new List<Player>();
            List<Player> playersToAdd = new List<Player>();

            foreach (Player player in PlayerList.Playerlist)
            {
                
                if (player.GamesPlayed == 0)
                    players0.Add(player);
                else if (player.GamesPlayed == 1)
                    players1.Add(player);
                else if (player.GamesPlayed == 2)
                    players2.Add(player);
            }

            if (players0.Any())
                foreach (Player player in players0)
                    if (player.IsActive)
                        playersToAdd.Add(player);
            if (players1.Any())
                foreach (Player player in players1)
                    if (player.IsActive)
                        playersToAdd.Add(player);
            if (players2.Any())
                foreach (Player player in players2)
                    if (player.IsActive)
                        playersToAdd.Add(player);

            if (playersToAdd.Any() && playersToAdd.Count > 1)
                playersToAdd.RemoveRange(groupSize, (playersToAdd.Count - groupSize));

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
                field.WithName($"{user.Username} ({user.Nickname})")
                    .WithValue($"*--------------------------*")
                    .WithIsInline(true);
                PlayerList.Playerlist.Remove(player);
                PlayerList.PlayerlistDB[PlayerList.PlayerlistDB.IndexOf(player)].GamesPlayed += 1;

                if (!PlayerList.Playerlist.Contains(player))
                    PlayerList.Playerlist.Add(player);

                mentions += $"{user.Mention} "; // @mentions the players
                await user.SendMessageAsync($"You are in {leader.Nickname}'s lobby. The password is: ` {password} ` .");
                PlayerListField.Add(field);
            }
            foreach (EmbedFieldBuilder field in PlayerListField)
            {
                embed.AddField(field);
                Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : LastCall: Adding fields to embed.");
            }

            var Messagae = await Context.Channel.SendMessageAsync(mentions, embed: embed.Build());
            Caches.Messages.LobbyMessages.Add(Messagae); // Use this for storing all called games.

            if (Caches.Messages.LobbyMessages.Count() > Config.bot.messagesize) // Only have x embed messages showing at a time.
            {
                await Context.Channel.DeleteMessageAsync(Caches.Messages.LobbyMessages[0]);
                Caches.Messages.LobbyMessages.RemoveAt(0);
            }
        }





        public class ConfigChecks
        {
            public static ConfigChecks Checks = new ConfigChecks();

            public bool CheckThis()
            {
                bool that = false;
                // if (here) {}
                return that;
            }

            public void DeleteQueueMessages()
            {
                Task.Run(async () =>
                {
                    if (!(Caches.Messages.ReactionMessage is null))
                        await Caches.Messages.ReactionMessage.DeleteAsync();      //Context.Channel.DeleteMessageAsync(Caches.Messages.ReactionMessage);  // Delete the message with reacts
                    if (!(Caches.Messages.PlayerListEmbed is null))
                        await Caches.Messages.PlayerListEmbed.DeleteAsync();      //Context.Channel.DeleteMessageAsync(Caches.Messages.PlayerListEmbed);
                    if (!(Caches.Messages.ListCommandMessage is null))
                        await Caches.Messages.ListCommandMessage.DeleteAsync();
                    if (!(Caches.Messages.ListCommandMessage2 is null))
                        await Caches.Messages.ListCommandMessage2.DeleteAsync();
                    foreach (IMessage message in Caches.Messages.LobbyMessages)
                        await message.DeleteAsync();
                });
            }
        }

    }
}



