using System;

namespace DiscordUnoBot
{
    public class Helper
    {
        public enum Phase { Pregame, Ingame, PostGame }

        public enum CardColor { Red, Blue, Yellow, Green, All }
        public enum CardType { Number, Skip, DrawTwo, Reverse, Wild, WildDrawFour }

        public Card GenerateCard()
        {
            Random rand = new Random();
            CardType type = (CardType)rand.Next(0, 6);
            CardColor color = (int)type >= 4 ? CardColor.All : (CardColor)rand.Next(0, 4);
            return new Card(color, type);
        }
    }
}
