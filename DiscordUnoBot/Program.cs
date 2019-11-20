using Discord.WebSocket;
using System;
using System.Collections.Generic;

namespace DiscordUnoBot
{
    class Program
    {
        enum CardColor { Red, Blue, Yellow, Green, All }
        enum CardType { Number, Skip, DrawTwo, Reverse, Wild, WildDrawFour }

        class Player
        {
            public SocketUser thisUser { get; }
            public string name { get; }
            public List<Card> cards { get; set; } = new List<Card>();

            public Player(SocketUser user)
            {
                thisUser = user;
                name = user.Username;
            }
        }
        class Card
        {
            public CardColor color { get; }
            public CardType type { get; }

            public Card(CardColor color, CardType type)
            {
                this.color = color;
                this.type = type;
            }
        }
        class Game
        {

        }
    }
}
