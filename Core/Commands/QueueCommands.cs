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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;


/*
 * 0.8 Changes made:
 * 
 * 0.8.4
 * Started logging with Serilog. Will do more with it later.
 * 
 * 0.8.3
 * Added "MapVote" command. Randomly pulls x from a pool of maps to put them up for a vote.
 * Added "Map" command. Will randomly choose a map from a pool.
 * 
 * 0.8.2
 * Fixed bug that would delete people from the queue.
 * Made small optimizations.
 * 
 * 0.8.1
 * -Renamed "Newest" to "New". Combined the functionality of "Next" and "Newest".
 * -Added a 'maximum number of players to be pulled' setting in the bot config. This is mostly to allow for numeric passwords.
 *      Defaults to 10.
 * -Began implementing logging services with Serilog. Why not, right?
 * 
 * 0.7 Changes made:
 * 
 * -Added a 'minimum number of games to play' setting in the bot config. Defaults to 2.
 * -Remodeled the "Random" command. It now just mixes up the player list to be use with "Next".
 * -Fixed "Newest" pulling inactive players.
 * -Added notice embed to "Newest" if pulled list is empty.
 * -Renamed command "qstat" to "stats"
 * -Began implementing SQLite database. Why? Why not? But seriously... Why again?
 * 
 * 0.6e Changes made:
 * 
 * -Added "Least" command, which shows players who have played less than 3 games in the queue.
 * -Fixed bug that randomly added people to the list and would pull them for lobbies.
 * -Added Role checks to reaction handler.
 * -Added "Config" command, which allows for changing the config file values from the bot. Restarting the bot is recommended for changes to fully take effect.
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
        [Alias("Create")]
        [Summary(": Create a new queue for users to join.\nYou must pass a user role @mention.\nex: `create @customs`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task NewQueue(IRole role, [Remainder] string fudulgy = "")
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
            var emote = new Emoji(Config.bot.Reaction);  //"👍"
            //var emote = Emote.Parse(Config.bot.reaction); // not working with custom emotes... for some reason.

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
                ConfigChecks.Checks.DeleteQueueMessages();
                await Context.Channel.SendMessageAsync(embed: embed.Build());
            }
        }

        [Command("config")]
        [Summary(": Change parameters of the bots configuration file.\nSyntax: `config -msg 4`\n\nSwitches:" +
            "\n-prefix\tChange the prefix string the bot responds to." +
            "\n-react\tChange the reaction used by the bot." +
            "\n-group\tChange the size of the group the bot will pull." +
            "\n-maxsize\tChange the maximum number of players the bot can pull." +
            "\n-role\tChange the role needed by the user for the bot to accept them." +
            "\n-msgsize\tChange the number of of messages to leave before starting to clean them up." +
            "\n-mingames\tChange the minimum number of games that players should be able to play." +
            "\n-votenum\tChange the number of maps to pull for voting."
            )] //make sure to list parameters in here!!
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
                embed.WithFooter($"Version: {Version.GetVersion}");

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
                    Config.bot.Prefix = value;
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
                    Config.bot.Reaction = value;
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
                    Config.bot.GroupSize = int.Parse(value);
                    await Context.Channel.SendMessageAsync(onSuccess);
                }
                catch
                {
                    await Context.Channel.SendMessageAsync(invalid);
                }
            }
            else if (attrib == "-msgsize")
            {
                try
                {
                    Config.bot.MessageSize = int.Parse(value);
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
                    Config.bot.Role = value;
                    await Context.Channel.SendMessageAsync(onSuccess);
                }
                catch
                {
                    await Context.Channel.SendMessageAsync(invalid);
                }
            }
            else if (attrib == "-mingames")
            {
                try
                {
                    Config.bot.MinGamesPlayed = Int16.Parse(value);
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
            else if (attrib == "-maxsize")
            {
                try
                {
                    Config.bot.MaxSize = int.Parse(value);
                    await Context.Channel.SendMessageAsync(onSuccess);
                }
                catch
                {
                    await Context.Channel.SendMessageAsync(invalid);
                }
            }
            else if (attrib == "-votenum")
            {
                try
                {
                    Config.bot.NumberOfVotes = int.Parse(value);
                    await Context.Channel.SendMessageAsync(onSuccess);
                }
                catch
                {
                    await Context.Channel.SendMessageAsync(invalid);
                }
            }
            else
                await Context.Channel.SendMessageAsync(invalid);


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
        [Summary(": Lists player with the least number of games played. Currently only shows a games played range of 0-2. If 3 or more games have been played, logic won't handle it and return empty embeds. Will fix this later.")]
        public async Task LatestArrivals()
        {
            if (!Caches.Lobby.IsOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                return;
            }
            await Context.Channel.TriggerTypingAsync();

            List<Player> playersOrdered = new List<Player>();
            var embed = new EmbedBuilder();

            int x = 0;
            while (x <= Config.bot.MinGamesPlayed)
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
                embed.WithTitle("Players with least games played:")
                .WithDescription($"as of: {DateTime.Now}.");


                EmbedFieldBuilder[] fields = new EmbedFieldBuilder[Config.bot.MinGamesPlayed];
                //   EmbedFieldBuilder field1 = new EmbedFieldBuilder();
                //   EmbedFieldBuilder field2 = new EmbedFieldBuilder();

                for (x = 0; x <= Config.bot.MinGamesPlayed; x++)
                {
                    fields[x] = new EmbedFieldBuilder().WithName($" {x} Played:")
                        .WithValue("--------------");
                }

                string[] gamesplayed = new string[Config.bot.MinGamesPlayed];

                foreach (Player player in playersOrdered)
                {
                    for (x = 0; x <= Config.bot.MinGamesPlayed; x++)
                        if (player.GamesPlayed == x)
                        {
                            gamesplayed[x] += $"\n{player.GuildUser.Username} ({player.GuildUser.Nickname})";
                            embed.AddField(fields[x]
                                .WithValue(gamesplayed[x])
                                .WithIsInline(true));
                        }

                }
            }
            else
            {
                embed.AddField(x =>
                {
                    x.Name = "List is empty.";
                    x.Value = "No one here.";
                });
            }
            if (Caches.Messages.LeastCommandMessage != null)
                await Caches.Messages.LeastCommandMessage.DeleteAsync();

            Caches.Messages.LeastCommandMessage = await Context.Channel.SendMessageAsync(embed: embed.Build());


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
                                 
            try
            {
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

                        PlayerListField.Add(field.WithName($"{player.GuildUser.Username} ({player.GuildUser.Nickname})")
                        .WithValue($"Time joined: {player.EntryTime.ToShortTimeString()}\nGames Played: {player.GamesPlayed}\nActive: {(player.IsActive ? "Yes" : "`No`")}")
                        .WithIsInline(true));
                        Console.WriteLine($"{DateTime.Now} at list in QueueCommands: foreach- listfield added.");
                    }
                    foreach (EmbedFieldBuilder field in PlayerListField)
                    {
                        embed.AddField(field);
                        Console.WriteLine($"{DateTime.Now} at list in QueueCommands: foreach2- field added to embed.");
                    }
                    Console.WriteLine($"{DateTime.Now} at list in QueueCommands: ReplyAsync(embed.Build())- Building and sending embed.");
                    if (!(Caches.Messages.ListCommandMessage is null))
                        await Caches.Messages.ListCommandMessage.DeleteAsync();
                    Caches.Messages.ListCommandMessage = await Context.Channel.SendMessageAsync(embed: embed.Build());

                }
                else if (PlayerList.PlayerlistDB.Count >= 25)
                {
                    for (int x = 0; x < 24; x++)
                    {
                        Log.Information($"list in QueueCommands: for- fieldbuilder.");
                        EmbedFieldBuilder field = new EmbedFieldBuilder();

                        PlayerListField.Add(field.WithName(PlayerList.PlayerlistDB[x].GuildUser.Username)
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
                    if (!(Caches.Messages.ListCommandMessage is null))
                        await Caches.Messages.ListCommandMessage.DeleteAsync();
                    Caches.Messages.ListCommandMessage = await Context.Channel.SendMessageAsync(embed: embed.Build());
                    PlayerListField.Clear();

                    for (int x = 25; x < PlayerList.PlayerlistDB.Count; x++)
                    {
                        Console.WriteLine($"{DateTime.Now} at list in QueueCommands: for- fieldbuilder.");
                        EmbedFieldBuilder field = new EmbedFieldBuilder();

                        PlayerListField.Add(field.WithName(PlayerList.PlayerlistDB[x].GuildUser.Username)
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
                    try
                    {
                        if (!(Caches.Messages.ListCommandMessage2 is null))
                            await Caches.Messages.ListCommandMessage2.DeleteAsync();
                    }
                    catch (Exception e)
                    {
                        Log.Information(e.Message);
                        Console.WriteLine(e.Message);
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
            catch (Exception e) // Something bad has happened.
            {
                Console.WriteLine(e.Message);
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
            int groupSize = Config.bot.GroupSize;
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
                    var checkRole = await Context.Channel.GetUserAsync(PlayerList.Playerlist[ListPos].GuildUser.Id) as SocketGuildUser;
                    if (checkRole.Roles.Any(r => r.Name == Config.bot.Role))
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
                var user = Context.Guild.GetUser(player.GuildUser.Id);
                Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - Building fields");
                EmbedFieldBuilder field = new EmbedFieldBuilder();
                field.WithName($"{player.GuildUser.Username} ({player.GuildUser.Nickname})")
                    .WithValue($"*--------------------------*")
                    .WithIsInline(true);
                Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - field built for {player.GuildUser.Username} ({player.GuildUser.Nickname})");

                PlayerListField.Add(field);
                PlayerList.Playerlist.Remove(player);
                PlayerList.PlayerlistDB[PlayerList.PlayerlistDB.IndexOf(player)].GamesPlayed += 1;
                PlayerList.Playerlist.Add(player);


                mentions += $"{user.Mention} "; // @mentions the players
                Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - DMing {player.GuildUser.Username} ({player.GuildUser.Nickname})");
                await user.SendMessageAsync($"You are in `{leader.Username}'s` lobby. The password is: ` {password} ` .");
                Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : next - Direct Message sent.");

            }
            PlayerList.RecentList = players;
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

            if (Caches.Messages.LobbyMessages.Count() > Config.bot.MessageSize) // Only have [x] embed messages showing at a time.
            {
                await Caches.Messages.LobbyMessages[0].DeleteAsync();
                Caches.Messages.LobbyMessages.RemoveAt(0);
            }
        }


        [Command("random")]  //new random
        [Summary(": Randomizes the player list to mix up the match ups.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task RandomAsync()
        {
            if (!Caches.Lobby.IsOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                return;
            }
            int groupSize = Config.bot.GroupSize;
            int count = 0;

            // Check for number of active players
            foreach (Player active in PlayerList.Playerlist)
            {
                if (active.IsActive && !active.IsBanned)
                    count++;
            }

            await Context.Channel.TriggerTypingAsync();
            var leader = Context.Guild.GetUser(Context.User.Id);

            List<EmbedFieldBuilder> PlayerListField = new List<EmbedFieldBuilder>();
            List<Player> players = new List<Player>();

            // Create and display embed for users selected for next game.
            var embed = new EmbedBuilder();
            var random = new Random();

            HashSet<int> numbers = new HashSet<int>();
            while (numbers.Count < PlayerList.Playerlist.Count())
            {
                int index = random.Next(0, PlayerList.Playerlist.Count());

                var checkRole = await Context.Channel.GetUserAsync(PlayerList.Playerlist[index].GuildUser.Id) as SocketGuildUser;
                if (!PlayerList.Playerlist[index].IsBanned && checkRole.Roles.Any(r => r.Name == Config.bot.Role))
                {
                    numbers.Add(index);
                }
            }

            foreach (int number in numbers)
            {
                if (PlayerList.Playerlist[number].IsActive)
                    players.Add(PlayerList.Playerlist[number]);
                else if (!PlayerList.Playerlist[number].IsActive && PlayerList.Playerlist[number].GamesPlayed <= Config.bot.MinGamesPlayed)
                    players.Insert(0, PlayerList.Playerlist[number]);
            }

            try
            {
                PlayerList.Playerlist = players;
                embed.AddField(f =>
                {
                    f.Name = "Random";
                    f.Value = "Player list randomized.";
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                embed.AddField(f =>
                {
                    f.Name = "UH OH!";
                    f.Value = "Something went wrong. Sorry!";
                });
            }

            var Messagae = await Context.Channel.SendMessageAsync(embed: embed.Build());
            await UpdateMethods.Update.PlayerList();
        }

        /*    [Command("newest")]
            [Summary("Gets and displays [x] number of players who have the lowest\n " +
                        "number of games played for the next lobbies.\nIf no number is provided, " +
                        "the default will be used.\nA third argument can be passed for the password." +
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
                int groupSize = Config.bot.GroupSize;
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

                if (PlayerList.RecentList.Any())
                {
                    PlayerList.RecentList.Clear();
                }

                Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : lastcall - Active player count: {count}");

                if (groupSize >= count)
                    groupSize = count;

                // End of Checks

                // Look into using a list of list<player>s

                /* Reduce total play count to two.
                 * Change logic to allow for configuration
                 *

                //List<List<Player>> PlayerLists = new List<List<Player>>();

                List<Player> players0 = new List<Player>();
                List<Player> players1 = new List<Player>();
                //List<Player> players2 = new List<Player>();
                List<Player> playersToAdd = new List<Player>();

                foreach (Player player in PlayerList.Playerlist)
                {

                    if (player.GamesPlayed == 0)
                        players0.Add(player);
                    else if (player.GamesPlayed == 1)
                        players1.Add(player);
                    /* else if (player.GamesPlayed == 2)
                         players2.Add(player); *
                }

                if (players0.Any())
                    foreach (Player player in players0)
                        if (player.IsActive)
                            playersToAdd.Add(player);
                if (players1.Any())
                    foreach (Player player in players1)
                        if (player.IsActive)
                            playersToAdd.Add(player);
                // if (players2.Any())
                //     foreach (Player player in players2)
                //         if (player.IsActive)
                //             playersToAdd.Add(player);

                if (playersToAdd.Any() && playersToAdd.Count > 1)
                    playersToAdd.RemoveRange(groupSize, (playersToAdd.Count - groupSize));

                await Context.Channel.TriggerTypingAsync();
                var leader = Context.Guild.GetUser(Context.User.Id);

                // Create and display embed for users selected for next game.


                List<EmbedFieldBuilder> PlayerListField = new List<EmbedFieldBuilder>();
                string mentions = "";
                if (playersToAdd.Count == 0)
                {
                    var embed = new EmbedBuilder().WithTitle($"List is empty")
                    .WithDescription($"All active players have played at least 2 games.")
                    .WithColor(Color.DarkRed);
                    var Messagae = await Context.Channel.SendMessageAsync(embed: embed.Build());
                }
                else
                {
                    // Create and display embed for users selected for next game.
                    var embed = new EmbedBuilder().WithTitle($"{leader.Username}'s Next Lobby Group:")
                        .WithDescription($"Here are the next {groupSize} players for {leader.Username}'s lobby.\nThe password is: ` {password} `\n*Only join this lobby if your name is in this list!*")
                        .WithColor(Color.DarkGreen);

                    foreach (Player player in playersToAdd)
                    {
                        var user = Context.Guild.GetUser(player.GuildUser.Id); // get the userID to mention

                        EmbedFieldBuilder field = new EmbedFieldBuilder();
                        field.WithName($"{user.Username} ({user.Nickname})")
                            .WithValue($"*--------------------------*")
                            .WithIsInline(true);

                        Console.WriteLine($"{DateTime.Now} => [DEBUGGING] :: Newest: {user.Username} has been pulled.");

                        PlayerList.Playerlist.Remove(player);
                        PlayerList.PlayerlistDB[PlayerList.PlayerlistDB.IndexOf(player)].GamesPlayed += 1;
                        PlayerList.RecentList.Add(player);
                        if (!PlayerList.Playerlist.Contains(player))
                            PlayerList.Playerlist.Add(player);

                        mentions += $"{user.Mention} "; // @mentions the players
                        await user.SendMessageAsync($"You are in {leader.Nickname}'s lobby. The password is: ` {password} ` .");
                        PlayerListField.Add(field);
                    }
                    foreach (EmbedFieldBuilder field in PlayerListField)
                    {
                        embed.AddField(field);
                        Console.WriteLine($"{DateTime.Now} => [DEBUGGING] :: Newest: Adding fields to embed.");
                    }

                    var Messagae = await Context.Channel.SendMessageAsync(mentions, embed: embed.Build());
                    Caches.Messages.LobbyMessages.Add(Messagae); // Use this for storing all called games.

                    if (Caches.Messages.LobbyMessages.Count() > Config.bot.MessageSize) // Only have x embed messages showing at a time.
                    {
                        await Context.Channel.DeleteMessageAsync(Caches.Messages.LobbyMessages[0]);
                        Caches.Messages.LobbyMessages.RemoveAt(0);
                    }
                }
            }

            */

        //------------------------------------------------------------------------------------------------------
        //********************** Newest and Next are being combined here ***************************************

        [Command("new")]
        [Summary("Gets and displays [x] number of players who have the lowest\n " +
                    "number of games played for the next lobbies.\nIf no number is provided, " +
                    "the default will be used.\nA third argument can be passed for the password when passing a number." +
                    "\nex: `new [password]` or `new [x] [password]`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task New(string arg = "", [Remainder] string password = "")
        {

            #region Start checks
            if (!Caches.Lobby.IsOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                return;
            }
            int count = 0;
            int groupSize = Config.bot.GroupSize;

            // Check for first argument: Is it a different group size number or just a password?

            try
            {
                int number = int.Parse(arg);

                if (number < Config.bot.MaxSize)
                {

                    if (number < Config.bot.MaxSize)
                        groupSize = number;
                }
                else
                {
                    password = arg;
                }
            }
            catch
            {
                Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : New - Only a password passed");
                password = arg;
            }

            // Check for number of active players
            foreach (Player active in PlayerList.Playerlist)
            {
                if (active.IsActive && !active.IsBanned)
                    count++;
            }

            if (PlayerList.RecentList.Any())
            {
                PlayerList.RecentList.Clear();
            }

            Console.WriteLine($"{DateTime.Now} => [DEBUGGING] : New - Active player count: {count}");

            if (groupSize >= count)
                groupSize = count;

            #endregion
            await Context.Channel.TriggerTypingAsync();

            List<Player> playersToAdd = new List<Player>();
            //   Timer timer = new Timer(60000);         

            foreach (Player player in PlayerList.Playerlist) // Get players with 0 games played
            {
                if (player.GamesPlayed == 0 && player.IsActive)
                    playersToAdd.Add(player);
            }

            if (playersToAdd.Count < groupSize)
            {
                foreach (Player player in PlayerList.Playerlist) // Get players with 1 game played.
                {
                    if (player.GamesPlayed == 1 && player.IsActive)
                        playersToAdd.Add(player);
                }
            }

            if (playersToAdd.Count < groupSize) // Fill the empty space.
            {
                Console.WriteLine("{DateTimeNow} => [DEBUGGING] : New - Fill section: {playersToAdd.Count} players in ToAdd list", DateTime.Now, playersToAdd.Count);
                int x = 0;
                while (playersToAdd.Count < groupSize)
                {
                    if (!playersToAdd.Contains(PlayerList.Playerlist[x]) && PlayerList.Playerlist[x].IsActive)
                    {
                        playersToAdd.Add(PlayerList.Playerlist[x]);
                        Log.Debug("{DateTimeNow} => [DEBUGGING] : New - Fill section: {PlayerList.Playerlist[x].GuildUser.Username} added to ToAdd list", 
                            DateTime.Now, PlayerList.Playerlist[x].GuildUser.Username);
                    }
                    x++;
                }
            }



            playersToAdd.RemoveRange(groupSize, (playersToAdd.Count - groupSize)); // Cut out everyone below the group count so we only have groupSize number of players.

            foreach (Player player in playersToAdd) // Move the players to the bottom of the list
            {
                try 
                {
                    PlayerList.Playerlist.Remove(player);
                    PlayerList.Playerlist.Add(player);
                }
                catch (Exception e)
                {
                    Log.Error("{DateTime} => [DEBUG] New - failed to remove/add player {player} to playerlist. Exception: {e}",
                        DateTime.Now, e.Message);
                }
            }


            await Context.Channel.TriggerTypingAsync();
            var leader = Context.Guild.GetUser(Context.User.Id);

            // Create and display embed for users selected for next game.


            List<EmbedFieldBuilder> PlayerListField = new List<EmbedFieldBuilder>();
            string mentions = "";



            // Create and display embed for users selected for next game.
            var embed = new EmbedBuilder().WithTitle($"{leader.Username}'s Next Lobby Group:")
                .WithDescription($"Here are the next {groupSize} players for {leader.Username}'s lobby.\nThe password is: ` {password} `\n*Only join this lobby if your name is in this list!*")
                .WithColor(Color.DarkGreen);

            //var role = Context.Guild.Roles.FirstOrDefault(r => r.Name == "Pulled");

            Log.Information("{timestamp} : {leaderName} has pulled these players:",DateTime.Now, leader.Username);

            foreach (Player player in playersToAdd)
            {
                Log.Information("{plyaer}", player.GuildUser.Username);

                // Give Pulled role to players pulled. --retired
                // if (!player.GuildUser.Roles.Any(r => r.Name == role.Name))
                // {
                //     await player.GuildUser.AddRoleAsync(role);
                // }

                EmbedFieldBuilder field = new EmbedFieldBuilder();
                field.WithName($"{player.GuildUser.Username} ({player.GuildUser.Nickname})")
                    .WithValue($"*--------------------------*")
                    .WithIsInline(true);

                PlayerList.PlayerlistDB[PlayerList.PlayerlistDB.IndexOf(player)].GamesPlayed += 1;

                mentions += $"{player.GuildUser.Mention} "; // @mentions the players
                await player.GuildUser.SendMessageAsync($"You are in `{leader.Username}'s` lobby. The password is: ` {password} ` .");
                PlayerListField.Add(field);
            }
            PlayerList.RecentList = playersToAdd;
            
            foreach (EmbedFieldBuilder field in PlayerListField)
            {
                embed.AddField(field);
            }
                        
            var Messagae = await Context.Channel.SendMessageAsync(mentions, embed: embed.Build());
            Caches.Messages.LobbyMessages.Add(Messagae); // Use this for storing all called games.

            if (Caches.Messages.LobbyMessages.Count() > Config.bot.MessageSize) // Only have x embed messages showing at a time.
            {
                await Context.Channel.DeleteMessageAsync(Caches.Messages.LobbyMessages[0]);
                Caches.Messages.LobbyMessages.RemoveAt(0);
            }
            RecentPulledList recent = new RecentPulledList(playersToAdd);
            // recent.StartTimer();


        }



        [Command("mapvote")]
        [Summary("Randomly chooses x number of maps from a pool and puts them to a vote.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task MapVote()
        {
            // Adding Map Vote here. Don't ask.
            

            var one = new Emoji("1️⃣");
            var two = new Emoji("2️⃣");
            var three = new Emoji("3️⃣");

            List<string> maps = new List<string>();

            // Create and display embed for maps selected for next game.
            var voteEmbed = new EmbedBuilder()
                .WithTitle("Next Game Map Vote")
                .WithDescription("Vote here!")
                .WithColor(Color.Blue);
            
            var random = new Random();

            HashSet<int> numbers = new HashSet<int>();
            while (numbers.Count <= Config.bot.NumberOfVotes)
            {
                int index = random.Next(0, MapList.List.Length);
                numbers.Add(index);
            }

            foreach (int number in numbers)
            {
                maps.Add(MapList.List[number]);
            }

            List<EmbedFieldBuilder> voteFields = new List<EmbedFieldBuilder>();

            for (int i = 0; i < Config.bot.NumberOfVotes; i++)
            {
                EmbedFieldBuilder votes = new EmbedFieldBuilder();
                votes.WithName(maps[i])
                    .WithValue($"`Map {i+1}`")
                    .WithIsInline(true);

                Console.WriteLine($"MAP VOTE => Populating fields. Map {i} is {maps[i]}.");
                voteFields.Add(votes);
            }

            foreach (EmbedFieldBuilder field in voteFields)
                voteEmbed.AddField(field);

            Console.WriteLine($"MAP VOTE => Building and sending Embed.");
            Caches.Messages.MapVoteMessage = await Context.Channel.SendMessageAsync(embed: voteEmbed.Build());
            Console.WriteLine($"MAP VOTE => Embed successfully sent.");
            await Caches.Messages.MapVoteMessage.AddReactionAsync(one);
            await Caches.Messages.MapVoteMessage.AddReactionAsync(two);
            await Caches.Messages.MapVoteMessage.AddReactionAsync(three);
        }

        [Command("map")]
        [Summary("Randomly chooses x number of maps from a pool and puts them to a vote.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task Map()
        {
            // Changed to just randomly pull a single map.


            var random = new Random();
            int index = random.Next(0, MapList.List.Length);

            // Create and display embed for maps selected for next game.
            var voteEmbed = new EmbedBuilder()
                .WithTitle($"{MapList.List[index]}")
                .WithDescription("The spirits have spoken.").WithColor(Color.Blue);

            List<EmbedFieldBuilder> voteFields = new List<EmbedFieldBuilder>();


            Console.WriteLine($"MAP  => Building and sending Embed.");
            Caches.Messages.MapVoteMessage = await Context.Channel.SendMessageAsync(embed: voteEmbed.Build());
            Console.WriteLine($"MAP  => Embed successfully sent.");

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
            try
            {
                if (!Task.Run(async () =>
                    {
                        if (!(Caches.Messages.ReactionMessage is null))
                        {
                            await Caches.Messages.ReactionMessage.DeleteAsync();      //Context.Channel.DeleteMessageAsync(Caches.Messages.ReactionMessage);  // Delete the message with reacts

                        }
                   
                        if (!(Caches.Messages.PlayerListEmbed is null))
                        {
                            await Caches.Messages.PlayerListEmbed.DeleteAsync();      //Context.Channel.DeleteMessageAsync(Caches.Messages.PlayerListEmbed);
                        }

                        if (!(Caches.Messages.ListCommandMessage is null))
                        {
                            await Caches.Messages.ListCommandMessage.DeleteAsync();
                        }

                        if (!(Caches.Messages.ListCommandMessage2 is null))
                        {
                            await Caches.Messages.ListCommandMessage2.DeleteAsync();
                        }

                        foreach (IMessage message in Caches.Messages.LobbyMessages)
                            await message.DeleteAsync();

                    }).IsCompletedSuccessfully
                  )
                {
                    if (!(Caches.Messages.ReactionMessage is null))
                    {
                        // Put logging in all of these
                    }
                    if (!(Caches.Messages.PlayerListEmbed is null))
                    {

                    }

                    if (!(Caches.Messages.ListCommandMessage is null))
                    {

                    }

                    if (!(Caches.Messages.ListCommandMessage2 is null))
                    {

                    }




                }
                else
                {
                    // Put logging here
                }
            }
            catch (Exception e)
            {
                Log.Debug("{time} :: Failed to delete all messages. Exception: {e}", DateTime.Now, e.Message);
            }
        }
    }

}





