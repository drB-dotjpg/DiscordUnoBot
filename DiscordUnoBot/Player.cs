using Discord.WebSocket;
using System.Collections.Generic;

namespace DiscordUnoBot
{
    public class Player : Helper
    {
        public SocketUser thisUser { get; }
        public string name { get; }
        public List<Card> Cards { get; set; } = new List<Card>();

		public bool hasGoneThisTurn = false; //not needed????

        public Player(SocketUser user)
        {
            thisUser = user;
            name = user.Username;
        }

		public void DrawCard()
		{
			Cards.Add(GenerateCard());
		}

		public void PlayCard(Card card)
		{
			Game.lastCard = card;
		}
    }
}
