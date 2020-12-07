using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System.Linq;
using Discord.WebSocket;
using System.Collections;
using Microsoft.Extensions.Logging;

namespace CustomsQueueBot.Core.Commands
{
    public class QueueCommands : ModuleBase<SocketCommandContext>
    {
        //  private readonly ILogger<QueueCommands> logger;

        //  public QueueCommands(ILogger<QueueCommands> _logger) 
        //      => logger = _logger;

        // Double check need for this command:

        [Command("createqueue")]
        [Alias("cq", "+q")]
        [Summary("Create a new queue for users to join.\nIf the player list contains data, will clear the list first.")]
     //   [RequireUserPermission(GuildPermission.Administrator)]
        public async Task NewQueue() //(ISocketMessageChannel channel)
        {
            var leader = Context.Guild.GetUser(Context.User.Id);

            await Context.Channel.TriggerTypingAsync();

            if (!(PlayerList.Playerlist.Count == 0))
                PlayerList.Playerlist.Clear();

            var embed = new EmbedBuilder();

            // Create message with reaction for queue
            embed.WithColor(Color.DarkGreen)
                .WithTitle("React to this post to enter the queue.")
                .WithTimestamp(DateTime.Now);

            var Message = await Context.Channel.SendMessageAsync("New queue coming up!", embed: embed.Build());    // Sends the embed for people to react to.
            var emote = new Emoji("👍");  // Change to Config.bot.reaction

            await Message.AddReactionAsync(emote);
            Caches.ReactionMessages.ReactionMessage = Message.Id;
        }

        [Command("removequeue")]
        [Alias("endqueue", "-q", "closequeue")]
        [Summary("Close off the queue and clear the playerlist")]
    //    [RequireUserPermission(GuildPermission.Administrator)]
        public async Task RemoveQueue()
        {
            await Context.Channel.TriggerTypingAsync();
            PlayerList.Playerlist.Clear();
            //   await Context.Channel.DeleteMessageAsync();     // Delete the message with reacts

            var embed = new EmbedBuilder().WithTitle("Queue Closed:")
                .WithColor(Color.DarkRed)
                .WithDescription("The queue has been closed and the list has been emptied.");

            //await Context.Channel.SendMessageAsync("The queue has been closed and the list has been emptied.");
            await ReplyAsync(embed: embed.Build());
        }

        [Command("playerlist")]
        [Alias("list")]
        [Summary("Shows all players in the playerlist.")]
        public async Task ShowPlayerList()
        {
            await Context.Channel.TriggerTypingAsync();

            Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : Creating EmbedBuilder.");
            string names = "";
            List<EmbedFieldBuilder> PlayerListField = new List<EmbedFieldBuilder>();
            var embed = new EmbedBuilder();

            if (PlayerList.Playerlist.Count == 0)
            {
                embed.WithTitle("The list is Empty.")
                    .WithDescription("Get people in the list!");
            }
            else
            {
                foreach (Player player in PlayerList.Playerlist)
                {
                    names = player.Nickname + "\n";
                    Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : Reading PlayerList: {player.Nickname} was loaded.");

                    EmbedFieldBuilder field = new EmbedFieldBuilder();

                    PlayerListField.Add(field.WithName(names)
                    .WithValue($"Games: {player.GamesPlayed}\nActive: {(player.IsActive ? "Yes" : "No")}")
                    .WithIsInline(true));

                }
                foreach (EmbedFieldBuilder field in PlayerListField)
                    embed.AddField(field);
            }

            // Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : List of names: {names}");
            Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : Firing ReplyAsync Method.");
            await ReplyAsync(embed: embed.Build());
            Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : ReplyAsync Method has fired.");

        }
        [Command("nextgame")]
        [Alias("next")]
        [Summary("Gets and displays [x] number of users for the next lobby\nIf no number is provided, default will be 8.\nA second argument can be passed for the password.\nex: next [x] [password]")]
    //    [RequireUserPermission(GuildPermission.Administrator)]
        public async Task NextGroup(int groupSize = 8, [Remainder] string password = "")
        {
            // Pull the next group from the queue list and displays their names.
            // Maybe have them get PMed the information(?)

            await Context.Channel.TriggerTypingAsync();
            var leader = Context.Guild.GetUser(Context.User.Id);

            // Create and display embed for users selected for next game.
            var embed = new EmbedBuilder().WithTitle($"{leader.Username}'s Next Lobby Group:")
                .WithDescription($"Here is the next {groupSize} players for {leader.Username}'s lobby.")
                .WithColor(Color.DarkGreen);

            List<EmbedFieldBuilder> PlayerListField = new List<EmbedFieldBuilder>();

            int index = 0;
            int ListPos = 0;
            int[] PosArray = new int[groupSize];
            string names = "";

            do
            {
                if (PlayerList.Playerlist[ListPos].IsActive)
                {
                    names = PlayerList.Playerlist[ListPos].Nickname + "\n";
                    Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : Reading PlayerList: {PlayerList.Playerlist[ListPos].Nickname} was loaded.");

                    EmbedFieldBuilder field = new EmbedFieldBuilder();

                    PlayerListField.Add(field.WithName(names)
                        .WithValue(PlayerList.Playerlist[ListPos].GamesPlayed)
                        .WithIsInline(true));

                    PlayerList.Playerlist[index].GamesPlayed += 1;
                    PosArray[index] = ListPos;
                    index++;
                    ListPos++;
                    Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : Reading PlayerList: While loop: Z = {PosArray[index]}.");

                }
                else  //Skip inactive players
                {
                    ListPos++;
                }

            } while (index < groupSize);

                for (index = 0; index < groupSize; index++)
            {
                PlayerList.Playerlist.RemoveAt(PosArray[index]);   // Remove selected users from queue // Update PlayerList
                Console.WriteLine($"{DateTime.Now} => [QUEUE_EVENT: Debug] : Reading PlayerList: {PlayerList.Playerlist[ListPos].Nickname} was removed from the list.");
            }

            foreach (EmbedFieldBuilder field in PlayerListField)
                embed.AddField(field);

            await Context.Channel.SendMessageAsync(embed: embed.Build());


        }

