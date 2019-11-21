using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnoBot
{
    class Game : Helper
    {
        static void Main() => new Game().Login().GetAwaiter().GetResult();
        //public Game() => new Game();

        Phase phase;

        SocketGuild server;
        SocketTextChannel channel;

        DiscordSocketClient _client;
        CommandService _commands;
        IServiceProvider _services;

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
                    file.WriteLine();
                }
            }

            try
            {
                await _client.LoginAsync(TokenType.Bot, "");
                await _client.StartAsync();
            }
            catch (Discord.Net.HttpException)
            {
                File.Delete("data.txt");
                goto login;
            }

            phase = Phase.Pregame;
        }

        Task MessageReceived(SocketMessage arg)
        {
            throw new NotImplementedException();
        }

        Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }
    }
}
