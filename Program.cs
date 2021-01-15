/*
 * A player queue management bot originally designed for KamiVS weekly customs games
 * in the Hi-Rez team based hero shooter game, Paladins Champions of the Realm.
 * Will manage a large group of users in a first come, first serve list style with
 * many other functions to pull however many players you need for the next game lobby.
 * 
 * Has been designed so that arguments can be passed to accomidate other games such as
 * Overwatch, CS:GO, Call of Duty, or pretty much any first person shooter game that
 * has custom matches that can be made private.
 * 
 * Do not forget to configure the bot in the \Resources\config.json file!
 * 
 * By: Jersin - 12 DEC 2020
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace CustomsQueueBot
{
    class Program
    {
        static void Main(string[] args)
            => new Bot().MainAsync().GetAwaiter().GetResult();
    }
}
