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

        public static Card lastCard; //the card at the top of the stack

        bool loggedIn = false;

        int turn = 1; //game turn number
        int playerTurnIndex = 0;
        bool reverse = false;

        bool nextTurnFlag = false;

        int timeForTurn = 30; //Seconds to play/draw until your turn is skipped and you are forced to draw a card

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

            await PreGame();
        }

        async Task PreGame()
        {
            var messages = await channel.GetMessagesAsync().FlattenAsync();
            await channel.DeleteMessagesAsync(messages);

            int minutes = 0;
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

            foreach (Player player in players)
            {
                for (int i = 0; i < 6; i++)
                {
                    player.Cards.Add(GenerateCard());
                }
            }

            await message.DeleteAsync();

            Shuffle(players);
            await InGame();
        }

        async Task InGame()
        {
            phase = Phase.Ingame;
            lastCard = GenerateCard(true);

            while (true)
            {
                await SendTurnsToPlayers();
                await AlertPlayerTurn(GetCurrentTurnOrderPlayer());

                int turnTimer = 0;
                while (!nextTurnFlag && turnTimer < timeForTurn)
                {
                    await Task.Delay(1000);
                    turnTimer++;
                }
                StartNextTurn();

                turn++;
                nextTurnFlag = false;
            }
        }

        async Task SendTurnsToPlayers()
        {
            foreach (Player player in players)
            {
                EmbedBuilder builder = GetTurnBreifing(player);
                await (await player.thisUser.GetOrCreateDMChannelAsync() as SocketDMChannel).SendMessageAsync(null, false, builder.Build());
            }
        }

        EmbedBuilder GetTurnBreifing(Player player, bool withCurrentCard = true, bool withOtherPlayers = true, bool withYourHand = true)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle("Turn " + turn.ToString());

            switch (lastCard.color)
            {
                case CardColor.Red:
                    builder.WithColor(Color.Red); break;
                case CardColor.Blue:
                    builder.WithColor(Color.Blue); break;
                case CardColor.Yellow:
                    builder.WithColor(Color.Gold); break;
                case CardColor.Green:
                    builder.WithColor(Color.Green); break;
            }

            if (withCurrentCard)
            {
                builder.AddField("CURRENT CARD", CardToString(lastCard));
            }

            if (withOtherPlayers)
            {
                foreach (Player otherPlayer in players)
                {
                    bool isOtherPlayerTurn = GetCurrentTurnOrderPlayer().thisUser.Equals(otherPlayer);
                    string nameDisplay = !isOtherPlayerTurn ? otherPlayer.name : "👉 " + otherPlayer.name;

                    builder.AddField(nameDisplay, $"Cards: {otherPlayer.Cards.Count}", true);
                }
            }

            if (withYourHand)
            {
                int index = 1;
                string hand = "";
                foreach (Card card in player.Cards)
                {
                    hand += $"*{index}* : {CardToString(card)}\n";
                    index++;
                }

                builder.AddField("Your hand", hand);
            }

            return builder;
        }

        async Task MessageReceived(SocketMessage arg)
        {
            if (arg.Channel is SocketDMChannel && !arg.Author.IsBot)
            {
                var DM = await arg.Author.GetOrCreateDMChannelAsync() as SocketDMChannel;
                switch (phase)
                {
                    case Phase.Pregame:
                        if (!IsPlayerInGame(arg.Author))
                        {
                            players.Add(new Player(arg.Author));
                            await DM.SendMessageAsync("You have joined this round of UNO!");
                        }
                        break;

                    case Phase.Ingame:
                        if (arg.Author == GetCurrentTurnOrderPlayer().thisUser)
                        {
                            if (arg.Content.StartsWith("play"))
                            {
                                //figure out later
                            }
                            if (arg.Content.StartsWith("draw"))
                            {
                                GetCurrentTurnOrderPlayer().DrawCard();
                                await DM.SendMessageAsync(null, false, GetTurnBreifing(GetCurrentTurnOrderPlayer(), false, false).Build());
                            }
                        }
                        else
                        {
                            await DM.SendMessageAsync("Its not your turn yet!");
                        }
                        break;
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

        bool HaveAllPlayersPlayed() //not needed?????
        {
            foreach (Player player in players)
            {
                if (!player.hasGoneThisTurn)
                {
                    return false;
                }
            }

            return true;
        }

        Player GetCurrentTurnOrderPlayer()
        {
            return players[playerTurnIndex];
        }

        async Task AlertPlayerTurn(Player player)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle("It's your turn! Select a card to play or draw a card.");
            builder.WithFooter($"You have {timeForTurn} seconds to play a card.");
            builder.WithColor(Color.LightGrey);
            await (await player.thisUser.GetOrCreateDMChannelAsync() as SocketDMChannel).SendMessageAsync("", false, builder.Build());
        }

        void StartNextTurn()
        {
            playerTurnIndex += !reverse ? 1 : -1;

            if (playerTurnIndex > players.Count - 1)
            {
                playerTurnIndex = 0;
            }
            else if (playerTurnIndex < 0)
            {
                playerTurnIndex = players.Count - 1;
            }

            nextTurnFlag = true;
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
