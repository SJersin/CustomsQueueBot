using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomsQueueBot.Caches
{
    public class IsOpen
    {
        public static bool isOpen { get; set; } = false;
    }

    public class Messages
    {
        public static IChannel ReactionMessageChannel { get; set; }
        public static IMessage ReactionMessage { get; set; }
        public static IUserMessage PlayerListEmbed { get; set; }
        public static List<IMessage> LobbyMessages { get; set; } = new List<IMessage>();
    }

   // public class DefaultResponses { }
}