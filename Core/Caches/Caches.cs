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
        public static IMessage ListCommandMessage2 { get; set; } // This is literally for the list command message. wtf was I thinking?? I wasn't high enough for this, clearly. Anyways, double check that I have properly deleted the messages/embeds BEFORE posting new ones. If you have done this, thank you and you may delete most of this stupid comment. Or leave the rest of it here so other people can wonder why they've kept on reading this far. If you're using word wrap, you're a cheater.
    }

    // public class DefaultResponses 
}
