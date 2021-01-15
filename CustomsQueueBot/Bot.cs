using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks; //Run Async tasks
using Discord;
using Discord.Commands; //Discord command handler
using Discord.WebSocket; //Discord Web connection
using System.Reflection; //Using external files
using System.Linq;  //Using Lists
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using Discord.Webhook;

namespace CustomsQueueBot
{
    public class Bot
    {
        private DiscordSocketClient Client;    // Socket Client for things
        private CommandService Commands;       // Command Services
        private IServiceProvider Services;     // Interface Service Provider 

        public Bot()
        {
            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug,
                AlwaysDownloadUsers = true,
                MessageCacheSize = 20
            });

            Commands = new CommandService(new CommandServiceConfig
            {
                // Make bot respond to case sensitive commands
                CaseSensitiveCommands = true,
                DefaultRunMode = RunMode.Async,
                LogLevel = LogSeverity.Debug
            });

            Services = BuildServiceProvider();
        }

        public async Task MainAsync()
        {
            //--Initialize CommandManager
            // CommandManager cmdManager = new CommandManager(Services);
            // await cmdManager.InitializeAsync();

            await new CommandHandler(Services).InitializeAsync();             //----Alternitive, easier way to initialize CommandManager.
            await new EventHandler(Services).InitializeAsync();

            Client.Log += Client_Log;
            if (string.IsNullOrWhiteSpace(Config.bot.token))    // Check for bot's token. Same thing as: if (Config.bot.token == "" || Config.bot.token == null) return;
            {
                Console.WriteLine("\n--------**** ERROR ****--------");
                Console.WriteLine("The bot's token is not set. Set the token in the config file located in the \\Resources\\ directory.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Config.bot.prefix))    // Check for bot's prefix.
            {
                Console.WriteLine("\n--------**** ERROR ****--------");
                Console.WriteLine("The bot's prefix is not set. Set the prefix in the config file located in the \\Resources\\ directory.");
                return;
            }
            else
            {
                Console.WriteLine($"{ DateTime.Now} => [Prefix Key] : Set to [{Config.bot.prefix}].");
            }

            await Client.LoginAsync(TokenType.Bot, Config.bot.token);
            await Client.StartAsync();
            await Task.Delay(Timeout.Infinite); //Time for tasks to run. -1 is unlimited time. Timeout.Infinite has clearer intent.

        }

        private Task Client_Log(LogMessage message)
        {
            // Console.WriteLine($"{DateTime.Now} at {message.Source}: {message.Message}");  //Consistancy is important I guess.
            if (message.Message != "Received Dispatch (PRESENCE_UPDATE)")
                if(message.Message != "Received Dispatch (TYPING_START)")
                    if(message.Message != "Received Dispatch (MESSAGE_CREATE)")
                        Console.WriteLine($"{DateTime.Now} => [{message.Source}] : {message.Message}");
            return Task.CompletedTask;
        }

        private ServiceProvider BuildServiceProvider()
        {
            return new ServiceCollection()
                .AddSingleton(Client)
                .AddSingleton(Commands)
                .BuildServiceProvider();
        }

        


    }
}

