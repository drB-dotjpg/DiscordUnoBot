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
        Discord.Rest.RestUserMessage message;

        DiscordSocketClient _client;
        CommandService _commands;
        IServiceProvider _services;

        public static Card lastCard; //the card at the top of the stack

        bool loggedIn = false;

        int turn = 1; //game turn number
        int playerTurnIndex = 0;
        bool reverse = false;
        int turnMultiplier = 1;
        int drawMultiplier = 0;

        Player winningPlayer;

        bool nextTurnFlag = false;

        int timeForTurn = 60; //Seconds to play/draw until your turn is skipped and you are forced to draw a card

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

            int minutes = 3;
            int seconds = 0;

            const string messageContent = "**DM to join bideo gam**";
            message = await channel.SendMessageAsync(messageContent, false, new EmbedBuilder().WithFooter("Created by Jeff and .jpg.\nView our code here: https://github.com/drB-dotjpg/DiscordUnoBot").Build());

            do
            {
                bool twoOrMore = players.Count >= 2;

                string time = twoOrMore ? $"`{minutes.ToString("00")}:{seconds.ToString("00")}`" : "`Waiting for two or more players`";

                string playersDisplay = "";
                foreach (Player player in players) playersDisplay += "`" + player.name + "` ";
                playersDisplay = players.Count != 0 ? playersDisplay.Trim() : "`No players`";

                await message.ModifyAsync(x => x.Content = $"{messageContent}\n" +
                $"> Time remaining: {time}\n" +
                $"> Players: {playersDisplay}");

                if (twoOrMore)
                    seconds--;
                if (seconds < 0 && minutes > 0)
                {
                    minutes--;
                    seconds += 59;
                }
                await Task.Delay(1000);
            } while (seconds > 0 || minutes > 0);

            foreach (Player player in players)
            {
                for (int i = 0; i < 6; i++)
                {
                    player.Cards.Add(GenerateCard());
                }
            }
            Shuffle(players);
            await InGame();
        }

        async Task InGame()
        {
            phase = Phase.Ingame;
            lastCard = GenerateCard(true);

            while (true)
            {
                await SendTurnsToPlayersAsync();
                await message.ModifyAsync(x => x.Content = "Current Game");
                await message.ModifyAsync(x => x.Embed = GetTurnBriefing(null, true, true, false).Build());

                string drawNotif = drawMultiplier > 0 ? $"\nYou will draw {drawMultiplier} at the end of the turn, unless you stack." : "";

                await AlertPlayerAsync(GetCurrentTurnOrderPlayer(), "Its your turn! Select a card to play or draw a card.", $"You have {timeForTurn} seconds to play a card.{drawNotif}");

                int turnTimer = 0;
                while (!nextTurnFlag && turnTimer < timeForTurn)
                {
                    await Task.Delay(1000);
                    turnTimer++;

                    if (timeForTurn - turnTimer == (int)(timeForTurn / 3))
                        await AlertPlayerAsync(GetCurrentTurnOrderPlayer(), $"You have {timeForTurn - turnTimer} seconds to play a card!");
                }

                if (!nextTurnFlag)
                    await AlertPlayerAsync(GetCurrentTurnOrderPlayer(), "You ran out of time!", "Starting next turn");

                if (GetCurrentTurnOrderPlayer().Cards.Count == 0) //if the player has 0 cards
                {
                    winningPlayer = GetCurrentTurnOrderPlayer();
                    await PostGame();
                    break;
                }
                if (players.Count < 2) //if there are less than 2 players (ie: someone left the game)
                {
                    winningPlayer = players.FirstOrDefault();
                    await PostGame();
                    break;
                }

                StartNextTurn();

                turnMultiplier = 1;
                turn++;
                nextTurnFlag = false;
            }
        }

        async Task PostGame()
        {
            phase = Phase.PostGame;

            foreach (Player player in players)
            {
                if (player == winningPlayer)
                {
                    await AlertPlayerAsync(player, "You have won! The game is now over.");
                }
                else
                {
                    await AlertPlayerAsync(player, $"{winningPlayer.name} has won the game! The game is now over.");
                }
            }

            //int postGameSeconds = 60;
            //await message.ModifyAsync(x => x.Embed = null);
            //do
            //{
            //    await Task.Delay(1000);
            //    postGameSeconds--;
            //    await message.ModifyAsync(x => x.Content = $"{winningPlayer.name} has won this round!\n> `0:{postGameSeconds.ToString("00")}` until next round.");
            //} while (postGameSeconds > 0);

			Environment.Exit(1);
            //Restart();

            //await PreGame();
        }

        async Task SendTurnsToPlayersAsync()
        {
            foreach (Player player in players)
            {
                EmbedBuilder builder = GetTurnBriefing(player);
                await (await player.thisUser.GetOrCreateDMChannelAsync() as SocketDMChannel).SendMessageAsync(null, false, builder.Build());
            }
        }

        EmbedBuilder GetTurnBriefing(Player player, bool withCurrentCard = true, bool withOtherPlayers = true, bool withYourHand = true)
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
                builder.AddField("CURRENT CARD", $":{lastCard.color.ToString().ToLower()}_circle: {CardToString(lastCard)}");
            }

            if (withOtherPlayers)
            {
                foreach (Player otherPlayer in players)
                {
                    bool isOtherPlayerTurn = GetCurrentTurnOrderPlayer().Equals(otherPlayer);
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
                    hand += IsCardCompatable(card)
                        ? $"***{index}*** : **{CardToString(card)}**\n"
                        : $"*{index}* : {CardToString(card)}\n";
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
                            await DM.SendMessageAsync("You have joined this round of UNO!\n" +
                                "\n> __**How to play:**__" +
                                "\n> `play (card number)` to play a card." +
                                "\n> `draw` to draw a new card." +
                                "\n> `leave` to leave the game.");
                        }
                        else if (arg.Content.StartsWith("leave"))
                        {
                            await HandleLeave(arg);
                            await DM.SendMessageAsync("You have left this round");
                        }
                        break;

                    case Phase.Ingame:
                        if (IsPlayerInGame(arg.Author) && arg.Author == GetCurrentTurnOrderPlayer().thisUser)
                        {
                            Player user = GetPlayerObject(arg.Author);

                            if (arg.Content.StartsWith("play"))
                            {
                                await HandleCardPlay(arg, user);
                            }

                            if (arg.Content.StartsWith("draw"))
                            {
                                await HandleCardDraw(arg, DM);
                            }

                            if (arg.Content.StartsWith("leave"))
                            {
                                await HandleLeave(arg);
                            }
                        }
                        else if (arg.Content.StartsWith("leave"))
                        {
                            await HandleLeave(arg);
                        }
                        else
                        {
                            await DM.SendMessageAsync("Its not your turn yet!");
                        }
                        break;
                }
            }
        }

        async Task HandleCardDraw(SocketMessage arg, SocketDMChannel DM)
        {
            Player user = GetPlayerObject(arg.Author);

            if (arg.Content.StartsWith("play"))
            {
                await HandleCardPlay(arg, user);
            }

            if (arg.Content.StartsWith("draw"))
            {
                bool hasPlayableCards = false;
                foreach (Card card in GetCurrentTurnOrderPlayer().Cards)
                {
                    if (IsCardCompatable(card))
                    {
                        hasPlayableCards = true;
                        break;
                    }
                }
                if (!hasPlayableCards)
                {
                    List<Card> drewCards = new List<Card>();

                    if (drawMultiplier == 0) drawMultiplier++;
                    while (drawMultiplier > 0)
                    {
                        drewCards.Add(GetCurrentTurnOrderPlayer().DrawCard());
                        drawMultiplier--;
                    }

                    string playableAlert = IsCardCompatable(drewCards[0]) && drewCards.Count == 1 ? "You can play this card." : null;

                    string cardNames = "";
                    foreach (Card card in drewCards)
                    {
                        if (cardNames != "") cardNames += ", ";
                        cardNames += CardToString(card);
                    }

                    await AlertPlayerAsync(user, $"You drew a {cardNames}.", playableAlert);
                    await DM.SendMessageAsync(null, false, GetTurnBriefing(GetCurrentTurnOrderPlayer(), true, false).Build());

                    foreach (Player player in players)
                    {
                        if (player != user)
                            await AlertPlayerAsync(player, $"{user.name} drew {(drewCards.Count == 1 ? "a card" : drewCards.Count + " cards")}", $"{user.name} now has {user.Cards.Count} cards.");
                    }
                    if (!IsCardCompatable(drewCards[0]) || drewCards.Count > 1)
                    {
                        nextTurnFlag = true;
                    }
                }
                else
                {
                    await AlertPlayerAsync(GetCurrentTurnOrderPlayer(), "You have playable cards so you cannot draw!");
                }
            }
        }

        async Task HandleLeave(SocketMessage arg)
        {
            string playerName = arg.Author.Username;
            Player leavingPlayer = null;

            foreach(Player player in players.ToList())
            {
                if (phase == Phase.Ingame && players.Count > 0) await AlertPlayerAsync(player, $"{playerName} has left the game.");

                if (player.thisUser == arg.Author)
                {
                    if (player == GetCurrentTurnOrderPlayer())
                    {
                        nextTurnFlag = true;
                    }

                    leavingPlayer = player;
                }
            }

            players.Remove(leavingPlayer);
        }

        async Task HandleCardPlay(SocketMessage arg, Player user)
        {
            //if the number isn't a number or it out of range goto else statement
            if (int.TryParse(arg.Content.Split()[1], out int index) && index > 0 && index < user.Cards.Count + 1)
            {
                index--;

                Card pickedCard = user.Cards[index];

                if (!IsCardCompatable(pickedCard))
                {
                    await AlertPlayerAsync(user, "Invalid card!", "Card must be the same color, type, or number");
                    return;
                }
                else if (pickedCard.type == CardType.Wild || pickedCard.type == CardType.WildDrawFour)
                {
                    bool validColor = false;
                    string[] colors = { "red", "blue", "yellow", "green" };
                    foreach (string color in colors)
                    {
                        try
                        {
                            if (arg.Content.Split()[2] == color)
                            {
                                validColor = true;
                                break;
                            }
                        }
                        catch (IndexOutOfRangeException)
                        {
                            break;
                        }
                    }
                    if (!validColor)
                    {
                        await AlertPlayerAsync(user, "Invalid wild color!", "Enter the name of color at the end of your message!");
                        return;
                    }
                }

                int prevDrawMulti = drawMultiplier;

                switch (pickedCard.type)
                {
                    case CardType.Reverse:
                        reverse = !reverse;
                        break;

                    case CardType.Skip:
                        turnMultiplier++;
                        break;

                    case CardType.DrawTwo:
                        drawMultiplier += 2;
                        break;

                    case CardType.Wild:
                        pickedCard = new Card(GetColorFromString(arg.Content.Split()[2].ToLower()), pickedCard.type, pickedCard.value);
                        break;

                    case CardType.WildDrawFour:
                        drawMultiplier += 4;
                        goto case CardType.Wild;
                }

                lastCard = pickedCard;
                int cardCount = user.Cards.Count - 1;

                if (prevDrawMulti > 0 && lastCard.type != CardType.DrawTwo && lastCard.type != CardType.WildDrawFour) //account for draw 2 and draw 4
                {
                    cardCount += prevDrawMulti;
                }

                foreach (Player player in players)
                {
                    await AlertPlayerAsync(player, $"{user.name} played a {CardToString(pickedCard)}", $"{user.name} now has {cardCount} cards.");
                    if (user == player)
                    {
                        player.Cards.RemoveAt(index);
                    }
                }
                nextTurnFlag = true;
            }
            else
            {
                await AlertPlayerAsync(user, "Invalid choice");
            }
        }

        bool IsCardCompatable(Card card)
        {
            if (drawMultiplier <= 0) //players that have to draw cards can only play draw2 or wilddraw4 cards to stack it
            {
                if (card.color == CardColor.Any || card.color == lastCard.color) //check colors
                {
                    return true;
                }
                else if (card.type == lastCard.type && card.type != CardType.Number) //check types
                {
                    return true;
                }
                else if (card.type == CardType.Number && card.value == lastCard.value && card.value != 0) //check numbers
                {
                    return true;
                }
                return false;
            }
            /*
            else if (card.type == CardType.DrawTwo || card.type == CardType.WildDrawFour || card.type == CardType.Skip)
            {
                if (card.color == CardColor.Any || card.color == lastCard.color) //check colors
                {
                    return true;
                }
                else if (card.type == lastCard.type && card.type != CardType.Number) //check types
                {
                    return true;
                }
            }
            */
            return false;
        }

        bool IsPlayerInGame(SocketUser user)
        {
            foreach (Player player in players)
                if (player.thisUser == user)
                    return true;
            return false;
        }

        Player GetCurrentTurnOrderPlayer()
        {
            return players[playerTurnIndex];
        }

        async Task AlertPlayerAsync(Player player, string text, string footer = null)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle(text);
            if (footer != null) builder.WithFooter(footer);
            builder.WithColor(Color.LightGrey);
            await (await player.thisUser.GetOrCreateDMChannelAsync() as SocketDMChannel).SendMessageAsync("", false, builder.Build());
        }

        Player GetPlayerObject(SocketUser search)
        {
            foreach (Player player in players)
            {
                if (player.thisUser.Equals(search)) return player;
            }
            return null;
        }

        void StartNextTurn()
        {
            while (turnMultiplier > 0) //accounts for skips
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

                turnMultiplier--;
            }

            turnMultiplier = 1;
            nextTurnFlag = true;
        }

        void Restart()
        {
            turn = 1;
            playerTurnIndex = 0;
            reverse = false;
            turnMultiplier = 1;
            drawMultiplier = 0;

            nextTurnFlag = false;

            players.Clear();

            lastCard = null;
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
