using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System.Linq;
using Discord.WebSocket;

namespace CustomsQueueBot.Core.Commands
{
    public class PlayerCommands : ModuleBase<SocketCommandContext>
    {


        [Command("list")]
        [Alias("playerlist")]
        [Summary("Shows all players in the playerlist.")]
        public async Task ShowPlayerList()
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
                    .WithValue($"Active: {(player.IsActive ? "Yes" : "No")}")
                    .WithIsInline(true));

                }
                foreach (EmbedFieldBuilder field in PlayerListField)
                    embed.AddField(field);
            }

            await ReplyAsync(embed: embed.Build());

        }

        [Command("place")]
        [Alias("position")]
        public async Task GetPlayerPosition()
        {
            if (!Caches.IsOpen.isOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                return;
            }
            var embed = new EmbedBuilder();

            var user = Context.Guild.GetUser(Context.User.Id);
            int count = 0;
            foreach (Player player in PlayerList.Playerlist)
            {
                count++;
                if (player.DiscordID == (ulong)user.Id)
                {


                     embed.WithTitle($"{player.Nickname} your q-UωU-e position is:")
                        .WithDescription($"Number {count}. ").WithColor(Color.DarkTeal);

                    await Context.Channel.SendMessageAsync(embed: embed.Build());
                    return;
                }
            }


             embed.WithTitle($"Player not found!")
                .WithDescription($"You might not be in the queue.").WithColor(Color.DarkRed);

            await Context.Channel.SendMessageAsync(embed: embed.Build());


        }

        [Command("active")]
        [Alias("status")]
        [Summary("Change your active status. If you want to be active, type true\nIf you want to be inactive, type false\nex: +status false <-Sets you inactive")]
        public async Task SetActiveStatus(bool isActive, [Remainder] string mentionUser = "")
        {
            if (!Caches.IsOpen.isOpen) return;
            IUser user;
            if(mentionUser != "")
            {

                try
                {
                    ulong _user = ulong.Parse(mentionUser);
                    user = Context.Guild.GetUser(_user);
                }
                catch
                {
                    user = Context.Guild.GetUser(Context.Message.MentionedUsers.First().Id);
                }
            }
            else
            {
                user = Context.Message.Author;
            }

            foreach (Player player in PlayerList.Playerlist)
            {
                if (player.DiscordID == user.Id)
                {
                    player.IsActive = isActive;
                    PlayerList.PlayerlistDB[PlayerList.PlayerlistDB.IndexOf(player)].IsActive = isActive;
                    await Context.Channel.SendMessageAsync($"Player {player.Nickname} has been set to {(isActive ? "active" : "inactive")}.");
                    return;
                }
            }

            await Context.Channel.SendMessageAsync("Player not found in the list.");
        }

    }
}
