using System;
using System.Threading.Tasks; //Run Async tasks
using Discord;
using Discord.Commands; //Discord command handler
using Discord.WebSocket; //Discord Web connection
using System.Reflection; //Using external files
using System.Linq;  //Using Lists
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using Discord.Webhook;
using Serilog;

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
                MessageCacheSize = 100
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
            if (string.IsNullOrWhiteSpace(Config.bot.Token))    // Check for bot's token. Same thing as: if (Config.bot.token == "" || Config.bot.token == null) return;
            {
                Log.Information("\n--------**** ERROR ****--------");
                Log.Information("The bot's token is not set. Set the token in the config file located in the \\Resources\\ directory.");
                Console.Read();
                return;
            }

            if (string.IsNullOrWhiteSpace(Config.bot.Prefix))    // Check for bot's prefix.
            {
                Log.Information("\n--------**** ERROR ****--------");
                Log.Information("The bot's prefix is not set. Set the prefix in the config file located in the \\Resources\\ directory.");
                Console.Read();
                return;
            }
            else
            {
                Console.WriteLine($"{ DateTime.Now} => [Prefix Key] : Set to [{Config.bot.Prefix}].");
            }

            await Client.LoginAsync(TokenType.Bot, Config.bot.Token);
            await Client.StartAsync();
            await Task.Delay(Timeout.Infinite); //Time for tasks to run. -1 is unlimited time. Timeout.Infinite has clearer intent.

        }

        private Task Client_Log(LogMessage message)
        {
            // Console.WriteLine($"{DateTime.Now} at {message.Source}: {message.Message}");  //Consistancy is important I guess.

            // Filter out annoying repetative messages.
            if (message.Message != "Received Dispatch (PRESENCE_UPDATE)")
                if (message.Message != "Received Dispatch (TYPING_START)")
                    if (message.Message != "Received Dispatch (MESSAGE_CREATE)")    // Too lazy for &&
                        if (message.Message != "Received HeartbeatAck")
                            if (message.Message != "Sent Heartbeat")
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

