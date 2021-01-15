using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System.Linq;
using Discord.WebSocket;

/*
 * 0.6c - Changes to file:
 * 
 * - Added qstats command.
 * 
 */


namespace CustomsQueueBot.Core.Commands
{
    public class ModCommands : ModuleBase<SocketCommandContext>
    {   
        [Command("active")]
        [Alias()]
        [Summary(": Change a player's active status. Use the command to change a player's active status between active <-> inactive\nex: +status 123456789 or +active @playername")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task ChangeActiveStatus(string userID = "")
        {
            if (!Caches.IsOpen.isOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                return;
            }
            IUser user;
            if (userID != "") // If an argument was passed.
            {

                try // check if argument is a ulong userID
                {
                    ulong _user = ulong.Parse(userID);
                    user = Context.Guild.GetUser(_user);
                }
                catch // Parse as an @mention
                {
                    user = Context.Guild.GetUser(Context.Message.MentionedUsers.First().Id);
                }
            }
            else // If no argument, user is the one who called the command.
            {
                user = Context.Message.Author;
            }

            foreach (Player player in PlayerList.Playerlist)
            {
                if (player.DiscordID == user.Id)
                {
                    if (player.IsActive)
                    {
                        player.IsActive = false;
                        PlayerList.PlayerlistDB[PlayerList.PlayerlistDB.IndexOf(player)].IsActive = false;
                        await Context.Channel.SendMessageAsync($"Player {player.Nickname} has been set to {(player.IsActive ? "active" : "inactive")}.");
                        await UpdateList();
                        return;
                    }
                    else
                    {
                        player.IsActive = true;
                        PlayerList.PlayerlistDB[PlayerList.PlayerlistDB.IndexOf(player)].IsActive = true;
                        await Context.Channel.SendMessageAsync($"Player {player.Nickname} has been set to {(player.IsActive ? "active" : "inactive")}.");
                        await UpdateList();
                        return;

                    }
                }
            }
            await Context.Channel.SendMessageAsync("Player not found in the list.");
        }

        [Command("add")]
        [Alias("ap", "addplayer")]
        [Summary(": Adds a player to the end of the list.\nCan use DiscordID or @mention\nSyntax: add [ID] or add [ID] [status]\nex: `add 123456789 false`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task AddPlayer(string userID, bool isActive = true)
        {
            ulong _id;
            SocketGuildUser _user;
            if (!Caches.IsOpen.isOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                return;
            }

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

            foreach (Player check in PlayerList.PlayerlistDB)
            {
                if (check.DiscordID == _user.Id)
                {
                    foreach (Player player in PlayerList.Playerlist)
                    {
                        if (player.DiscordID == _user.Id)
                        {
                            await Context.Channel.SendMessageAsync($"{player.Nickname} is already in the list at number {(PlayerList.Playerlist.IndexOf(player) + 1)}.");
                            return;
                        }
                        else
                        {
                            PlayerList.Playerlist.Add(check);
                            await Context.Channel.SendMessageAsync($"{check.Nickname} has been added to the list.");
                            return;
                        }
                    }
                }
            }

            Player player1 = new Player();
            player1.Nickname = _user.Username;
            player1.DiscordID = _user.Id;
            player1.IsActive = isActive;

            PlayerList.Playerlist.Add(player1);
            PlayerList.PlayerlistDB.Add(player1);

            await Context.Channel.SendMessageAsync($"{player1.Nickname} has been added to the queue.");
            await UpdateList();
        }

        [Command("remove")]
        [Alias("removeplayer", "rp", "-p")]
        [Summary(": Remove a player from the queue\nCan pass either a userID number or an @mention.\n" +
            "Can pass a reason as a second argument.\nex. `remove @[player] reason.`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task RemovePlayer(string userID, [Remainder] string reason = "")
        {
            if (!Caches.IsOpen.isOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                return;
            }

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
                    PlayerList.PlayerlistDB.Remove(player);
                    embed.WithDescription($"{player.Nickname} has been removed from the queue.\nReason: {reason}")
                        .WithColor(Color.DarkRed);
                    await _user.SendMessageAsync($"You have been removed from the queue.\nReason: {reason}");
                    break;
                }
                else
                {
                    embed.WithDescription("Player not found")
                        .WithColor(Color.DarkRed);
                }
            }

            await Context.Channel.SendMessageAsync(embed: embed.Build());
            await UpdateList();
        }

        [Command("insert")]
        [Alias("ip", "insertplayer")]
        [Summary(": Insert a player into a specific spot in the queue.\nDefaults to the 1st element (front of queue).\n" +
            "Arguments to pass are discord userID (or @mention), active state, and position to add in.\n" +
            "ex: `insert 123456789 false 5`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task InsertPlayer(string userID, bool isActive = true, int position = 0)
        {
            ulong _id;
            SocketGuildUser _user;
            if (!Caches.IsOpen.isOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                return;
            }

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

            foreach (Player check in PlayerList.PlayerlistDB)
            {
                if (check.DiscordID == _user.Id)
                {
                    foreach (Player player in PlayerList.Playerlist)
                    {
                        if (player.DiscordID == _user.Id)
                        {
                            await Context.Channel.SendMessageAsync($"{player.Nickname} is already in the list at number {(PlayerList.Playerlist.IndexOf(player) + 1)}.");
                            return;
                        }
                        else
                        {
                            PlayerList.Playerlist.Add(check);
                            await Context.Channel.SendMessageAsync($"{check.Nickname} has been added to the list.");
                            return;
                        }
                    }
                }
            }

            Player player1 = new Player();
            player1.Nickname = _user.Username;
            player1.DiscordID = _user.Id;
            player1.IsActive = isActive;

            PlayerList.Playerlist.Add(player1);
            PlayerList.PlayerlistDB.Add(player1);

            await Context.Channel.SendMessageAsync($"{player1.Nickname} has been added to the queue.");

        }

        [Command("move")]
        [Summary(": Move a player in the list from one position to another.\nCan use DiscordID or @mention.\nSyntax: move [ID] [position]\nex: `move 123456789 5`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task MovePlayer(string userID, int position)
        {
            if (!Caches.IsOpen.isOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                return;
            }
            var embed = new EmbedBuilder();
            int location;
            if (position > 0) position--;

            ulong _id;
            SocketGuildUser _user;
            

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
                if (player.DiscordID == _user.Id)
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

        [Command("ban")]
        [Summary(": Ban a player from the queue\nCan pass a reason as a second argument.\nCan use either @mention or Discord ID\nex. `ban 123456789 reason.`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task BanPlayer(string userID, [Remainder] string reason = "")
        {
            if (!Caches.IsOpen.isOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                return;
            }

            await Context.Channel.TriggerTypingAsync();
            ulong _id;
            SocketGuildUser _user;
            var embed = new EmbedBuilder().WithTitle("Ban Player");

            try  //Check if userID is an @mention or a discordID and assigns them appropriately.
            {
                _id = ulong.Parse(userID);
                _user = Context.Guild.GetUser(_id);
            }
            catch 
            {
                _user = Context.Guild.GetUser(Context.Message.MentionedUsers.First().Id);
            }

            // Search user list for username
            foreach (Player player in PlayerList.PlayerlistDB)
            {
                if (player.DiscordID == _user.Id)   // Remove user from list
                {
                    player.IsBanned = true;
                    player.BannedReason = reason;
                    if (PlayerList.Playerlist.Contains(player))
                        PlayerList.Playerlist.Remove(player);
                    PlayerList.PlayerlistDB[PlayerList.PlayerlistDB.IndexOf(player)].IsBanned = true;
                    PlayerList.Bannedlist.Add(player);
                    embed.WithDescription($"{player.Nickname} has been banned from the queue.\nReason: {reason}")
                        .WithColor(Color.DarkRed);
                    await _user.SendMessageAsync($"You have been banned from the queue.\nReason: {reason}");
                    break;
                }
                else
                {
                    embed.WithDescription("Player not found")
                        .WithColor(Color.DarkRed);
                }
            }

            await Context.Channel.SendMessageAsync(embed: embed.Build());

        }

        [Command("unban")]
        [Summary(": Removes a player from the banned list. Unbans the player.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task UnbanAsync(string userID)
        {
            SocketGuildUser _user;
            ulong _id;
            var embed = new EmbedBuilder().WithTitle("Unban");

            try  //Check if userID is an @mention or a discordID and assigns them appropriately.
            {
                _id = ulong.Parse(userID);
                _user = Context.Guild.GetUser(_id);
            }
            catch (Exception)
            {
                _user = Context.Guild.GetUser(Context.Message.MentionedUsers.First().Id);
            }

            foreach (Player player in PlayerList.Bannedlist)
            {
                if (player.DiscordID == _user.Id)
                {
                    if (PlayerList.PlayerlistDB.Contains(player))
                    {
                        PlayerList.PlayerlistDB[PlayerList.PlayerlistDB.IndexOf(player)].IsBanned = false;
                        PlayerList.Bannedlist.Remove(player);
                        embed.WithDescription($"{player.Nickname}'s ban has been lifted.")
                            .WithColor(Color.DarkRed);
                        await _user.SendMessageAsync($"Your ban from the queue has been lifted.");
                        break;
                    }
                    else
                    {
                        embed.WithDescription("Player not found")
                            .WithColor(Color.DarkRed);
                    }
                    break;
                }
            }

            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }

/*        [Command("blist")]
        [Summary(": Shows all banned players for the session.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task ShowBannedList()
        {
            if (!Caches.IsOpen.isOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                return;
            }

            var embed = new EmbedBuilder().WithTitle($"{PlayerList.Bannedlist.Count} banned players:")
                .WithDescription("Banned List");
            List<EmbedFieldBuilder> FieldList = new List<EmbedFieldBuilder>();

            foreach(Player player in PlayerList.Bannedlist)
            {
                var field = new EmbedFieldBuilder().WithName(player.Nickname).WithValue(player.BannedReason);
                FieldList.Add(field);
            }

            foreach (EmbedFieldBuilder field in FieldList)
            {
                embed.AddField(field);
            }

            await Context.Channel.SendMessageAsync(embed: embed.Build());

        }
*/

        [Command("qstat")]
        [Alias("qstats")]
        [Summary(": Displays the total stats of the currently opened queue.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task QueueStats()
        {
            double AverageGames = 0;

            var embed = new EmbedBuilder().WithTitle("Queue Stats")
                .WithDescription("Statistics:")
                .WithColor(Color.DarkGrey);

            var field = new EmbedFieldBuilder();

            field.WithName("Total players:")
                .WithValue(PlayerList.PlayerlistDB.Count());
            embed.AddField(field);

            foreach (Player player in PlayerList.PlayerlistDB)
                AverageGames += player.GamesPlayed;

            var field1 = new EmbedFieldBuilder();
            field1.WithName("Avg. Games Played:")
                .WithValue( (AverageGames / PlayerList.PlayerlistDB.Count()) );
            embed.AddField(field1);

            var uptime = DateTime.Now - Caches.Messages.ReactionMessage.CreatedAt;
       
            var field2 = new EmbedFieldBuilder();
            field2.WithName("Queue Uptime:")
                .WithValue($"{uptime.Hours} h {uptime.Minutes} m");
            embed.AddField(field2);

            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }
   
        [Command("gp+")]
        [Summary(": Adds one to the games played counter of provided user\nAccepts either @mentions or User IDs.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task GamesPlayedAdd(string userID)
        {
            if (!Caches.IsOpen.isOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                return;
            }

            await Context.Channel.TriggerTypingAsync();
            ulong _id;
            SocketGuildUser _user;

            try  //Check if userID is an @mention or a discordID and assigns them appropriately.
            {
                _id = ulong.Parse(userID);
                _user = Context.Guild.GetUser(_id);
            }
            catch
            {
                _user = Context.Guild.GetUser(Context.Message.MentionedUsers.First().Id);
            }

            foreach (Player player in PlayerList.PlayerlistDB)
            {
                if (player.DiscordID == _user.Id)
                {
                    player.GamesPlayed++;
                    await Context.Channel.SendMessageAsync($"{player.Nickname}'s game count incremented by 1.");
                }
            }
        }

        [Command("gp-")]
        [Summary(": Subtracts one from the games played counter of provided user\nAccepts either @mentions or User IDs.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task GamesPlayedSubtracts(string userID)
        {
            if (!Caches.IsOpen.isOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                return;
            }

            await Context.Channel.TriggerTypingAsync();
            ulong _id;
            SocketGuildUser _user;

            try  //Check if userID is an @mention or a discordID and assigns them appropriately.
            {
                _id = ulong.Parse(userID);
                _user = Context.Guild.GetUser(_id);
            }
            catch
            {
                _user = Context.Guild.GetUser(Context.Message.MentionedUsers.First().Id);
            }

            foreach (Player player in PlayerList.PlayerlistDB)
            {
                if (player.DiscordID == _user.Id)
                {
                    if (player.GamesPlayed > 0)
                    { 
                        player.GamesPlayed--; 
                        await Context.Channel.SendMessageAsync($"{player.Nickname}'s game count decremented by 1.");
                    }
                }
            }
        }

        /*        [Command("test")]
                [Summary(": For testing code purposes. Don't actually use this without express permission.")]
                [RequireUserPermission(GuildPermission.ManageChannels)]
                public async Task testing()
                {



                }
            */

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
