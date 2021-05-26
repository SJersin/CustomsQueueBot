/*
 * A player queue management bot originally designed for KamiVS weekly customs games
 * in the Hi-Rez team based hero shooter game, Paladins Champions of the Realm.
 * Will manage a large group of users in a list style with
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
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CustomsQueueBot
{
    class Program
    {
        static void Main(string[] args)
        {

            Bot bot = new Bot();
            bot.MainAsync().GetAwaiter().GetResult();
        } 
            
        public static void BuildConfig(IConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables();
        }

    }
}
