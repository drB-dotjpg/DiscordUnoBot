using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordUnoBot
{
    class Game : Helper
    {
        static void Main() => new Game().Login().GetAwaiter().GetResult();

        Phase phase;

        SocketGuild server;
        SocketTextChannel channel;

        DiscordSocketClient _client;
        CommandService _commands;
        IServiceProvider _services;

        public static Card lastPlayedCard;

        List<Player> players = new List<Player>();

        async Task Login()
        {
        login:

            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _services = new ServiceCollection().AddSingleton(_client).AddSingleton(_commands).BuildServiceProvider();

            _client.Log += Log;
            _client.MessageReceived += MessageReceived;

            if (!File.Exists("data.txt"))
            {
                using (StreamWriter file = new StreamWriter("data.txt"))
                {
                    Console.WriteLine("Enter Token: ");
                    file.WriteLine(Console.ReadLine());
                    Console.WriteLine("Enter Server ID: ");
                    file.WriteLine(Console.ReadLine());
                    Console.WriteLine("Enter channel ID");
                    file.WriteLine(Console.ReadLine());
                }
            }

            string[] loginInfo = File.ReadAllLines("data.txt");

            try
            {
                await _client.LoginAsync(TokenType.Bot, loginInfo[0]);
                await _client.StartAsync();
            }
            catch (Exception)
            {
                File.Delete("data.txt");
                goto login;
            }

            phase = Phase.Pregame;
            server = _client.GetGuild(ulong.Parse(loginInfo[1]));
			channel = server.Channels.FirstOrDefault(x => x.Name == "uno") as SocketTextChannel;

            while (true)
            {
                await PreGame();
            }
        }

        async Task PreGame()
        {
            var messages = await channel.GetMessagesAsync().FlattenAsync();
            await channel.DeleteMessagesAsync(messages);

            int seconds = 8;
            var message = channel.SendMessageAsync("DM to join bideo gam");

            do
            {


                seconds--;
            } while (seconds > 0 || players.Count <= 1);
        }

        Task MessageReceived(SocketMessage arg)
        {
            if (arg.Channel is SocketDMChannel && !arg.Author.IsBot && phase == Phase.Pregame)
            {
                players.Add(new Player(arg.Author));
            }

            return Task.CompletedTask;
        }

        Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }
    }
}
