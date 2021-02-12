using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
// using CustomsQueueBot.Core.Database;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace CustomsQueueBot
{
    class EventHandler
    {
        private DiscordSocketClient Client;
        private readonly CommandService Commands;
        private readonly IServiceProvider _Services;


        public EventHandler(IServiceProvider Services)
        {
            Client = Services.GetRequiredService<DiscordSocketClient>();
            Commands = Services.GetRequiredService<CommandService>();
            _Services = Services;
        }

        public Task InitializeAsync()
        {
            Client.MessageReceived += OnMessageReceived;
            Client.Ready += Ready_Event;
            Client.ReactionAdded += ReactionAdded;
            Client.ReactionRemoved += ReactionRemoved;

            return Task.CompletedTask;
        }

        private async Task Ready_Event()
        {
            Console.WriteLine($"{DateTime.Now} => [READY_EVENT] : {Client.CurrentUser.Username} is ready."); // Remember, consistancy is ImPoRtAnT.
            await Client.SetGameAsync($"{Config.bot.prefix}help"); // Shows the prefix and "help" under Username.
            await Client.SetStatusAsync(UserStatus.Online); //Set the bot as online (enumerator)
        }

        private async Task OnMessageReceived(SocketMessage _message)
        {
            var Message = _message as SocketUserMessage;    //Gets the user message object from SocketMessage
            var Context = new SocketCommandContext(Client, Message);

            // Ignore system messages, or messages from other bots
            if (!(_message is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            // Bans oVerbosity
            if (message.Author.Id == 254809263405269005)
            {
                Random random = new Random();
                int rand = random.Next(1001);
                if (rand == 69 || rand == 420)
                {
                    await Context.Channel.SendMessageAsync("YOU'VE BEEN B& YOU SILLY BOT!!");
                    return;
                }
            }

            

            int ArgPos = 0;
            if (!(Message.HasStringPrefix(Config.bot.prefix, ref ArgPos) || Message.HasMentionPrefix(Client.CurrentUser, ref ArgPos))) return; //Ignore non-prefixed messages or has an @mention(?)

            var Result = await Commands.ExecuteAsync(Context, ArgPos, _Services); // Third arguement set IServices. Use null if not using an IService.
            if (!Result.IsSuccess && Result.Error != CommandError.UnknownCommand)
            {
                Console.WriteLine($"{DateTime.Now} at Command: {Commands.Search(Context, ArgPos).Commands[0].Command.Name} in {Commands.Search(Context, ArgPos).Commands[0].Command.Module.Name }] {Result.ErrorReason }");

                var embed = new EmbedBuilder(); // Creates embed object neccessary to display things.

                embed.WithTitle("***ERROR***");
                embed.WithDescription(Result.ErrorReason);

                await Context.Channel.SendMessageAsync(embed: embed.Build()); //Must be embed: embed.Build()
            }
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> usermessage, ISocketMessageChannel channel, SocketReaction reaction)
        {

            #region "Queue Message Reaction"
            if(!(Caches.Messages.ReactionMessage is null) &&  usermessage.Id == Caches.Messages.ReactionMessage.Id)
                try
                {
                    var user = reaction.User.Value as SocketGuildUser;

                    Console.WriteLine($"{DateTime.Now} at ReactionAdded in EventHandler: Function Called.");
                    if (reaction.User.Value.IsBot) return;
                    Console.WriteLine($"{DateTime.Now} at ReactionAdded in EventHandler: Bot check: Passed.");
                    if (reaction == null || Caches.Messages.ReactionMessage == null) return;
                    Console.WriteLine($"{DateTime.Now} at ReactionAdded in EventHandler: Null or Empty message check: Passed.");
                    if (!user.Roles.Any(r => r.Name == Config.bot.role)) return; //ignore users who don't have the proper role. Don't know exactly if this is needed yet.

                    Console.WriteLine($"{DateTime.Now} at ReactionAdded in EventHandler: Role check: Passed.");

                    var emoji = new Emoji(Config.bot.reaction);  //"👍"
                                                                 //var emote = Emote.Parse();
                    bool exists = false;
                    Player playerCheck = new Player();

                    if (PlayerList.Playerlist.Count == 0 && user.Roles.Any(r => r.Name == Config.bot.role))
                    {
                        playerCheck.DiscordID = user.Id;
                        playerCheck.DiscordName = user.Username;
                        playerCheck.Nickname = user.Nickname;
                        playerCheck.IsActive = true;
                        PlayerList.Playerlist.Add(playerCheck);
                        PlayerList.PlayerlistDB.Add(playerCheck);
                        await UpdateMethods.Update.PlayerList();
                    }
                    else if (PlayerList.Playerlist.Count > 0)
                    {
                        foreach (Player player in PlayerList.PlayerlistDB)
                        {
                            if (player.DiscordID == user.Id)  // Check if player is in the database
                            {
                                playerCheck = player;
                            }
                        }
                        Console.WriteLine($"{DateTime.Now} at ReactionAdded in EventHandler: Player Check returns: {playerCheck.DiscordID}.");
                        if (playerCheck.DiscordID == 0 && user.Roles.Any(r => r.Name == Config.bot.role)) //Player not found in DB
                        {
                            playerCheck.DiscordID = user.Id;
                            playerCheck.DiscordName = user.Username;
                            playerCheck.Nickname = user.Nickname;
                            playerCheck.IsActive = true;
                            PlayerList.Playerlist.Add(playerCheck);
                            PlayerList.PlayerlistDB.Add(playerCheck);
                            await UpdateMethods.Update.PlayerList();
                            return;
                        }
                        else
                        {
                            foreach (Player player in PlayerList.Playerlist)  // Check if player is in the list
                            {
                                if (player.DiscordID == playerCheck.DiscordID && user.Roles.Any(r => r.Name == Config.bot.role))
                                {
                                    Console.WriteLine($"{DateTime.Now} at ReactionAdded in EventHandler: Player found. Switching status to active.");
                                    player.IsActive = true;
                                    await UpdateMethods.Update.PlayerList();
                                    return;
                                }
                            }
                        }
                    }
                    else if (!exists)
                    {
                        playerCheck.DiscordID = user.Id;
                        playerCheck.DiscordName = user.Username;
                        playerCheck.Nickname = user.Nickname;
                        playerCheck.IsActive = true;
                        PlayerList.Playerlist.Add(playerCheck);
                        PlayerList.PlayerlistDB.Add(playerCheck);
                        await UpdateMethods.Update.PlayerList();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine();
                    Console.WriteLine(e.Message);
                    Console.WriteLine();
                }
            #endregion

            //else


        }

        



        private async Task ReactionRemoved(Cacheable<IUserMessage, ulong> userMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!reaction.User.IsSpecified) return;
            if (reaction.User.Value.IsBot) return;
            if (reaction is null || (Caches.Messages.ReactionMessage is null)) return;

            Console.WriteLine($"{DateTime.Now} at ReactionRemoved in EventHandler: PBasic check: Passed.");

            //userMessage.id is the id of the message the user is reacting to
            //Check if the message the user is reacting to is a valid reaction message
            //If valid, the message id should exist in our ReactionMessages collection

            #region "Queue Message Reaction"
            if (Caches.Messages.ReactionMessage.Id == userMessage.Id)
            {
                Console.WriteLine($"{DateTime.Now} at ReactionRemoved in EventHandler: Reaction Message check: Passed.");
                var user = reaction.User.Value;

                foreach (Player player in PlayerList.Playerlist)
                {
                    Console.WriteLine($"{DateTime.Now} at ReactionRemoved in EventHandler: Playerlist ForEach~~.");
                    if (player.DiscordID == user.Id)
                    {
                        Console.WriteLine($"{DateTime.Now} at ReactionRemoved in EventHandler: User check: Passed.");
                        player.IsActive = false;
                        PlayerList.PlayerlistDB[PlayerList.PlayerlistDB.IndexOf(player)].IsActive = false;

                    }
                }
            }
            await UpdateMethods.Update.PlayerList();
           
            #endregion
        
        }



    }
}
