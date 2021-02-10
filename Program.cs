using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Discord.WebSocket;
using Discord;
using Discord.Commands;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Discord.Rest;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using System.Net.NetworkInformation;
using System.Net;
using DiscordTagger.Core;

namespace DiscordTagger
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Run(args);
        }

        public static async void Run(string[] args)
        {
            new Program().MainAsync();
            KeepAlivePinger();
            CreateHostBuilder(args).Build().Run();
        }

        private async static Task KeepAlivePinger()
        {
            while(true)
            {
                await Task.Delay(10 * 60 * 1000);

                using (WebClient client = new WebClient())
                {
                    client.DownloadString("https://discordagger3110.herokuapp.com/");
                }
            }
           
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        public async Task MainAsync()
        {
            DiscordTagBot tagBot = new DiscordTagBot();
            await tagBot.Run();
        }
    }
}
