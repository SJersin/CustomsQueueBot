using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
// using CustomsQueueBot.Core.Database;
using Microsoft.Extensions.DependencyInjection;

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
            // Console.WriteLine($"{Client.CurrentUser.Username} is ready."); // $ is String Injection... gotta look that up.
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

            int ArgPos = 0;
            if (!(Message.HasStringPrefix(Config.bot.prefix, ref ArgPos) || Message.HasMentionPrefix(Client.CurrentUser, ref ArgPos))) return; //Ignore non-prefixed messages or has an @mention(?)

            var Result = await Commands.ExecuteAsync(Context, ArgPos, _Services); // Third arguement set IServices. Use null if not using an IService.
            if (!Result.IsSuccess && Result.Error != CommandError.UnknownCommand)
            {
                Console.WriteLine($"{DateTime.Now} at Command: {Commands.Search(Context, ArgPos).Commands[0].Command.Name} in {Commands.Search(Context, ArgPos).Commands[0].Command.Module.Name }] {Result.ErrorReason }");

                var embed = new EmbedBuilder(); // Creates object neccessary to display things.

                embed.WithTitle("***ERROR***");
                embed.WithDescription(Result.ErrorReason);

                await Context.Channel.SendMessageAsync(embed: embed.Build()); //Must be embed: embed.Build()
            }
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> usermessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            // if (!reaction.User.IsSpecified) return;
            if (reaction.MessageId != Caches.ReactionMessages.ReactionMessage) return;
            //     if (reaction.Emote.Name != "👍") return;   // Change over to Config.bot.reaction
            if (reaction.User.Value.IsBot) return;

            Console.WriteLine($"{DateTime.Now} at ReactionAdded in EventHandler: Function Called.");

            var user = reaction.User.Value as SocketGuildUser;
            var emote = new Emoji("👍");
            bool exists = false;
            if (PlayerList.Playerlist.Count != 0)
            {
                foreach (Player player in PlayerList.Playerlist)
                {
                    if (player.DiscordID == user.Id)
                    {
                        exists = true; 
                        Console.WriteLine($"{DateTime.Now} at ReactionAdded in EventHandler: Player found. Switching status to active.");
                        player.IsActive = true;
                    }
                }
                    Console.WriteLine($"{DateTime.Now} at ReactionAdded in EventHandler: ForEach iteration active.");
                    Console.WriteLine($"{DateTime.Now} at ReactionAdded in EventHandler: Check for player: {user.Username}: {user.Id}.");
                    if (!exists)
                    {
                        Console.WriteLine($"{DateTime.Now} at ReactionAdded in EventHandler: Player not found. Creating player.");
                        Player newPlayer = new Player();
                        newPlayer.DiscordID = user.Id;
                        newPlayer.Nickname = user.Username;
                        newPlayer.IsActive = true;
                        PlayerList.Playerlist.Add(newPlayer);
                    }
            }
            else
            {
                Console.WriteLine($"{DateTime.Now} at ReactionAdded in EventHandler: PlayerList is empty. Creating new player.");
                Player player = new Player();
                player.DiscordID = user.Id;
                player.Nickname = user.Username;
                player.IsActive = true;
                PlayerList.Playerlist.Add(player);
                Console.WriteLine($"{DateTime.Now} at ReactionAdded in EventHandler: New player {player.Nickname} successfully added.");
            }

        }



        private async Task ReactionRemoved(Cacheable<IUserMessage, ulong> userMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!reaction.User.IsSpecified) return;
            if (reaction.MessageId != Caches.ReactionMessages.ReactionMessage) return;

            Console.WriteLine($"{DateTime.Now} at ReactionRemoved in EventHandler: Function Called.");


            //userMessage.id is the id of the message the user is reacting to
            //Check if the message the user is reacting to is a valid reaction message
            //If valid, the message id should exist in our ReactionMessages collection
            //Valid reaction messages are the only message that should assign or remove roles
            if (Caches.ReactionMessages.ReactionMessage == userMessage.Id)
            {
                var user = reaction.User.Value;

                foreach (Player player in PlayerList.Playerlist)
                {
                    if (player.DiscordID == user.Id)
                    {
                        Console.WriteLine($"{DateTime.Now} at ReactionRemoved in EventHandler: Player found. Switching to inactive.");
                        player.IsActive = false;
                    }
                }


                //The unicode string (👍 and 👎) is used when comparing Discord emojis
                //    if (reaction.Emote.Name.Equals("👍"))
                //Retrieve the "good role" from the guild, using the role id
                //        role = channel.Guild.GetRole(123456789) as SocketGuildChannel;
                //    else reaction.Emote.Name.Equals("👎") Then
                //Retrieve the "bad role" from the guild, using the role id
                //        role = DirectCast(channel, SocketGuildChannel).Guild.GetRole(987654321)


                // If the role was found within the guild and the user currently has the role assigned, remove the role from the user
                // if (role != Nothing && user.Roles.Any Then Await user.RemoveRoleAsync(role)
            }
        }



    }

}