        [Command("removeplayer")]
        [Alias("remove", "rp", "-p")]
        [Summary("Remove a player from the queue.")]
    //    [RequireUserPermission(GuildPermission.Administrator)]  // This replaces a block of code commented in this command
        public async Task RemovePlayer(string user, [Remainder] string reason = "")
        {


            await Context.Channel.TriggerTypingAsync();

            var _user = Context.Guild.GetUser(Context.Message.MentionedUsers.First().Id);
            var embed = new EmbedBuilder()
                .WithTitle("Player Removed:");

            // Search user list for username
            foreach (Player player in PlayerList.Playerlist)
            {
                if (player.DiscordID == _user.Id)   // Remove user from list
                {
                    PlayerList.Playerlist.Remove(player);

                    embed.WithDescription($"{_user.Username} has been removed from the queue.\n{reason}")
                        .WithColor(Color.DarkRed);
                }
            }
            await ReplyAsync(embed: embed.Build());

        }

        [Command("addplayer")]
        [Alias("ap", "+p")]
        [Summary("Insert a player into a specific spot in the queue.\nDefaults to the 1st element (front of queue).")]
    //    [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddPlayer(string user, int position = 0)
        {
            await Context.Channel.TriggerTypingAsync();
            var _user = Context.Guild.GetUser(Context.Message.MentionedUsers.First().Id);

            if (_user == null)
            {
                await Context.Channel.SendMessageAsync($"User was not found.");
                return;
            }

            foreach (Player player in PlayerList.Playerlist)
            {
                if (player.DiscordID == _user.Id)
                {
                    await Context.Channel.SendMessageAsync($"{player.Nickname} is already in the list at number {(PlayerList.Playerlist.IndexOf(player) + 1)}.");
                    return;
                }
            }

            Player player1 = new Player();
            player1.Nickname = _user.Username;
            player1.DiscordID = _user.Id;
            player1.IsActive = true;

            PlayerList.Playerlist.Insert(position, player1);
            await Context.Channel.SendMessageAsync($"{player1.Nickname} has been added to the list.");
        }



        [Command("test")]
        public async Task EmbedTesting()
        {
            var exampleAuthor = new EmbedAuthorBuilder()
            .WithName("I am a bot")
            .WithIconUrl("https://discordapp.com/assets/e05ead6e6ebc08df9291738d0aa6986d.png");

            var exampleFooter = new EmbedFooterBuilder()
                    .WithText("I am a nice footer")
                    .WithIconUrl("https://discordapp.com/assets/28174a34e77bb5e5310ced9f95cb480b.png");

            var exampleField = new EmbedFieldBuilder()
                    .WithName("Title of Another Field")
                    .WithValue("I am an [example](https://example.com).")
                    .WithIsInline(true);

            var otherField = new EmbedFieldBuilder()
                    .WithName("Title of a Field")
                    .WithValue("Notice how I'm inline with that other field next to me.")
                    .WithIsInline(true);

            var embed = new EmbedBuilder()
                    .AddField(exampleField)
                    .AddField(otherField)
                    .WithAuthor(exampleAuthor)
                    .WithFooter(exampleFooter)
                    .Build();

            await ReplyAsync(embed: embed);
        }

        [Command("test2")]
        public async Task EmoteTesting(ISocketMessageChannel channel)
        {

            var message = await channel.SendMessageAsync("I am a message.");

            // Creates a Unicode-based emoji based on the Unicode string
            // This is effectively the same as new Emoji("💕
            var heartEmoji = new Emoji("\U0001f495");
            // Reacts to the message with the Emoji.            
            await message.AddReactionAsync(heartEmoji);

            // Parses a custom emote based on the provided Discord emote format.            
            // Please note that this does not guarantee the existence of            
            // the emote.
            var emote = Emote.Parse("<:thonkang:282745590985523200>");
            // Reacts to the message with the Emote.           
            await message.AddReactionAsync(emote);

        }

        [Command("testembed")]
        public async Task TestEmbed2()
        {

        }
    }
}

/* // -- This block of code is replaced by the RequiredUserPermission(GuildPermission.Administrator) --
 * 
 *   var leader = Context.Guild.GetUser(Context.User.Id);
 *   if (!leader.GuildPermissions.Administrator)
 *   {
 *       await Context.Channel.SendMessageAsync("You are not authorized to use this command.");
 *       return;
 *   }
 */
