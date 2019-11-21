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
        bool loggedIn = false;

        List<Player> players = new List<Player>();

        async Task Login()
        {

            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _services = new ServiceCollection().AddSingleton(_client).AddSingleton(_commands).BuildServiceProvider();

            _client.Log += Log;
            _client.MessageReceived += MessageReceived;
            _client.Ready += OnReady;

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
                while (!loggedIn) await Task.Delay(100);
            }
            catch (Exception)
            {
                File.Delete("data.txt");
                await Login();
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

            int minutes = 3;
            int seconds = 8;

            const string messageContent = "**DM to join bideo gam**";
            var message = await channel.SendMessageAsync(messageContent);

            do
            {
                bool twoOrMore = players.Count >= 2;

                string time = twoOrMore ? $"`{minutes.ToString("00")}:{seconds.ToString("00")}`" : "`Waiting for two or more players`";

                string playersDisplay = "";
                foreach (Player player in players) playersDisplay += "`" + player.name + "` ";
                playersDisplay = players.Count != 0 ? playersDisplay.Trim() : "`No players`";

                await message.ModifyAsync(x => x.Content = $"{messageContent}\nTime remaining: {time}\nPlayers: {playersDisplay}");

                //if (twoOrMore)
                    seconds--;
                if (seconds < 0 && minutes > 0)
                {
                    minutes--;
                    seconds += 59;
                }
                await Task.Delay(1000);
            } while (seconds > 0);
        }

        async Task MessageReceived(SocketMessage arg)
        {
            if (arg.Channel is SocketDMChannel && !arg.Author.IsBot)
            {
                if (phase == Phase.Pregame)
                {
                    if (!IsPlayerInGame(arg.Author))
                    {
                        players.Add(new Player(arg.Author));
                        var DM = await arg.Author.GetOrCreateDMChannelAsync() as SocketDMChannel;
                        await DM.SendMessageAsync("You have joined this round of UNO!");
                    }
                }
            }
        }

        bool IsPlayerInGame(SocketUser user)
        {
            foreach (Player player in players)
                if (player.thisUser == user)
                    return true;
            return false;
        }

        Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        Task OnReady()
        {
            loggedIn = true;
            return Task.CompletedTask;
        }
    }
}
