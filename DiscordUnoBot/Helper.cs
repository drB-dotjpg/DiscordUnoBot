using System;

namespace DiscordUnoBot
{
    public class Helper
    {
        public enum Phase { Pregame, Ingame, PostGame }

        public enum CardColor { Red, Blue, Yellow, Green, Any }
        public enum CardType { Number, Skip, DrawTwo, Reverse, Wild, WildDrawFour }

        public Card GenerateCard(bool numberOnly = false)
        {
            Random rand = new Random();
            CardType type = !numberOnly ? (CardType)rand.Next(0, 6) : CardType.Number;
            CardColor color = (CardColor)rand.Next(0, 4);
            if (type == CardType.Number)
            {
                return new Card(color, type, rand.Next(0, 10));
            }
            return new Card(color, type);
        }
    }
}
