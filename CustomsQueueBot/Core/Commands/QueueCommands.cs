using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System.Linq;
using Discord.WebSocket;
using System.Collections;
using Microsoft.Extensions.Logging;

namespace CustomsQueueBot.Core.Commands
{
    public class QueueCommands : ModuleBase<SocketCommandContext>
    {


        [Command("create")]
        [Alias("cq", "+q", "createqueue")]
        [Summary("Create a new queue for users to join.\nYou must pass a user role @mention.\n`ex: +create @customs`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task NewQueue(IRole role)
        {

            var leader = Context.Guild.GetUser(Context.User.Id);

            await Context.Channel.TriggerTypingAsync();

            //          if (!(PlayerList.Playerlist.Count == 0))
            //              PlayerList.Playerlist.Clear();

            var embed = new EmbedBuilder();
            var field = new EmbedFieldBuilder();

            // Create message with reaction for queue
            embed.WithColor(Color.DarkGreen)
                .WithTitle($"Hey { role.Name }! The customs games queue is now open!")
                .WithTimestamp(DateTime.Now);

            field.WithName("Click or Tap on reaction to join queue.")
                .WithValue("Removing your reaction will set you as inactive.\nThis will skip over your turn when " +
                "lobbies are called,\nbut you will retain your position in line.\nReact to this message " +
                "again to become active and stop being skipped." +
                "\nReact to this message only if you are not already in either\na custom lobby or a custom match.");

            embed.AddField(field);
            Console.WriteLine("Firing Send Message Async.");
            var Message = await Context.Channel.SendMessageAsync(embed: embed.Build());    // Sends the embed for people to react to.
            var emote = new Emoji("👍");  // Change to Config.bot.reaction

            await Message.AddReactionAsync(emote);
            Caches.ReactionMessages.ReactionMessage = Message.Id;
            Caches.IsOpen.isOpen = true;
        }

        [Command("close")]
        [Alias("removequeue", "-q", "closequeue")]
        [Summary("Close off the queue and clear the playerlist")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task RemoveQueue()
        {
            // var message = Context.Channel.GetMessageAsync(Caches.ReactionMessages.ReactionMessage);
            await Context.Channel.TriggerTypingAsync();
            PlayerList.Playerlist.Clear();
            PlayerList.PlayerlistDB.Clear();
            //   await Context.Channel.DeleteMessageAsync();     // Delete the message with reacts

            var embed = new EmbedBuilder().WithTitle("The customs queue has been closed!")
                .WithColor(Color.DarkRed)
                .WithDescription("The queue has been closed and the list has been emptied.\nThank you everyone" +
                "who joined in today's session!!");

            await Context.Channel.DeleteMessageAsync(Caches.ReactionMessages.ReactionMessage);
            await ReplyAsync(embed: embed.Build());
            Caches.IsOpen.isOpen = false;
        }


        [Command("mlist")]
        [Summary("Not yet implemented")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task ShowModPlayerList()
        {
            if (!Caches.IsOpen.isOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                return;
            }
            await Context.Channel.TriggerTypingAsync();

            Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : Creating EmbedBuilder.");
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
                    Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : Reading PlayerList: {player.Nickname} was loaded.");

                    EmbedFieldBuilder field = new EmbedFieldBuilder();

                    PlayerListField.Add(field.WithName(names)
                    .WithValue($"Games Played: {player.GamesPlayed} \nActive: {(player.IsActive ? "Yes" : "No")}")
                    .WithIsInline(true));

                }
                foreach (EmbedFieldBuilder field in PlayerListField)
                    embed.AddField(field);
            }

            // Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : List of names: {names}");
            Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : Firing ReplyAsync Method.");
            await ReplyAsync(embed: embed.Build());
            Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : ReplyAsync Method has fired.");
        }

        [Command("next")]
        [Alias("nextgame")]
        [Summary("Gets and displays [x] number of players for the next lobby\nIf no number is provided, default will be 8.\nA third argument can be passed for the password.\nex: next [x] [password]")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task NextGroupSize(int groupSize = 8, [Remainder] string password = "")
        {
            if (!Caches.IsOpen.isOpen) return;
            int count = 0;


            // Check for number of active players
            foreach (Player active in PlayerList.Playerlist)
            {
                if (active.IsActive)
                    count++;
            }

            if (groupSize > count)
            {
                groupSize = count;
                foreach (Player player in PlayerList.PlayerlistDB)
                {
                    if (player.IsActive)
                        PlayerList.Playerlist.Add(player);
                }

            }
            // Pull the next group from the queue list and displays their names.
            // Maybe have them get PMed the information(?)

            await Context.Channel.TriggerTypingAsync();
            var leader = Context.Guild.GetUser(Context.User.Id);

            Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : Building embed.");
            // Create and display embed for users selected for next game.
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
                if (PlayerList.Playerlist[ListPos].IsActive)
                {
                    Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : Reading PlayerList: {PlayerList.Playerlist[ListPos].Nickname} was loaded.");

                    players.Add(PlayerList.Playerlist[ListPos]);
                    index++;
                    ListPos++;
                }
                else  //Skip inactive players
                {
                    ListPos++;
                }

            }

            Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : ForEach loop, building fields.");
            foreach (Player player in players)
            {
                Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : ForEach loop: creating var user.");
                var user = Context.Guild.GetUser(player.DiscordID);

                EmbedFieldBuilder field = new EmbedFieldBuilder();
                PlayerListField.Add(field.WithName(player.Nickname)
                    .WithValue($"*___________*")
                    .WithIsInline(true));
                PlayerList.Playerlist.Remove(player);
                PlayerList.PlayerlistDB[PlayerList.PlayerlistDB.IndexOf(player)].GamesPlayed += 1;

                mentions += $"{user.Mention}, ";

                Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : ForEach loop, Field Built");
            }
            Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : Building embed.");
            foreach (EmbedFieldBuilder field in PlayerListField)
            {
                embed.AddField(field);
                Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : Adding fields to embed.");
            }

            Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : Sending Message with Embed Async.");
            await Context.Channel.SendMessageAsync(mentions, embed: embed.Build());
        }

        [Command("random")]
        [Summary("Gets and displays [x] number of random players for the next lobby\nIf no number is provided, default will be 8.\nA third argument can be passed for the password.\nex: next [x] [password]")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task RandomAsync(int groupSize = 8, [Remainder] string password = "")
        {
            if (!Caches.IsOpen.isOpen) return;

            await Context.Channel.TriggerTypingAsync();
            var leader = Context.Guild.GetUser(Context.User.Id);
            List<EmbedFieldBuilder> PlayerListField = new List<EmbedFieldBuilder>();
            List<Player> players = new List<Player>();

            // Create and display embed for users selected for next game.
            var embed = new EmbedBuilder().WithTitle($"{leader.Username}'s Next Lobby Group:")
                .WithDescription($"Here are the next RANDOM {groupSize} players for {leader.Username}'s lobby.\nThe password is: ` {password} `\n*Only join this lobby if your name is in this list!*")
                .WithColor(Color.DarkGreen);
            var random = new Random();

            HashSet<int> numbers = new HashSet<int>();
            while (numbers.Count < groupSize)
            {
                numbers.Add(random.Next(0, PlayerList.Playerlist.Count()));
            }

            Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : Random: List size = {PlayerList.Playerlist.Count()}.");

            foreach (int number in numbers)
            {

                var field = new EmbedFieldBuilder();
                players.Add(PlayerList.Playerlist[number]);
                PlayerListField.Add(field.WithName(PlayerList.Playerlist[number].Nickname)
                    .WithValue("__________")
                    .WithIsInline(true));

            }

            foreach (Player player in players)
            {
                PlayerList.Playerlist.Remove(player);
                PlayerList.PlayerlistDB[PlayerList.PlayerlistDB.IndexOf(player)].GamesPlayed += 1;
            }

            foreach (EmbedFieldBuilder field in PlayerListField)
                embed.AddField(field);

            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }

        [Command("lastcall")]
        [Summary("Not yet implemented.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task LastCall()
        {

        }

        [Command("remove")]
        [Alias("removeplayer", "rp", "-p")]
        [Summary("Remove a player from the queue\nCan pass a reason as a second argument.\nex. remove @[player] reason.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]  // This replaces a block of code commented in this command
        public async Task RemovePlayer(string userID, [Remainder] string reason = "")
        {
            if (!Caches.IsOpen.isOpen) return;

            await Context.Channel.TriggerTypingAsync();
            ulong _id;
            SocketGuildUser _user;
            var embed = new EmbedBuilder().WithTitle("Remove Player");

            try  //Check if userID is an @mention or a discordID and assigns them appropriately.
            {
                _id = ulong.Parse(userID);
                _user = Context.Guild.GetUser(_id);
            }
            catch (Exception)
            {
                _user = Context.Guild.GetUser(Context.Message.MentionedUsers.First().Id);
            }

            // Search user list for username
            foreach (Player player in PlayerList.Playerlist)
            {
                if (player.DiscordID == _user.Id)   // Remove user from list
                {
                    PlayerList.Playerlist.Remove(player);
                    Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : Removing Player: {player.Nickname} is being removed.");

                    Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : Removing Player: Creating Embed.");
                    embed.WithDescription($"{player.Nickname} has been removed from the queue.\nReason: {reason}")
                        .WithColor(Color.DarkRed);
                    break;
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : Player Not Found: Creating Embed.");
                    embed.WithDescription("Player not found")
                        .WithColor(Color.DarkRed);

                }
            }

            Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : Removing Player: Sending Embed.");
            await Context.Channel.SendMessageAsync(embed: embed.Build());



        }

        [Command("insert")]
        [Alias("ip", "insertplayer")]
        [Summary("Insert a player into a specific spot in the queue.\nDefaults to the 1st element (front of queue).\n" +
            "Arguments to pass are discord userID (or @mention), active state, and position to add in.\n" +
            "ex: `+add 12345678987654321 false 5`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task InsertPlayer(string userID, bool isActive = true, int position = 0)
        {
            ulong _id;
            SocketGuildUser _user;
            if (!Caches.IsOpen.isOpen) return;

            if (position > 0) position--;
            try
            {
                _id = ulong.Parse(userID);
                _user = Context.Guild.GetUser(_id);
            }
            catch (Exception)
            {
                _user = Context.Guild.GetUser(Context.Message.MentionedUsers.First().Id);
            }

            await Context.Channel.TriggerTypingAsync();

            if (_user == null)
            {
                await Context.Channel.SendMessageAsync($"User was not found.");
                return;
            }

            foreach (Player player in PlayerList.Playerlist)
            {
                if (player.DiscordID == _user.Id)
                {
                    await Context.Channel.SendMessageAsync($"{player.Nickname} is already in the list at number {(PlayerList.Playerlist.IndexOf(player) + 1)}.");
                    return;
                }
            }

            Player player1 = new Player();
            player1.Nickname = _user.Username;
            player1.DiscordID = _user.Id;
            player1.IsActive = isActive;

            PlayerList.Playerlist.Insert(position, player1);
            if (!PlayerList.PlayerlistDB.Contains(player1))
                PlayerList.PlayerlistDB.Add(player1);
            await Context.Channel.SendMessageAsync($"{player1.Nickname} has been added to the list.");
        }

        [Command("add")]
        [Alias("ap", "addplayer")]
        [Summary("Adds a player to the end of the list.\nCan use DiscordID or @mention\nex: +add 12345678987654321 false")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task AddPlayer(string userID, bool isActive = true)
        {
            ulong _id;
            SocketGuildUser _user;
            if (!Caches.IsOpen.isOpen) return;

            try
            {
                _id = ulong.Parse(userID);
                _user = Context.Guild.GetUser(_id);
            }
            catch (Exception)
            {
                _user = Context.Guild.GetUser(Context.Message.MentionedUsers.First().Id);
            }

            await Context.Channel.TriggerTypingAsync();

            if (_user == null)
            {
                await Context.Channel.SendMessageAsync($"User was not found.");
                return;
            }

            foreach (Player player in PlayerList.Playerlist)
            {
                if (player.DiscordID == _user.Id)
                {
                    await Context.Channel.SendMessageAsync($"{player.Nickname} is already in the list at number {(PlayerList.Playerlist.IndexOf(player) + 1)}.");
                    return;
                }
            }

            Player player1 = new Player();
            player1.Nickname = _user.Username;
            player1.DiscordID = _user.Id;
            player1.IsActive = isActive;

            PlayerList.Playerlist.Add(player1);
            if(!PlayerList.PlayerlistDB.Contains(player1))
                PlayerList.PlayerlistDB.Add(player1);
            await Context.Channel.SendMessageAsync($"{player1.Nickname} has been added to the list.");
        }

        [Command("move")]
        [Summary("Move a player in the list from one position to another.")]
        public async Task MovePlayer(string userID, int position)
        {
            if (!Caches.IsOpen.isOpen) return;
            var embed = new EmbedBuilder();
            int location;
            if (position > 0) position--;

            ulong _id;
            SocketGuildUser _user;
            if (!Caches.IsOpen.isOpen) return;

            try
            {
                _id = ulong.Parse(userID);
                _user = Context.Guild.GetUser(_id);
            }
            catch (Exception)
            {
                _user = Context.Guild.GetUser(Context.Message.MentionedUsers.First().Id);
            }

            foreach (Player player in PlayerList.Playerlist)
            {
                if(player.DiscordID == _user.Id)
                {
                    location = PlayerList.Playerlist.IndexOf(player);
                    PlayerList.Playerlist.Remove(player);
                    PlayerList.Playerlist.Insert(position, player);

                    embed.WithTitle($"{player.Nickname} has been moved")
                        .WithDescription($" from position {location + 1} to {position + 1}.");

                    await Context.Channel.SendMessageAsync(embed: embed.Build());
                    return;
                }
            }

            embed.WithTitle("Player not found.")
                .WithDescription("Player was not found in the list.");

            await Context.Channel.SendMessageAsync(embed: embed.Build());                   

                
            

        }
    }

}



