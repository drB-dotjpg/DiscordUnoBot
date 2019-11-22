using System;
using System.Collections.Generic;

namespace DiscordUnoBot
{
    public class Helper
    {
        public enum Phase { Pregame, Ingame, PostGame }

        public enum CardColor { Red, Blue, Yellow, Green, Any }
        public enum CardType { Number, Skip, DrawTwo, Reverse, Wild, WildDrawFour }

		private static Random rng = new Random();

        public Card GenerateCard(bool numberOnly = false)
        {
            Random rand = new Random();

            if (!numberOnly && rand.Next(10) < 7)
            {
                numberOnly = true;
            }

            CardType type = !numberOnly ? (CardType)rand.Next(0, 6) : CardType.Number;
            CardColor color = (int)type < 4 ? (CardColor)rand.Next(0, 4) : CardColor.Any;

            if (type == CardType.Number)
            {
                return new Card(color, type, rand.Next(1, 10));
            }

            return new Card(color, type);
        }

        public string CardToString(Card card)
        {
            string output = "";

            if (card.color == CardColor.Any)
            {
                output = card.type.ToString();
            }
            else if (card.type == CardType.Number)
            {
                output = card.color.ToString() + " " + card.value;
            }
            else
            {
                output = card.color.ToString() + " " + card.type.ToString();
            }

            output = output.Replace("DrawTwo", "Draw Two");
            output = output.Replace("WildDrawFour", "Wild Draw Four");

            return output;
        }

		public void Shuffle(List<Player> list)
		{
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = rng.Next(n + 1);
				Player value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}
	}
}
