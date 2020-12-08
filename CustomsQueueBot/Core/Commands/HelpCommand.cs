using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;

namespace CustomsQueueBot.Core.Commands
{
    public class HelpCommand : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _Service;
        public HelpCommand(CommandService Service)
        {
            _Service = Service;
        }

        [Command("help")]
        [Alias("command", "commands")]
        [Summary("Displays all of the bot's commands, or displays info about a specific command.")]
        public async Task Help([Remainder] string command = "")
        {
            string prefix = Config.bot.prefix; //Set the bot prefix string to a variable
            if (command == "")
            {
                var embed = new EmbedBuilder()
                {
                    Color = new Color(004, 137, 218),   //random colors for funsies.
                    Description = "Here are the commands. Use help [command] for more information on the command."
                };

                foreach (var module in _Service.Modules)
                {
                    string description = "";
                    foreach (var cmd in module.Commands)
                    {
                        description += $"{prefix}{cmd.Aliases.First()}\n";
                    }

                    // If there is a description, populate the values.
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        string name = module.Name;
                        embed.AddField(x =>
                        {
                            x.Name = name;
                            x.Value = description;
                            x.IsInline = false; // Renders the embed containers (or elements/fields) vertically or horizontally. True = Horizontal | False = Vertical
                        });
                    }
                }
                await ReplyAsync(embed: embed.Build());

            }
            else
            {
                var result = _Service.Search(Context, command);

                if (!result.IsSuccess) // If the command is not found
                {
                    await ReplyAsync($"Sorry, command **{command}** could not be found.");
                    return;
                }

                var embed = new EmbedBuilder()
                {
                    Color = new Color(114, 137, 218),
                    Description = $"Here are commands like **{command}**"
                };

                foreach (var match in result.Commands)
                {
                    var cmd = match.Command;
                    embed.AddField(x =>
                    {
                        x.Name = string.Join(", ", cmd.Aliases);
                        x.Value = $"Parameters {string.Join(", ", cmd.Parameters.Select(p => p.Name))}\n" +
                        $"Summary {cmd.Summary}";
                        x.IsInline = false;
                    });
                }

                await ReplyAsync(embed: embed.Build());
            }
        }
    }
}
