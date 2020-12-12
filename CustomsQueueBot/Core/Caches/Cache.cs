using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomsQueueBot.Caches
{
    public class IsOpen
    {
        public static bool isOpen { get; set; } = false;
    }

    public class ReactionMessages
    {
        public static ulong ReactionMessage { get; set; } = new ulong();
    }

    public class ReactedUsers
    {
        public static IEnumerable<IUser> ReactionUsersList { get; set; }
        //  public static Lis
    }
}
