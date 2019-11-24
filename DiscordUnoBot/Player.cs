using Discord.WebSocket;
using System.Collections.Generic;

namespace DiscordUnoBot
{
    public class Player : Helper
    {
        public SocketUser thisUser { get; }
        public string name { get; }
        public List<Card> Cards { get; set; } = new List<Card>();
        public int afkSkips { get; set; } = 0; 

        public Player(SocketUser user)
        {
            thisUser = user;
            name = user.Username;
        }

		public Card DrawCard()
		{
            Card card = GenerateCard();
            Cards.Add(card);
            return card;
		}
    }
}
