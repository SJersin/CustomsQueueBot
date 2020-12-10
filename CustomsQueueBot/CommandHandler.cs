using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace CustomsQueueBot
{
    class CommandHandler
    {
        private readonly DiscordSocketClient Client;
        private readonly CommandService Commands;
        private readonly IServiceProvider _Services;

        // Constructor
        //  public CommandManager(DiscordSocketClient Client, CommandService Commands) Passing the client socket and command service without using IServiceProvider
        public CommandHandler(IServiceProvider Services)
        {
            //_Client = Client;
            Client = Services.GetRequiredService<DiscordSocketClient>();
            //_Commands = Commands;
            Commands = Services.GetRequiredService<CommandService>();
            _Services = Services;
        }

        public async Task InitializeAsync()
        {
            await Commands.AddModulesAsync(Assembly.GetEntryAssembly(), _Services); //Second arguement set IServices. Use null if not using an IService.
            foreach (var cmd in Commands.Commands)
                Console.WriteLine($"{DateTime.Now} => [COMMANDS] : {cmd.Name} was loaded.");

            Commands.Log += Command_Log;
        }

        private Task Command_Log(LogMessage Command)
        {
            Console.WriteLine(Command.Message);
            return Task.CompletedTask;
        }
    }
}
