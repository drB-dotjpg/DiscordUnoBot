using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordUnoBot
{
    public class Card : Helper
    {
        public CardColor color { get; }
        public CardType type { get; }

        public Card(CardColor color, CardType type)
        {
            this.color = color;
            this.type = type;
        }
    }
}
