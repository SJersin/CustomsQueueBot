using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Timers;
using Serilog;

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
            await Client.SetGameAsync($"{Config.bot.Prefix}help"); // Shows the prefix and "help" under Username.
            await Client.SetStatusAsync(UserStatus.Online); //Set the bot as online (enumerator)
        }

        private async Task OnMessageReceived(SocketMessage _message)
        {
            //**************************************************************
            //          DATABASE TESTING ZONE 

            //Core.Database.Database db = new Core.Database.Database(); //          DATABASE TESTING ZONE 
            //
            //**************************************************************


            var Message = _message as SocketUserMessage;    //Gets the user message object from SocketMessage
            var Context = new SocketCommandContext(Client, Message);

            // Ignore system messages, or messages from other bots
            if (!(_message is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
           
            int ArgPos = 0;
            if (!(Message.HasStringPrefix(Config.bot.Prefix, ref ArgPos) || Message.HasMentionPrefix(Client.CurrentUser, ref ArgPos))) return; //Ignore non-prefixed messages or bot @mention(?)

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

                    if (reaction.User.Value.IsBot) return;
                    if (reaction == null || Caches.Messages.ReactionMessage == null) return;
                    if (!user.Roles.Any(r => r.Name == Config.bot.Role)) return; //ignore users who don't have the proper role. Don't know exactly if this is needed yet.

                    var emoji = new Emoji(Config.bot.Reaction);  //"👍"
                                                                 //var emote = Emote.Parse();
                    bool exists = false;
                    Player playerCheck = new Player();

                    if (PlayerList.Playerlist.Count == 0 && user.Roles.Any(r => r.Name == Config.bot.Role)) // If list is empty and user has proper role.
                    {

                        playerCheck.IsActive = true;
                        playerCheck.EntryTime = DateTime.Now;
                        playerCheck.GuildUser = user;
                        PlayerList.Playerlist.Add(playerCheck);
                        PlayerList.PlayerlistDB.Add(playerCheck);
                        await UpdateMethods.Update.PlayerList();
                    }
                    else if (PlayerList.Playerlist.Count > 0) // If list isn't empty...
                    {
                        try
                        {
                            foreach (Player player in PlayerList.Playerlist) // Check list for user. if found and is not active, isActive = true.
                                if (player.GuildUser == user && !player.IsActive)
                                {
                                    player.IsActive = true;
                                    await UpdateMethods.Update.PlayerList();
                                    return;
                                }
                        }
                        catch (Exception e)
                        {
                            Log.Information(e.Message);
                        }

                        if (!exists && user.Roles.Any(r => r.Name == Config.bot.Role))    // If no user is found and user has "customs" role, add user to playerlist
                        {
                            playerCheck.IsActive = true;
                            playerCheck.EntryTime = DateTime.Now;
                            playerCheck.GuildUser = user;
                            PlayerList.Playerlist.Add(playerCheck);
                            PlayerList.PlayerlistDB.Add(playerCheck);
                            await UpdateMethods.Update.PlayerList();
                        }
                        
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine();
                    Log.Information("Error reading reaction from user {u}.\nException: {e}\n", reaction.User.Value.Username, e.Message);
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

            //userMessage.id is the id of the message the user is reacting to
            //Check if the message the user is reacting to is a valid reaction message
            //If valid, the message id should exist in our ReactionMessages collection

            #region "Queue Message Reaction"
            if (Caches.Messages.ReactionMessage.Id == userMessage.Id)
            {
                try
                {
                    var user = reaction.User.Value;

                    foreach (Player player in PlayerList.Playerlist)
                    {
                        if (player.GuildUser.Id == user.Id)
                        {
                            player.IsActive = false;
                            PlayerList.PlayerlistDB[PlayerList.PlayerlistDB.IndexOf(player)].IsActive = false;

                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Information("{datetime} ReactionRemoved method in EventHandler:\n"
                        + "Exception: {e}", DateTime.Now, e.Message);
                }
                await UpdateMethods.Update.PlayerList();

                #endregion

            }

        }


    }
}
