﻿using System;
using System.IO;
using System.Text;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System.Linq;
using Discord.WebSocket;
using Serilog;

/*
 * 0.8.3
 * 
 * Changed Recall to accept a message from the mod. or nothing at all with no message.
 * 
 * 
 * 
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
        [Summary(": Change a player's active status. Use the command to change a player's active status between active <-> inactive." +
            "\nCan use either a user id or @mention. ex: `active @playername`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task ChangeActiveStatus(string userID = "")
        {
            if (!Caches.Lobby.IsOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open q-υωυ-e, silly.");
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
                if (player.GuildUser.Id == user.Id)
                {
                    if (player.IsActive)
                    {
                        player.IsActive = false;
                        PlayerList.PlayerlistDB[PlayerList.PlayerlistDB.IndexOf(player)].IsActive = false;
                        await Context.Channel.SendMessageAsync($"Player {player.GuildUser.Username} has been set to {(player.IsActive ? "active" : "inactive")}.");
                        await UpdateMethods.Update.PlayerList();
                        return;
                    }
                    else
                    {
                        player.IsActive = true;
                        PlayerList.PlayerlistDB[PlayerList.PlayerlistDB.IndexOf(player)].IsActive = true;
                        await Context.Channel.SendMessageAsync($"Player {player.GuildUser.Username} has been set to {(player.IsActive ? "active" : "inactive")}.");
                        await UpdateMethods.Update.PlayerList();
                        return;

                    }
                }
            }
            await Context.Channel.SendMessageAsync("Player not found in the list.");
        }

        [Command("add")]
        [Alias("ap", "addplayer")]
        [Summary(": Adds a player to the end of the list.\nCan use a DiscordID or @mention\nSyntax: add [ID] or add [ID] [status]\nex: `add 123456789 false`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task AddPlayer(string userID, bool isActive = true)
        {
            ulong _id;
            SocketGuildUser _user;
            if (!Caches.Lobby.IsOpen)
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
                if (check.GuildUser.Id == _user.Id)
                {
                    foreach (Player player in PlayerList.Playerlist)
                    {
                        if (player.GuildUser.Id == _user.Id)
                        {
                            await Context.Channel.SendMessageAsync($"{player.GuildUser.Username} is already in the list at number {(PlayerList.Playerlist.IndexOf(player) + 1)}.");
                            return;
                        }
                        else
                        {
                            PlayerList.Playerlist.Add(check);
                            await Context.Channel.SendMessageAsync($"{check.GuildUser.Username} has been added to the list.");
                            return;
                        }
                    }
                }
            }

            Player player1 = new Player();

            player1.IsActive = isActive;
            player1.EntryTime = DateTime.Now;
            player1.GuildUser = _user;

            PlayerList.Playerlist.Add(player1);
            PlayerList.PlayerlistDB.Add(player1);

            await Context.Channel.SendMessageAsync($"{player1.GuildUser.Username} has been added to the queue.");
            await UpdateMethods.Update.PlayerList();
        }

        [Command("remove")]
        [Alias("removeplayer", "rp", "-p")]
        [Summary(": Remove a player from the queue\nCan pass either a userID number or an @mention.\n" +
            "Can pass a reason as a second argument.\nex. `remove @[player] [reason].`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task RemovePlayer(string userID, [Remainder] string reason = "")
        {
            if (!Caches.Lobby.IsOpen)
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
                if (player.GuildUser.Id == _user.Id)   // Remove user from list
                {
                    PlayerList.Playerlist.Remove(player);
                    PlayerList.PlayerlistDB.Remove(player);
                    embed.WithDescription($"{player.GuildUser.Username} has been removed from the queue.\nReason: {reason}")
                        .WithColor(Color.DarkRed);
                //    await _user.SendMessageAsync($"You have been removed from the queue.\nReason: {reason}");
                    break;
                }
                else
                {
                    embed.WithDescription("Player not found")
                        .WithColor(Color.DarkRed);
                }
            }

            await Context.Channel.SendMessageAsync(embed: embed.Build());
            await UpdateMethods.Update.PlayerList();
        }

        [Command("insert")]
        [Alias("ip", "insertplayer")]
        [Summary(": Insert a player into a specific spot in the queue.\nDefaults to the 1st element (front of queue).\n" +
            "Arguments to pass are a discord userID (or @mention), active state, and position to add in.\n" +
            "ex: `insert 123456789 false 5`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task InsertPlayer(string userID, bool isActive = true, int position = 0)
        {
            ulong _id;
            SocketGuildUser _user;
            if (!Caches.Lobby.IsOpen)
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
                if (check.GuildUser.Id == _user.Id)
                {
                    foreach (Player player in PlayerList.Playerlist)
                    {
                        if (player.GuildUser.Id == _user.Id)
                        {
                            await Context.Channel.SendMessageAsync($"{player.GuildUser.Username} is already in the list at number {(PlayerList.Playerlist.IndexOf(player) + 1)}.");
                            return;
                        }
                        else
                        {
                            PlayerList.Playerlist.Add(check);
                            await Context.Channel.SendMessageAsync($"{check.GuildUser.Username} has been added to the list.");
                            return;
                        }
                    }
                }
            }

            Player player1 = new Player();
            player1.GuildUser = _user;
            player1.IsActive = isActive;

            PlayerList.Playerlist.Add(player1);
            PlayerList.PlayerlistDB.Add(player1);

            await Context.Channel.SendMessageAsync($"{player1.GuildUser.Username} has been added to the queue.");

        }

        [Command("move")]
        [Summary(": Move a player in the list from one position to another.\nCan use DiscordID or @mention.\nSyntax: move [ID] [position]\nex: `move 123456789 5`")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task MovePlayer(string userID, int position)
        {
            if (!Caches.Lobby.IsOpen)
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
                if (player.GuildUser.Id == _user.Id)
                {
                    location = PlayerList.Playerlist.IndexOf(player);
                    PlayerList.Playerlist.Remove(player);
                    PlayerList.Playerlist.Insert(position, player);

                    embed.WithTitle($"{player.GuildUser.Username} has been moved")
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
            if (!Caches.Lobby.IsOpen)
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
                if (player.GuildUser.Id == _user.Id)   // Remove user from list
                {
                    player.IsBanned = true;
                    player.BannedReason = reason;
                    if (PlayerList.Playerlist.Contains(player))
                        PlayerList.Playerlist.Remove(player);
                    PlayerList.PlayerlistDB[PlayerList.PlayerlistDB.IndexOf(player)].IsBanned = true;
                    PlayerList.Bannedlist.Add(player);
                    embed.WithDescription($"{player.GuildUser.Username} has been banned from the queue.\nReason: {reason}")
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
        [Summary(": Removes a player from the banned list. Can use either @mention or Discord ID.")]
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
                if (player.GuildUser.Id == _user.Id)
                {
                    if (PlayerList.PlayerlistDB.Contains(player))
                    {
                        PlayerList.PlayerlistDB[PlayerList.PlayerlistDB.IndexOf(player)].IsBanned = false;
                        PlayerList.Bannedlist.Remove(player);
                        embed.WithDescription($"{player.GuildUser.Username}'s ban has been lifted.")
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

        [Command("stats")]
        [Alias("qstats")]
        [Summary(": Displays the total stats of the currently opened queue.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task QueueStats()
        {
            if (!Caches.Lobby.IsOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                return;
            }

            double AverageGames = 0;

            var embed = new EmbedBuilder().WithTitle("Statistics")
                .WithDescription("Queue Stats:")
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
                .WithValue($"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m");
            embed.AddField(field2);

            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }
   
        [Command("gp+")]
        [Summary(": Adds one to the games played counter of provided user\nAccepts either @mentions or User IDs.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task GamesPlayedAdd(string userID)
        {
            if (!Caches.Lobby.IsOpen)
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
                if (player.GuildUser.Id == _user.Id)
                {
                    player.GamesPlayed++;
                    await Context.Channel.SendMessageAsync($"{player.GuildUser.Username} ({player.GuildUser.Username})'s game count incremented by 1.");
                    break;
                }
            }
        }

        [Command("gp-")]
        [Summary(": Subtracts one from the games played counter of provided user\nAccepts either @mentions or User IDs.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task GamesPlayedSubtract(string userID)
        {
            if (!Caches.Lobby.IsOpen)
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
                if (player.GuildUser.Id == _user.Id)
                {
                    if (player.GamesPlayed > 0)
                    { 
                        player.GamesPlayed--; 
                        await Context.Channel.SendMessageAsync($"{player.GuildUser.Username} ({player.GuildUser.Username})'s game count decremented by 1.");
                        break;
                    }
                    else
                        await Context.Channel.SendMessageAsync($"Failed to decrement.");
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

        [Command("recall")]
        [Summary(": Announces the last group called by Next or Random again.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task RecallList([Remainder] string msg = "")
        {
            if (!Caches.Lobby.IsOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open q-υωυ-e, silly.");
                return;
            }

            string mentions = "";
            foreach (Player player in PlayerList.RecentList)
                mentions += $"{player.GuildUser.Mention} "; // @mentions the players
            
            mentions += $"\n\n{msg}";

            try
            {
                await Context.Channel.SendMessageAsync(mentions);
            }
            catch (Exception e)
            {
                Log.Information("Recall embed failed to send.");
                Log.Information(e.Message);

            }
            
            
        }

    }
}
