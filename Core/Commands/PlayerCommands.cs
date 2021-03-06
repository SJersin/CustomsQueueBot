﻿using System;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;


/*
 * 0.8 changes made:
 * 
 * - Clarified intent and usage of the "Quit" command.
 * - "Join" command put on indefinite hiatus. 
 * 
 * 0.6 - Changes to file:
 * 
 * -Status: no longer takes arguements. Instead toggles between active and inactive states.
 * -Join: Start work on command to allow players to join the queue without having to click the
 *     reaction emote. Why? Why not? Will need to find out if desired. But until then, still doing it.
 * -Remove certain player commands to check stability.
 * 
 */

namespace CustomsQueueBot.Core.Commands
{
    public class PlayerCommands : ModuleBase<SocketCommandContext>
    {
/*        [Command("list")]
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
                if (player.GuildUser.Id == (ulong)user.Id)
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
*/

        [Command("quit")]
        [Summary(": Indicate that you no longer intend to join any more custom games. This will remove you from the list." +
            " To prevent accidental use, the user will need to send 'yes' or 'agree' with the command." +
            "\nEx. Syntax: ` quit yes `")]
        public async Task QuitCustoms(string agree = "")
        {
            if (!Caches.Lobby.IsOpen) return;

            var user = Context.Message.Author;
            if (agree.ToLower() == "agree" || agree.ToLower() == "yes")
            {
                Player quitter;

                Console.WriteLine("DEBUG: ForLoop (DB) Start");
                foreach (Player player in PlayerList.PlayerlistDB)
                {
                    if (player.GuildUser.Id == user.Id)
                    {
                        quitter = player;
                        PlayerList.PlayerlistDB.Remove(quitter);
                        break;
                    }
                }
                foreach (Player player in PlayerList.Playerlist)
                {
                    if (player.GuildUser.Id == user.Id)
                    {
                        quitter = player;
                        PlayerList.Playerlist.Remove(quitter);
                        break;
                    }
                }
                  
                string response = $"You have removed yourself from the queue {user.Username}";

                await Context.Channel.SendMessageAsync(response);
                await UpdateMethods.Update.PlayerList();
            }
            else
            {
                await Context.Channel.SendMessageAsync($"Consent required to use command {user.Username}.");
            }
        }
        
        [Command("status")]
        [Summary(": Check your active status. \nex: +status")]
        public async Task SetActiveStatus()
        {
            if (!Caches.Lobby.IsOpen)
            {
                await Context.Channel.SendMessageAsync("There is no open queue.");
                return;
            }
            IUser user;
           
                user = Context.Message.Author; // User is player who used the command
            

            foreach (Player player in PlayerList.Playerlist)
            {
                if (player.GuildUser.Id == user.Id) // Find their player data
                {
                    if (player.IsActive) // Set to inactive
                    {
                        await Context.Channel.SendMessageAsync($"Player {player.GuildUser.Username} is currently {(player.IsActive ? "active" : "inactive")}.");
                        return;
                    }
                }
            }

            await Context.Channel.SendMessageAsync("Player not found in the list.");
        }

    }
}
