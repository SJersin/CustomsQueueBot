using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using Discord.Rest;

namespace CustomsQueueBot.Caches
{
    public class Lobby
    {
        public static bool IsOpen { get; set; } = false;
    }

    public struct Messages
    {
        public static IChannel ReactionMessageChannel { get; set; }
        public static IMessage ReactionMessage { get; set; }
        public static IUserMessage PlayerListEmbed { get; set; }
        public static IMessage LeastCommandMessage { get; set; }
        public static List<IMessage> LobbyMessages { get; set; } = new List<IMessage>();
        public static IMessage ListCommandMessage { get; set; }
        public static RestUserMessage ListCommandMessage2 { get; internal set; }
    }

    // public class DefaultResponses 
}
